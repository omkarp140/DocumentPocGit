using Newtonsoft.Json;

namespace SF.DocumentPoc.Models
{
    public class DocumentEntityTaggedCreateDto : DocumentTaggedBaseDto
    {
        [JsonProperty("ValueId")]
        public Guid EntityId { get; set; }
        public string WordIds { get; set; }
    }

    public class DocumentIntentTaggedCreateDto : DocumentTaggedBaseDto
    {
        [JsonProperty("ValueId")]
        public Guid IntentId { get; set; }
        public string WordIds { get; set; }
    }

    public class DocumentEntityTaggedReadDto : DocumentTaggedBaseDto
    {
        [JsonProperty("ValueId")]
        public Guid EntityId { get; set; }
        public Guid Id { get; set; }
        public List<int> WordIds { get; set; } = new List<int>();

    }

    public class DocumentIntentTaggedReadDto : DocumentTaggedBaseDto
    {
        [JsonProperty("ValueId")]
        public Guid IntentId { get; set; }
        public Guid Id { get; set; }
        public List<int> WordIds { get; set; } = new List<int>();

    }

    public class DocumentTaggedBaseDto : LinkedResourceBaseDto
    {
        public Guid DocumentId { get; set; }
        public string Value { get; set; }
        public int TaggedAuthor { get; set; }
        public bool IsDeleted { get; set; }
        public int? Confidence { get; set; }
        public bool FromOldModel { get; set; }
        public bool CorrectedValue { get; set; }
    }

    [Serializable]
    public abstract class LinkedResourceBaseDto : ILinkedResourceBaseDto
    {
        public List<LinkDto> Links { get; set; } = new List<LinkDto>();

    }

    public interface ILinkedResourceBaseDto
    {
        List<LinkDto> Links { get; set; }
    }

    [Serializable]
    public class LinkDto : ILinkDto
    {
        public string Href { get; set; }

        public string Rel { get; set; }

        public string Method { get; set; }

        public LinkDto(string href, string rel, string method)
        {
            Href = href;
            Rel = rel;
            Method = method;
        }
    }

    public interface ILinkDto
    {
        string Href { get; set; }

        string Rel { get; set; }

        string Method { get; set; }
    }
}
