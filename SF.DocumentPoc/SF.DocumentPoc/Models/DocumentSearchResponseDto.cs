using Newtonsoft.Json;

namespace SF.DocumentPoc.Models
{
    public class DocumentSearchResponseDto
    {
        [JsonProperty("result")]
        public Result Result { get; set; }
    }

    public class Result
    {
        [JsonProperty("records")]
        public List<Record> Records { get; set; }
    }

    public class Record
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("documentName")]
        public string DocumentName { get; set; }
    }
}
