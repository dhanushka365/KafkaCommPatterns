using COMMON.Configuration;
using COMMON.Kafka;
using COMMON.Messaging.Events.Sample.GetAll;

namespace API.Services.Kafka.Producers
{
    public class GetAllSampleProducer : AbstractKafkaProducer<WantsGetAllSampleEvent, CompletedGetAllSampleEvent>
    {
        public GetAllSampleProducer(KafkaConfig kafkaSettings, ILogger<GetAllSampleProducer> logger)
           : base(kafkaSettings, logger, Topics.Sample.CompletedGetAllSampleResponseTopic)
        {
        }

        public async Task<CompletedGetAllSampleEvent> SendGetAllSampleRequestAsync(WantsGetAllSampleEvent requestEvent, TimeSpan timeout)
        {
            return await SendRequestAsync(Topics.Sample.WantsGetAllSampleRequestTopic, requestEvent, timeout);
        }
    }
}
