using COMMON.Configuration;
using COMMON.Kafka;
using COMMON.Messaging.Events.Sample.Create;

namespace API.Services.Kafka.Producers
{
    public class CreateSampleProducer : AbstractKafkaProducer<WantsCreateSampleEvent, CompletedCreateSampleEvent>
    {
        public CreateSampleProducer(KafkaConfig kafkaSettings, ILogger<CreateSampleProducer> logger)
            : base(kafkaSettings, logger, Topics.Sample.CompletedCreateSampleResponseTopic)
        {
        }

        public async Task<CompletedCreateSampleEvent> SendCreateSampleRequestAsync(WantsCreateSampleEvent requestEvent, TimeSpan timeout)
        {
            return await SendRequestAsync(Topics.Sample.WantsCreateSampleRequestTopic, requestEvent, timeout);
        }
    }
}
