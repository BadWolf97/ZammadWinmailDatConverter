namespace ZammadWinmailDatConverter.Models
{
    public class ZammadAttachment
    {
        public int id { get; set; }
        public int store_file_id { get; set; }
        public string? filename { get; set; }
        public string? size { get; set; }
        public Dictionary<string, object>? preferences { get; set; }
    }
}
