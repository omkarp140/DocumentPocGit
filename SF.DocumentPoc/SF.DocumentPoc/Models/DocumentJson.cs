namespace SF.DocumentPoc.Models
{
    public class DocumentJson
    {
        public List<DocumentPagesDto> Pages { get; set; } = new List<DocumentPagesDto>();
    }

    public class DocumentPagesDto : PageContentDetails
    {
        public List<DocumentTextBoxesLayoutDto> TextBoxesLayout { get; set; } = new List<DocumentTextBoxesLayoutDto>();

        public List<DocumentWordLevelDto> WordLevel { get; set; } = new List<DocumentWordLevelDto>();
    }

    public class DocumentWordLevelDto
    {
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public int WordId { get; set; }
        public int Page { get; set; }
        public string Text { get; set; }
        public string BoxId { get; set; }
        public int EndInSentence { get; set; }
        public int EndInTextBox { get; set; }
        public int StartInSentence { get; set; }
        public int StartInTextBox { get; set; }
        public string Space { get; set; } = string.Empty;
    }

    public class DocumentTextBoxesLayoutDto
    {
        public List<DocumentBoxDto> Boxes { get; set; }
        public int Distance { get; set; }
    }

    public class DocumentBoxDto
    {
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public int Page { get; set; }
        public string Text { get; set; }
        public string BoxId { get; set; }
        public DocumentBoxTableCell TableCell { get; set; }
    }

    public class DocumentBoxTableCell
    {
        public string Id { get; set; }
        public double X0 { get; set; }
        public double X1 { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }
    }

    public class PageContentDetails
    {
        public double Height { get; set; }
        public double Width { get; set; }
    }
}
