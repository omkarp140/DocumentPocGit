using System.Collections.Concurrent;

namespace SF.DocumentPoc.Models
{
    public class NewDocument
    {
        public byte[] BinaryData { get; set; }
        public string FileName { get; set; }
        public ConcurrentBag<TextReplacementHelperDto> TextReplacementList { get; set; }
    }
}
