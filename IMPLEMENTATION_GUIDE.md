# Guia de Implementação - Serviços Restantes

Este guia contém o código completo para os serviços Order, Payment e Notification.

## Order Service

### OrderService.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Shared\Shared.csproj" />
  </ItemGroup>
</Project>
```

### Models/Order.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public class Order
{
    [Key]
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    [Key]
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public enum OrderStatus
{
    Pending,
    PaymentProcessing,
    PaymentFailed,
    PaymentConfirmed,
    Shipped,
    Delivered,
    Cancelled
}

public class CreateOrderRequest
{
    public Guid UserId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

### Data/OrderDbContext.cs
```csharp
using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }
}
```

### Controllers/OrdersController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using Shared.Messaging;
using Shared.Events;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderDbContext context, IMessageBus messageBus, ILogger<OrdersController> logger)
    {
        _context = context;
        _messageBus = messageBus;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TotalAmount = request.TotalAmount,
            ShippingAddress = request.ShippingAddress,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = request.Items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Publish OrderCreated event
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(i => new Shared.Events.OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };
        _messageBus.Publish(orderCreatedEvent, "ecommerce.events", "order.created");

        _logger.LogInformation($"Order created: {order.Id}");

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Order>>> GetUserOrders(Guid userId)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return NotFound();

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Order status updated: {id} -> {status}");

        return NoContent();
    }
}
```

### Program.cs
```csharp
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using OrderService.Data;
using Shared.Messaging;
using Shared.ServiceDiscovery;
using Shared.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("ServiceName", "OrderService")
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "ecommerce-orderservice-{0:yyyy.MM.dd}"
    })
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();

builder.Configuration["Service:Name"] = "order-service";
builder.Configuration["Service:Host"] = "order-service";
builder.Configuration["Service:Port"] = "80";
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

// Subscribe to PaymentProcessed events
var messageBus = app.Services.GetRequiredService<IMessageBus>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

messageBus.Subscribe<PaymentProcessedEvent>("order-payment-queue", "ecommerce.events", "payment.processed", 
    async (paymentEvent) =>
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        
        var order = await context.Orders.FindAsync(paymentEvent.OrderId);
        if (order != null)
        {
            order.Status = paymentEvent.Success 
                ? OrderService.Models.OrderStatus.PaymentConfirmed 
                : OrderService.Models.OrderStatus.PaymentFailed;
            order.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            
            logger.LogInformation($"Order payment status updated: {order.Id}");
        }
    });

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["OrderService/OrderService.csproj", "OrderService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "OrderService/OrderService.csproj"

COPY . .
WORKDIR "/src/OrderService"
RUN dotnet build "OrderService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
```

## Payment Service

### PaymentService.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    <PackageReference Include="Stripe.net" Version="43.0.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Shared\Shared.csproj" />
  </ItemGroup>
</Project>
```

### Models/Payment.cs
```csharp
namespace PaymentService.Models;

public class ProcessPaymentRequest
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string PaymentMethodId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}

public class PaymentResponse
{
    public Guid PaymentId { get; set; }
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### Controllers/PaymentsController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Stripe;
using PaymentService.Models;
using Shared.Messaging;
using Shared.Events;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IConfiguration configuration, IMessageBus messageBus, ILogger<PaymentsController> logger)
    {
        _configuration = configuration;
        _messageBus = messageBus;
        _logger = logger;
        
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    [HttpPost("process")]
    public async Task<ActionResult<PaymentResponse>> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        var paymentId = Guid.NewGuid();

        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), // Stripe uses cents
                Currency = request.Currency,
                PaymentMethod = request.PaymentMethodId,
                Confirm = true,
                ReturnUrl = "https://example.com/return",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            var success = paymentIntent.Status == "succeeded";

            // Publish PaymentProcessed event
            var paymentEvent = new PaymentProcessedEvent
            {
                OrderId = request.OrderId,
                PaymentId = paymentId,
                Success = success,
                TransactionId = paymentIntent.Id
            };
            _messageBus.Publish(paymentEvent, "ecommerce.events", "payment.processed");

            _logger.LogInformation($"Payment processed: {paymentId} - Success: {success}");

            return Ok(new PaymentResponse
            {
                PaymentId = paymentId,
                Success = success,
                TransactionId = paymentIntent.Id
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, $"Payment failed: {paymentId}");

            var paymentEvent = new PaymentProcessedEvent
            {
                OrderId = request.OrderId,
                PaymentId = paymentId,
                Success = false
            };
            _messageBus.Publish(paymentEvent, "ecommerce.events", "payment.processed");

            return Ok(new PaymentResponse
            {
                PaymentId = paymentId,
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }
}
```

### Program.cs
```csharp
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Shared.Messaging;
using Shared.ServiceDiscovery;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("ServiceName", "PaymentService")
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "ecommerce-paymentservice-{0:yyyy.MM.dd}"
    })
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();

