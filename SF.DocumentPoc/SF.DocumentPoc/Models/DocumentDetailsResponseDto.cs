namespace SF.DocumentPoc.Models
{
    public class DocumentDetailsResponseDto
    {
        public DocumentDetails Result { get; set; }
    }

    public class DocumentDetails
    {
        public Guid Id { get; set; }
        public Guid DocumentbotId { get; set; }
        public string DocumentName { get; set; }
        public DocumentJson DocumentJson { get; set; }
        public DocumentTaggedDto TaggedData { get; set; } = new DocumentTaggedDto();
    }

    public class DocumentTaggedDto
    {
        public IList<DocumentIntentTaggedReadDto> IntentsTagged { get; set; } = new List<DocumentIntentTaggedReadDto>();
        public IList<DocumentEntityTaggedReadDto> EntitiesTagged { get; set; } = new List<DocumentEntityTaggedReadDto>();
        public IList<DocumentTypeLinkReadDto> DocumentTypeLink { get; set; } = new List<DocumentTypeLinkReadDto>();
    }

    public class DocumentTypeLinkReadDto
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Guid DocumentTypeId { get; set; }
        public int TaggedAuthor { get; set; }
        public int Confidence { get; set; }
        public bool IsDeleted { get; set; }
        public int? PageNumber { get; set; }
        public int? DetectionPolicy { get; set; }
    }
}
