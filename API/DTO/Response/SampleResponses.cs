namespace API.DTO.Response
{
    public class SampleResponses
    {
        public List<SampleResponseDto> Samples { get; set; } = new List<SampleResponseDto>();
        public PaginationDto Pagination { get; set; } = new PaginationDto();
    }
}
