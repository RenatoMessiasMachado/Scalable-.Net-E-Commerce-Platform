using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared.Messaging;

public interface IMessageBus
{
    void Publish<T>(T message, string exchange, string routingKey) where T : class;
    void Subscribe<T>(string queue, string exchange, string routingKey, Action<T> handler) where T : class;
}

public class RabbitMQMessageBus : IMessageBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQMessageBus> _logger;

    public RabbitMQMessageBus(IConfiguration configuration, ILogger<RabbitMQMessageBus> logger)
    {
        _logger = logger;
        
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void Publish<T>(T message, string exchange, string routingKey) where T : class
    {
        try
        {
            _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation($"Published message to {exchange}/{routingKey}: {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error publishing message to {exchange}/{routingKey}");
            throw;
        }
    }

    public void Subscribe<T>(string queue, string exchange, string routingKey, Action<T> handler) where T : class
    {
        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue, exchange, routingKey);
        _channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (sender, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json);

                if (message != null)
                {
                    handler(message);
                    _channel.BasicAck(eventArgs.DeliveryTag, false);
                    _logger.LogInformation($"Processed message from {queue}: {typeof(T).Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message from {queue}");
                _channel.BasicNack(eventArgs.DeliveryTag, false, true);
            }

            await Task.CompletedTask;
        };

        _channel.BasicConsume(queue, autoAck: false, consumer);
        _logger.LogInformation($"Subscribed to {queue} on {exchange}/{routingKey}");
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
