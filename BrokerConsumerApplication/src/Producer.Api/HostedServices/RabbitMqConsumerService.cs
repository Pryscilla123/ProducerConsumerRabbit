
using Consumer.Api.Repository;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using Consumer.Api.Models;

namespace Consumer.Api.HostedServices
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IArmazemRepository _armazemRepository;
        private IConnection _connection;
        private IChannel _channel;

        public RabbitMqConsumerService (IConfiguration configuration,
                                        IArmazemRepository armazemRepository)
        {
            _configuration = configuration;
            _armazemRepository = armazemRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ConfigureConsumer();

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                Console.WriteLine("Message received!");
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                // Minha lógica vai ficar aqui dentro

                var armazem = JsonConvert.DeserializeObject<Armazem>(message);

                try
                {
                    await _armazemRepository.CriarArmazem(armazem!);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Console.WriteLine($"Armazem {armazem!.Nome} cadastrado!");
            };

            await _channel.BasicConsumeAsync(queue: _configuration["RABBITMQ_QUEUE"]!,
                                     autoAck: true,
                                     consumer: consumer);

            await Task.CompletedTask;
        }

        private async Task ConfigureConsumer()
        {
            var factory = new ConnectionFactory() { 
                HostName = _configuration["RABBITMQ_HOST"]!,
                Port = int.Parse(_configuration["RABBITMQ_PORT"]!),
                UserName = _configuration["RABBITMQ_USER"]!,
                Password = _configuration["RABBITMQ_PASSWORD"]!
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(queue: _configuration["RABBITMQ_QUEUE"]!,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
        }

        public override void Dispose()
        {
            _channel.CloseAsync();
            _connection.CloseAsync();
            base.Dispose();
        }
    }
}
