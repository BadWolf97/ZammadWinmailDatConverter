using System.Text.Json.Serialization;

namespace ZammadWinmailDatConverter.Models
{
    public class ZammadTicketArticle
    {
        public int id { get; set; }
        public int ticket_id { get; set; }
        public int type_id { get; set; }
        public int sender_id { get; set; }
        public string? from { get; set; }
        public string? to { get; set; }
        public string? cc { get; set; }
        public string? subject { get; set; }
        public string? reply_to { get; set; }
        public string? message_id { get; set; }
        public string? message_id_md5 { get; set; }
        public string? in_reply_to { get; set; }
        public string? content_type { get; set; }
        public string? references { get; set; }
        public string? body { get; set; }
        [JsonPropertyName("internal")] public bool isinternal { get; set; }
        public Dictionary<string, object>? preferences { get; set; }
        public int updated_by_id { get; set; }
        public int created_by_id { get; set; }
        public int? origin_by_id { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public List<ZammadAttachment>? attachments { get; set; }
        public string? type { get; set; }
        public string? sender { get; set; }
        public string? created_by { get; set; }
        public string? updated_by { get; set; }
        public string? time_unit { get; set; }
    }
}
