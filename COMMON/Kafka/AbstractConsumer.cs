using COMMON.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace COMMON.Kafka
{
    /// <summary>
    /// Abstract base class for Kafka consumers supporting both Fire-and-Forget and Request-Reply patterns.
    /// </summary>
    /// <typeparam name="TRequestEvent">Type of the incoming request event.</typeparam>
    /// <typeparam name="TResponseEvent">Type of the response event. Use VoidResponse for Fire-and-Forget.</typeparam>
    public abstract class AbstractKafkaConsumer<TRequestEvent, TResponseEvent> : IDisposable
        where TRequestEvent : class
        where TResponseEvent : class
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly IProducer<string, string>? _producer;
        private readonly KafkaConfig _kafkaSettings;
        private readonly ILogger _logger;
        private readonly string _requestTopic;
        private readonly string _replyTopic;
        private readonly CancellationTokenSource _cts;
        private readonly Task _consumerTask;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractKafkaConsumer{TRequestEvent, TResponseEvent}"/> class.
        /// </summary>
        /// <param name="kafkaSettings">Kafka configuration settings.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="requestTopic">Kafka topic to consume requests from.</param>
        /// <param name="replyTopic">Kafka topic to send replies to. Use null or VoidResponse for Fire-and-Forget.</param>
        protected AbstractKafkaConsumer(
            KafkaConfig kafkaSettings,
            ILogger logger,
            string requestTopic,
            string? replyTopic = null)
        {
            _kafkaSettings = kafkaSettings ?? throw new ArgumentNullException(nameof(kafkaSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _requestTopic = requestTopic ?? throw new ArgumentNullException(nameof(requestTopic));
            _replyTopic = replyTopic ?? string.Empty; // Empty string indicates no reply

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                GroupId = $"{requestTopic}-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true,
                SessionTimeoutMs = 30000, // Default is 10000 ms
                MaxPollIntervalMs = 120000, // Adjust if rebalancing takes longer
                ReconnectBackoffMs = 100,
                ReconnectBackoffMaxMs = 10000,
                AllowAutoCreateTopics = true
                // Include security settings if necessary
            };

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            _consumer.Subscribe(_requestTopic);

            // Initialize producer only if a reply is expected
            if (!string.IsNullOrEmpty(_replyTopic))
            {
                var producerConfig = new ProducerConfig
                {
                    BootstrapServers = _kafkaSettings.BootstrapServers
                    // Include security settings if necessary
                };
                _producer = new ProducerBuilder<string, string>(producerConfig).Build();
            }

            _cts = new CancellationTokenSource();
            _consumerTask = Task.Run(() => ConsumeLoop(_cts.Token));
        }

        /// <summary>
        /// The main loop for consuming messages.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task ConsumeLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = _consumer.Consume(cancellationToken);
                    var message = consumeResult.Message;

                    TRequestEvent? requestEvent = null;
                    try
                    {
                        requestEvent = JsonSerializer.Deserialize<TRequestEvent>(message.Value);
                        if (requestEvent == null)
                        {
                            _logger.LogWarning("Received null message on topic {Topic}. Skipping.", _requestTopic);
                            continue;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize message on topic {Topic}. Skipping.", _requestTopic);
                        continue;
                    }

                    _logger.LogInformation("Received message on topic {Topic}: {Message}", _requestTopic, message.Value);

                    TResponseEvent? responseEvent = null;
                    try
                    {
                        responseEvent = await HandleMessageAsync(requestEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling message of type {EventType} on topic {Topic}.", typeof(TRequestEvent).Name, _requestTopic);
                        // Depending on requirements, you might choose to continue or break
                        continue;
                    }

                    // If a reply is expected and responseEvent is not VoidResponse, send the reply
                    if (!string.IsNullOrEmpty(_replyTopic) && !(responseEvent is VoidResponse))
                    {
                        try
                        {
                            // Extract correlationId from message headers
                            var correlationId = message.Headers.TryGetLastBytes("correlationId", out var correlationIdBytes)
                                ? Encoding.UTF8.GetString(correlationIdBytes)
                                : Guid.NewGuid().ToString(); // Fallback to new GUID

                            var responseJson = JsonSerializer.Serialize(responseEvent);

                            var responseMessage = new Message<string, string>
                            {
                                Key = null,
                                Value = responseJson,
                                Headers = new Headers
                                {
                                    { "correlationId", Encoding.UTF8.GetBytes(correlationId) },
                                    { "eventType", Encoding.UTF8.GetBytes(typeof(TResponseEvent).Name) }
                                }
                            };

                            await _producer.ProduceAsync(_replyTopic, responseMessage, cancellationToken);
                            _logger.LogInformation("Sent response to topic {ReplyTopic} with correlationId {CorrelationId}.", _replyTopic, correlationId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send response to topic {ReplyTopic}.", _replyTopic);
                            // Depending on requirements, you might choose to handle the failure differently
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumer loop for topic {Topic} is stopping due to cancellation.", _requestTopic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in consumer loop for topic {Topic}.", _requestTopic);
            }
            finally
            {
                _consumer.Close();
                _consumer.Dispose();

                if (!string.IsNullOrEmpty(_replyTopic))
                {
                    _producer.Dispose();
                }

                _logger.LogInformation("Consumer for topic {Topic} has stopped.", _requestTopic);
            }
        }

        /// <summary>
        /// Handles the incoming message and optionally returns a response.
        /// </summary>
        /// <param name="message">The deserialized request event.</param>
        /// <returns>The response event or VoidResponse for Fire-and-Forget.</returns>
        protected abstract Task<TResponseEvent?> HandleMessageAsync(TRequestEvent message);

        /// <summary>
        /// Disposes the consumer and cancels the consuming loop.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _cts.Cancel();
                try
                {
                    _consumerTask.Wait();
                }
                catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
                {
                    // Expected when cancelling
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during disposal of consumer for topic {Topic}.", _requestTopic);
                }
                finally
                {
                    _cts.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}
