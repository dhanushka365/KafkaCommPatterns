using COMMON.Messaging.Events.Sample.Common;

namespace COMMON.Messaging.Events.Sample.GetAll
{
    public class CompletedGetAllSampleEvent
    {
        public IEnumerable<SampleData> Data { get; set; } = [];

        // Pagination fields
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
    }
}
