using COMMON.Configuration;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace COMMON.Kafka
{
    public class TopicManager
    {
        private readonly KafkaConfig _kafkaSettings;
        private readonly ILogger<TopicManager> _logger;

        public TopicManager(KafkaConfig kafkaSettings, ILogger<TopicManager> logger)
        {
            _kafkaSettings = kafkaSettings;
            _logger = logger;
        }

        public async Task CreateTopicsAsync(IEnumerable<string> topicNames)
        {
            var adminConfig = new AdminClientConfig { BootstrapServers = _kafkaSettings.BootstrapServers };

            using (var adminClient = new AdminClientBuilder(adminConfig).Build())
            {
                var topicSpecifications = new List<TopicSpecification>();
                foreach (var topicName in topicNames)
                {
                    topicSpecifications.Add(new TopicSpecification
                    {
                        Name = topicName,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    });
                }

                try
                {
                    await adminClient.CreateTopicsAsync(topicSpecifications);
                    _logger.LogInformation("Topics created successfully: {Topics}", string.Join(", ", topicNames));
                }
                catch (CreateTopicsException e)
                {
                    foreach (var result in e.Results)
                    {
                        if (result.Error.Code == ErrorCode.TopicAlreadyExists)
                        {
                            _logger.LogInformation("Topic {Topic} already exists.", result.Topic);
                        }
                        else
                        {
                            _logger.LogError("An error occurred creating topic {Topic}: {Reason}", result.Topic, result.Error.Reason);
                            throw;
                        }
                    }
                }
            }
        }
    }
}
