using Newtonsoft.Json;

namespace SF.DocumentPoc.Models
{
    public class EntityHelperDto
    {
        public int ExcelIndex { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
    
    public class EntityResponseDto
    {
        [JsonProperty("result")]
        public EntityResult Result { get; set; }

    }

    public class EntityResult
    {
        [JsonProperty("records")]
        public List<EntityHelperDto> Records { get; set; }
    }
}
