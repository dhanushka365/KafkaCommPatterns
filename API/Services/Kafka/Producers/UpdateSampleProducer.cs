using COMMON.Configuration;
using COMMON.Kafka;
using COMMON.Messaging.Events.Sample.Update;

namespace API.Services.Kafka.Producers
{
    public class UpdateSampleProducer : AbstractKafkaProducer<WantsUpdateSampleEvent, CompletedUpdateSampleEvent>
    {
        public UpdateSampleProducer(KafkaConfig kafkaSettings, ILogger<UpdateSampleProducer> logger)
            : base(kafkaSettings, logger, Topics.Sample.CompletedUpdateSampleResponseTopic)
        {
        }

        public async Task<CompletedUpdateSampleEvent> SendUpdateSampleRequestAsync(WantsUpdateSampleEvent requestEvent, TimeSpan timeout)
        {
            return await SendRequestAsync(Topics.Sample.WantsUpdateSampleRequestTopic, requestEvent, timeout);
        }
    }
}
