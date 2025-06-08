namespace API.DTO.Response
{
    public class SampleResponseDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Type { get; set; }
        public int Count { get; set; }
    }
}
