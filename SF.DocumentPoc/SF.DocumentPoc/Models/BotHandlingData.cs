namespace SF.DocumentPoc.Models
{
    public class BotHandlingData : BotHandlingBaseData
    {
        public Guid Id { get; set; }
        public BotHandlingEmailType[] EmailTypes { get; set; } = Array.Empty<BotHandlingEmailType>();
        public BotHandlingEmailType[] DocumentTypes { get; set; } = Array.Empty<BotHandlingEmailType>();
        public BotHandlingEmailType[] TableTypes { get; set; } = Array.Empty<BotHandlingEmailType>();
        public List<DocumentbotHandlingPerPageDetails> BotHandlingPerPageDetails = new List<DocumentbotHandlingPerPageDetails>();
        public List<TableExtractionBotHandlingPerPageDetails> TableExtractionBotHandlingPerPageDetails = new List<TableExtractionBotHandlingPerPageDetails>();
        public DocumentApproveStatus DocumentApproveStatus { get; set; }
        public string LanguageDetected { get; set; }
    }

    public class BotHandlingBaseData
    {
        public BotHandlingAction[] Actions { get; set; } = new BotHandlingAction[] { };
        public BotHandlingEntityIntent[] Entities { get; set; } = new BotHandlingEntityIntent[] { };
        public BotHandlingEntityIntent[] Intents { get; set; } = new BotHandlingEntityIntent[] { };
        public BotHandlingAction[] ActionFailedActions { get; set; } = new BotHandlingAction[] { };

    }

    public class BotHandlingAction
    {
        public string Action { get; set; }
        public int ActionType { get; set; }
        public bool Succeeded { get; set; }
        public string ShortDetails { get; set; }
        public string ActionDetails { get; set; }

        [Obsolete("Do not use this field anymore. Left for compatibility with old data")]
        public string Details { get; set; }

        [Obsolete("Do not use this field anymore. Left for compatibility with old data")]
        public string Value { get; set; }

        [Obsolete("Do not use this field anymore. Left for compatibility with old data")]
        public string InternalKey { get; set; }
        public bool IsActionSkipped { get; set; }
    }

    public class BotHandlingEntityIntent : BotHandlingCommon
    {
        public string Value { get; set; }
        public bool DataMasked { get; set; } = false;
        public bool FromOldModel { get; set; }
    }

    public abstract class BotHandlingCommon
    {
        public string Name { get; set; }
        public byte? Confidence { get; set; }
        public Guid Id { get; set; }
        public Guid BotHandlingId { get; set; }
        public bool IsLatestThread { get; set; }
    }

    public class BotHandlingEmailType : BotHandlingCommon
    {
        public string Details { get; set; }
        public bool Display { get; set; }
        public string Description { get; set; }
        public int? DetectionPolicy { get; set; }
    }

    public class DocumentbotHandlingPerPageDetails
    {
        public BotHandlingEmailType[] DocumentTypes { get; set; }
        public int PageNumber { get; set; }
    }

    public class TableExtractionBotHandlingPerPageDetails
    {
        public BotHandlingEmailType[] DocumentTableTypes { get; set; }
        public int PageNumber { get; set; }
        //public List<DocumentbotTableExtractionTableResponse> TableData { get; set; }
        public List<object> TableData { get; set; }
    }

    public enum DocumentApproveStatus
    {
        PendingForApproval = 0,
        ApprovedByBot = 1,
        ApprovedByHuman = 2,
        ApprovedViaCorrection = 3
    }
}