builder.Configuration["Service:Name"] = "payment-service";
builder.Configuration["Service:Host"] = "payment-service";
builder.Configuration["Service:Port"] = "80";
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PaymentService/PaymentService.csproj", "PaymentService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "PaymentService/PaymentService.csproj"

COPY . .
WORKDIR "/src/PaymentService"
RUN dotnet build "PaymentService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PaymentService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.dll"]
```

## Notification Service

### NotificationService.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    <PackageReference Include="SendGrid" Version="9.29.3" />
    <PackageReference Include="Twilio" Version="6.16.1" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Shared\Shared.csproj" />
  </ItemGroup>
</Project>
```

### Services/EmailService.cs
```csharp
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NotificationService.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var client = new SendGridClient(apiKey);
        
        var from = new EmailAddress("noreply@ecommerce.com", "E-Commerce Platform");
        var toAddress = new EmailAddress(to);
        
        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, body, body);
        
        var response = await client.SendEmailAsync(msg);
        
        _logger.LogInformation($"Email sent to {to}: {response.StatusCode}");
    }
}
```

### Services/SmsService.cs
```csharp
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace NotificationService.Services;

public interface ISmsService
{
    Task SendSmsAsync(string to, string message);
}

public class SmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsService> _logger;

    public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var accountSid = _configuration["Twilio:AccountSid"];
        var authToken = _configuration["Twilio:AuthToken"];
        TwilioClient.Init(accountSid, authToken);
    }

    public async Task SendSmsAsync(string to, string message)
    {
        var fromNumber = _configuration["Twilio:PhoneNumber"];
        
        var messageResource = await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(fromNumber),
            to: new Twilio.Types.PhoneNumber(to)
        );
        
        _logger.LogInformation($"SMS sent to {to}: {messageResource.Sid}");
    }
}
```

### Program.cs
```csharp
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Shared.Messaging;
using Shared.ServiceDiscovery;
using Shared.Events;
using NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("ServiceName", "NotificationService")
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "ecommerce-notificationservice-{0:yyyy.MM.dd}"
    })
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<ISmsService, SmsService>();
builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();

builder.Configuration["Service:Name"] = "notification-service";
builder.Configuration["Service:Host"] = "notification-service";
builder.Configuration["Service:Port"] = "80";
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var messageBus = app.Services.GetRequiredService<IMessageBus>();
var emailService = app.Services.GetRequiredService<IEmailService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Subscribe to events
messageBus.Subscribe<UserRegisteredEvent>("notification-user-queue", "ecommerce.events", "user.registered",
    async (userEvent) =>
    {
        await emailService.SendEmailAsync(
            userEvent.Email,
            "Welcome to E-Commerce Platform",
            $"Hello {userEvent.FullName}, welcome to our platform!"
        );
        logger.LogInformation($"Welcome email sent to {userEvent.Email}");
    });

messageBus.Subscribe<OrderCreatedEvent>("notification-order-queue", "ecommerce.events", "order.created",
    async (orderEvent) =>
    {
        await emailService.SendEmailAsync(
            orderEvent.UserEmail,
            "Order Confirmation",
            $"Your order #{orderEvent.OrderId} has been confirmed. Total: ${orderEvent.TotalAmount}"
        );
        logger.LogInformation($"Order confirmation sent for order {orderEvent.OrderId}");
    });

messageBus.Subscribe<OrderShippedEvent>("notification-shipped-queue", "ecommerce.events", "order.shipped",
    async (shippedEvent) =>
    {
        await emailService.SendEmailAsync(
            shippedEvent.UserEmail,
            "Order Shipped",
            $"Your order has been shipped! Tracking number: {shippedEvent.TrackingNumber}"
        );
        logger.LogInformation($"Shipping notification sent for order {shippedEvent.OrderId}");
    });

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NotificationService/NotificationService.csproj", "NotificationService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "NotificationService/NotificationService.csproj"

COPY . .
WORKDIR "/src/NotificationService"
RUN dotnet build "NotificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
```

## Instruções de Build

Para cada serviço, crie os arquivos conforme especificado acima e execute:

```bash
docker-compose build
docker-compose up -d
```
