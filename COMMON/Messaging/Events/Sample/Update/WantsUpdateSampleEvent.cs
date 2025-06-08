namespace COMMON.Messaging.Events.Sample.Update
{
    public class WantsUpdateSampleEvent
    {
        public string? Id { get; set; }
        public string? Name { get; set; } = null!;

        public string? Description { get; set; }

        public string? Type { get; set; }

        public int? Count { get; set; }
    }
}
