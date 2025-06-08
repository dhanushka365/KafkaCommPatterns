using COMMON.Configuration;
using COMMON.Kafka;
using COMMON.Messaging.Events.Sample.Get;

namespace API.Services.Kafka.Producers
{
    public class GetSampleProducer : AbstractKafkaProducer<WantsGetSampleEvent, CompletedGetSampleEvent>
    {
        public GetSampleProducer(KafkaConfig kafkaSettings, ILogger<GetSampleProducer> logger)
           : base(kafkaSettings, logger, Topics.Sample.CompletedGetSampleResponseTopic)
        {
        }

        public async Task<CompletedGetSampleEvent> SendGetOfferPackageRequestAsync(WantsGetSampleEvent requestEvent, TimeSpan timeout)
        {
            return await SendRequestAsync(Topics.Sample.WantsGetSampleRequestTopic, requestEvent, timeout);
        }
    }
}
