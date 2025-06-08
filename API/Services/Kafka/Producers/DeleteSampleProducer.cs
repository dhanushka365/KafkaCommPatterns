using COMMON.Configuration;
using COMMON.Kafka;
using COMMON.Messaging.Events.Sample.Delete;

namespace API.Services.Kafka.Producers
{
    public class DeleteSampleProducer : AbstractKafkaProducer<WantsDeleteSampleEvent, CompletedDeleteSampleEvent>
    {
        public DeleteSampleProducer(KafkaConfig kafkaSettings, ILogger<DeleteSampleProducer> logger)
           : base(kafkaSettings, logger, Topics.Sample.CompletedDeleteSampleResponseTopic)
        {
        }

        public async Task<CompletedDeleteSampleEvent> SendDeleteSampleRequestAsync(WantsDeleteSampleEvent requestEvent, TimeSpan timeout)
        {
            return await SendRequestAsync(Topics.Sample.WantsDeleteSampleRequestTopic, requestEvent, timeout);
        }
    }
}
