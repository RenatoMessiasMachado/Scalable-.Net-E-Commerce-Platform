using Serilog;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;
using Shared.Messaging;
using Shared.ServiceDiscovery;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("ServiceName", "CartService")
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "ecommerce-cartservice-{0:yyyy.MM.dd}"
    })
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration["Redis:Configuration"] ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(configuration);
});

// Services
builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();

// Service Discovery
builder.Configuration["Service:Name"] = "cart-service";
builder.Configuration["Service:Host"] = "cart-service";
builder.Configuration["Service:Port"] = "80";
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

// Health Checks
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
