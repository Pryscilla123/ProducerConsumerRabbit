using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Producer.Api.Infrastructure
{
    public class RabbitMqProducer : IDisposable
    {
        private readonly IConfiguration _configuration;
        private IConnection _connection;
        private IChannel _channel;

        public RabbitMqProducer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async void SendMessage<T>(T message)
        {
            await ConfigureProducer();

            var body = JsonConvert.SerializeObject(message);
            var bodyBytes = Encoding.UTF8.GetBytes(body);

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _configuration["RABBITMQ_QUEUE"]!,
                body: bodyBytes
            );
        }

        private async Task ConfigureProducer()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RABBITMQ_HOST"]!,
                Port = int.Parse(_configuration["RABBITMQ_PORT"]!),
                UserName = _configuration["RABBITMQ_USER"]!,
                Password = _configuration["RABBITMQ_PASSWORD"]!
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: _configuration["RABBITMQ_QUEUE"]!,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
        }

        public void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
        }
    }
}
