using System.Text.Json.Serialization;

namespace ZammadWinmailDatConverter.Models
{
    public class ZammadAttachmentCreate
    {
        public string? filename { get; set; }
        public string? data { get; set; }
        [JsonPropertyName("mime-type")] public string? mime_type { get; set; }
    }
}
