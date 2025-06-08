namespace COMMON.Kafka
{
    public static class Topics
    {
        public static class Sample
        {
            //-------------------------------------------------------------------------------------------
            public const string SampleConsumerGroup = "sample-service";
            //-------------------------------------------------------------------------------------------
            public const string WantsCreateSampleRequestTopic = "wants-create-sample";
            public const string CompletedCreateSampleResponseTopic = "completed-create-sample";
            //-------------------------------------------------------------------------------------------
            public const string WantsUpdateSampleRequestTopic = "wants-update-sample";
            public const string CompletedUpdateSampleResponseTopic = "completed-update-sample";
            //-------------------------------------------------------------------------------------------
            public const string WantsDeleteSampleRequestTopic = "wants-delete-sample";
            public const string CompletedDeleteSampleResponseTopic = "completed-delete-sample";
            //-------------------------------------------------------------------------------------------
            public const string WantsGetSampleRequestTopic = "wants-get-sample";
            public const string CompletedGetSampleResponseTopic = "completed-get-sample";
            //-------------------------------------------------------------------------------------------
            public const string WantsGetAllSampleRequestTopic = "wants-get-samples";
            public const string CompletedGetAllSampleResponseTopic = "completed-get-samples";
            //-------------------------------------------------------------------------------------------
        }
    }
}
