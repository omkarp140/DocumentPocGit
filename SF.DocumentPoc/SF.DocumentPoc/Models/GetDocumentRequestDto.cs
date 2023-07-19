namespace SF.DocumentPoc.Models
{
    public class GetDocumentRequestDto
    {
        public string SearchText { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public int UserOffset { get; set; }
        public string OrderByColumn { get; set; }
        public string OrderDirection { get; set; }
        public IList<CommonFilterReadDto> DocumentStatus { get; set; } = new List<CommonFilterReadDto>();
        public IList<CommonFilterReadDto> DocumentApproveStatus { get; set; } = new List<CommonFilterReadDto>();
        public IList<CommonFilterReadDto> DocumentSource { get; set; } = new List<CommonFilterReadDto>();
        public IList<CommonFilterReadDto> DocumentTypes { get; set; } = new List<CommonFilterReadDto>();
        public IList<CommonFilterReadDto> Entities { get; set; } = new List<CommonFilterReadDto>();
        public IList<CommonFilterReadDto> Intents { get; set; } = new List<CommonFilterReadDto>();
        public IList<CommonFilterReadDto> ActionStatus { get; set; } = new List<CommonFilterReadDto>();
    }

    public class CommonFilterReadDto
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsChecked { get; set; }
    }

}
