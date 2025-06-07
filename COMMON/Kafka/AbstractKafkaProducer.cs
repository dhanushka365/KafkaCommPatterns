using COMMON.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace COMMON.Kafka
{
    public abstract class AbstractKafkaProducer<TRequestEvent, TResponseEvent> : IDisposable
        where TRequestEvent : class
        where TResponseEvent : class
    {
        private readonly IProducer<string, string> _producer;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<TResponseEvent>> _responseHandlers;
        private readonly string _replyTopic;
        private readonly IConsumer<string, string> _consumer;
        private readonly CancellationTokenSource _cts;
        private readonly Task _consumerTask;
        protected readonly ILogger _logger;
        protected readonly KafkaConfig KafkaSettings;
        private bool _disposed = false;

        protected AbstractKafkaProducer(KafkaConfig kafkaSettings, ILogger logger, string replyTopic)
        {
            KafkaSettings = kafkaSettings ?? throw new ArgumentNullException(nameof(kafkaSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _replyTopic = replyTopic ?? throw new ArgumentNullException(nameof(replyTopic));

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = KafkaSettings.BootstrapServers,
                // Include security settings if necessary
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();

            // Set up a consumer to listen on the reply topic
            _responseHandlers = new ConcurrentDictionary<string, TaskCompletionSource<TResponseEvent>>();

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = KafkaSettings.BootstrapServers,
                GroupId = $"kafka-producer-client-{Guid.NewGuid()}",
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
            _consumer.Subscribe(_replyTopic);

            _cts = new CancellationTokenSource();
            _consumerTask = Task.Run(() => ConsumeReplies(_cts.Token));
        }

        private void ConsumeReplies(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = _consumer.Consume(cancellationToken);
                    var message = consumeResult.Message;
                    var headers = message.Headers;

                    var correlationId = headers.TryGetLastBytes("correlationId", out var correlationIdBytes)
                        ? Encoding.UTF8.GetString(correlationIdBytes)
                        : null;

                    if (correlationId != null && _responseHandlers.TryRemove(correlationId, out var tcs))
                    {
                        if (message.Value != null)
                        {
                            var responseEvent = JsonSerializer.Deserialize<TResponseEvent>(message.Value);
                            tcs.SetResult(responseEvent);
                        }
                        else
                        {
                            tcs.SetException(new InvalidOperationException("Received null response message."));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ConsumeReplies operation canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConsumeReplies.");
            }
            finally
            {
                _consumer.Close();
                _consumer.Dispose();
            }
        }

        public async Task<TResponseEvent> SendRequestAsync(string topic, TRequestEvent requestEvent, TimeSpan timeout)
        {
            var correlationId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<TResponseEvent>();
            _responseHandlers[correlationId] = tcs;

            var eventJson = JsonSerializer.Serialize(requestEvent);
            var message = new Message<string, string>
            {
                Key = null,
                Value = eventJson,
                Headers = new Headers
                {
                    { "correlationId", Encoding.UTF8.GetBytes(correlationId) },
                    { "replyTopic", Encoding.UTF8.GetBytes(_replyTopic) },
                    { "eventType", Encoding.UTF8.GetBytes(typeof(TRequestEvent).Name) }
                }
            };

            try
            {
                await _producer.ProduceAsync(topic, message).ConfigureAwait(false);
                _logger.LogInformation("Event {EventType} produced to topic {Topic}", typeof(TRequestEvent).Name, topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error producing event {EventType} to topic {Topic}", typeof(TRequestEvent).Name, topic);
                _responseHandlers.TryRemove(correlationId, out _);
                throw;
            }

            using var cts = new CancellationTokenSource(timeout);
            using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                try
                {
                    var responseEvent = await tcs.Task.ConfigureAwait(false);
                    return responseEvent;
                }
                catch (TaskCanceledException)
                {
                    throw new TimeoutException("The request timed out.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during response processing.");
                    throw;
                }
                finally
                {
                    _responseHandlers.TryRemove(correlationId, out _);
                }
            }
        }

        public async Task FireAndForgetAsync(string topic, TRequestEvent eventMessage)
        {
            var eventJson = JsonSerializer.Serialize(eventMessage);
            var message = new Message<string, string>
            {
                Key = null,
                Value = eventJson,
                Headers = new Headers
                {
                    { "eventType", Encoding.UTF8.GetBytes(typeof(TRequestEvent).Name) }
                }
            };

            try
            {
                await _producer.ProduceAsync(topic, message).ConfigureAwait(false);
                _logger.LogInformation("Event {EventType} produced to topic {Topic}", typeof(TRequestEvent).Name, topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error producing event {EventType} to topic {Topic}", typeof(TRequestEvent).Name, topic);
                throw;
            }
        }

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
                    // Expected exception during cancellation
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception during Dispose.");
                }

                _producer.Dispose();
                _cts.Dispose();
                _disposed = true;
            }
        }
    }
}