
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
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);

                    Console.WriteLine($"Armazem {armazem!.Nome} cadastrado!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);

                    //int retryCount = 0;

                    /*if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.TryGetValue("retry-count", out var value))
                        retryCount = Convert.ToInt32(value);

                    if (retryCount <= int.Parse(_configuration["RETRY_COUNT"]!))
                    {
                        ea.BasicProperties.Headers!["retry-count"] = ++retryCount;

                        // Posso ou não posso passar minha basic Properties
                        // Nada faz sentidooo
                        await _channel.BasicPublishAsync(
                            exchange: string.Empty,
                            routingKey: _configuration["RABBITMQ_DELAYED"]!,
                            basicProperties: ea.BasicProperties,
                            body: ea.Body
                        );
                    }
                    else
                    {
                        await _channel.BasicPublishAsync(
                            exchange: string.Empty,
                            routingKey: _configuration["RABBITMQ_DEADLETTER"]!,
                            body: ea.Body
                        );
                    }*/
                }
            };

            await _channel.BasicConsumeAsync(queue: _configuration["RABBITMQ_QUEUE"]!,
                                     autoAck: false,
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

            // declarando minha deadletter
            await _channel.QueueDeclareAsync(queue: _configuration["RABBITMQ_DEADLETTER"]!,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            // config da deadletter da fila principal
            var mainArgs = new Dictionary<string, object>
            {
                {"x-dead-letter-exchange", "" },
                { "x-dead-letter-routing-key", _configuration["RABBITMQ_DELAYED"]! }
            };

            // config da delayed da fila principal
            var delayedArgs = new Dictionary<string, object>
            {
                {"x-dead-letter-exchange", "" },
                { "x-dead-letter-routing-key", _configuration["RABBITMQ_QUEUE"]! },
                { "x-message-ttl", 600000 }
            };

            // declarando minha delayed
            await _channel.QueueDeclareAsync(queue: _configuration["RABBITMQ_DELAYED"]!,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: delayedArgs!);

            // config da fila

            await _channel.QueueDeclareAsync(queue: _configuration["RABBITMQ_QUEUE"]!,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: mainArgs!);
        }

        public override void Dispose()
        {
            _channel.CloseAsync();
            _connection.CloseAsync();
            base.Dispose();
        }
    }
}
