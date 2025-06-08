namespace COMMON.Messaging.Events.Sample.GetAll
{
    public class WantsGetAllSampleEvent
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string? SearchField { get; set; }
        public string? SearchTerm { get; set; }
    }
}
