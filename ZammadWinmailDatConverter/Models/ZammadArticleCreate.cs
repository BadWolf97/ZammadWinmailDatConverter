using System.Text.Json.Serialization;

namespace ZammadWinmailDatConverter.Models
{
    public class ZammadArticleCreate
    {
        public int ticket_id { get; set; }
        public string? to { get; set; }
        public string? cc { get; set; }
        public string? subject { get; set; }
        public string? body { get; set; }
        public string? content_type { get; set; }
        public string? type { get; set; }
        [JsonPropertyName("internal")]
        public bool isinternal { get; set; }
        public string? time_unit { get; set; }
        public List<ZammadAttachmentCreate>? attachments { get; set; }
    }
}
