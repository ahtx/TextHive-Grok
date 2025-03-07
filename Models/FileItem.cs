namespace TextHiveGrok.Models
{
    public class FileItem
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public DateTime Modified { get; set; }
    }

    public class RelatedFile
    {
        public string FileName { get; set; } = string.Empty;
        public string RelatedWords { get; set; } = string.Empty;
    }
}