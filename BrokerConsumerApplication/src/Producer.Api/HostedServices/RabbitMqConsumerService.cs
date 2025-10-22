using Consumer.Api.Repository;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using Consumer.Api.Models;
using Consumer.Api.Utils;

namespace Consumer.Api.HostedServices
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IArmazemRepository _armazemRepository;
        private IConnection _connection;
        private IChannel _channel;
        private int retryCount = 0;

        private readonly int MAX_RETRY = 0;

        public RabbitMqConsumerService(IConfiguration configuration,
                                        IArmazemRepository armazemRepository)
        {
            _configuration = configuration;
            _armazemRepository = armazemRepository;
            MAX_RETRY = int.Parse(_configuration["RETRY_COUNT"]!);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ConfigureConsumer();

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                Console.WriteLine("Message received!");

                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                var propsMessage = new BasicProperties
                {
                    Persistent = true,
                    ContentType = ea.BasicProperties.ContentType,
                    DeliveryMode = ea.BasicProperties.DeliveryMode,
                    Expiration = 60000.ToString(), // 1 minuto
                    Headers = ea.BasicProperties.Headers != null ? new Dictionary<string, object>(ea.BasicProperties.Headers) : new Dictionary<string, object>()
                };

                // Minha lógica vai ficar aqui dentro

                var armazem = JsonConvert.DeserializeObject<Armazem>(message);

                try
                {

                    if (propsMessage.Headers.TryGetValue("CountRetry", out var currentRetry) && (int)currentRetry == MAX_RETRY)
                    {
                        throw new ExceptionUtils("Max retries reached");
                    }

                    throw new Exception("Erro genérico");
                    await _armazemRepository.CriarArmazem(armazem!);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);

                    Console.WriteLine($"Armazem {armazem!.Nome} cadastrado!");
                }
                catch (ExceptionUtils ex)
                {
                    //publicar na deadletter0
                    await _channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: _configuration["RABBITMQ_DEADLETTER"]!,
                        body: ea.Body
                        );

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    if (!propsMessage.Headers.TryGetValue("CountRetry", out var currentRetry))
                    {
                        propsMessage.Headers!["CountRetry"] = 1;

                        //propsMessage.Headers!["ttl"] = 10000 * 1; // 10 segundos * 1

                        await _channel.BasicPublishAsync(
                            exchange: string.Empty,
                            routingKey: _configuration["RABBITMQ_DELAYED"]!,
                            basicProperties: propsMessage,
                            body: ea.Body,
                            mandatory: true
                            );

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    else if (propsMessage.Headers.TryGetValue("CountRetry", out currentRetry))
                    {
                        int updatedRetry = (int)currentRetry + 1;
                        propsMessage.Headers!["CountRetry"] = updatedRetry;

                        await _channel.BasicPublishAsync(
                            exchange: string.Empty,
                            routingKey: _configuration["RABBITMQ_DELAYED"]!,
                            basicProperties: propsMessage,
                            body: ea.Body,
                            mandatory: true
                            );

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);

                        //publica com TTL
                    }
                }
            };

            await _channel.BasicConsumeAsync(queue: _configuration["RABBITMQ_QUEUE"]!,
                                         autoAck: false,
                                         consumer: consumer);

            await Task.CompletedTask;
        }

        private async Task ConfigureConsumer()
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

            // declarando minha deadletter
            await _channel.QueueDeclareAsync(queue: _configuration["RABBITMQ_DEADLETTER"]!,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            // config da delayed da fila principal
            var delayedArgs = new Dictionary<string, object>
            {
                {"x-dead-letter-exchange", "" },
                { "x-dead-letter-routing-key", _configuration["RABBITMQ_QUEUE"]! }
                //{ "x-message-ttl", 600000 }
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
                                 arguments: null);
        }

        public override void Dispose()
        {
            if (_channel != null)
            {
                _channel.CloseAsync();
            }
            if (_connection != null)
            {
                _connection.CloseAsync();
            }
            base.Dispose();
        }
    }
}