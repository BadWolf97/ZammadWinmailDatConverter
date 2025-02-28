namespace ZammadWinmailDatConverter.Models
{
    public class ZammadTicket
    {
        public int id { get; set; }
        public int group_id { get; set; }
        public int priority_id { get; set; }
        public int state_id { get; set; }
        public int? organization_id { get; set; }
        public string? number { get; set; }
        public string? title { get; set; }
        public int owner_id { get; set; }
        public int customer_id { get; set; }
        public string? note { get; set; }
        public DateTime? first_response_at { get; set; }
        public DateTime? first_response_escalation_at { get; set; }
        public int? first_response_in_min { get; set; }
        public int? first_response_diff_in_min { get; set; }
        public DateTime? close_at { get; set; }
        public DateTime? close_escalation_at { get; set; }
        public int? close_in_min { get; set; }
        public int? close_diff_in_min { get; set; }
        public DateTime? update_escalation_at { get; set; }
        public int? update_in_min { get; set; }
        public int? update_diff_in_min { get; set; }
        public DateTime? last_contact_at { get; set; }
        public DateTime? last_contact_agent_at { get; set; }
        public DateTime? last_contact_customer_at { get; set; }
        public DateTime? last_owner_update_at { get; set; }
        public int create_article_type_id { get; set; }
        public int create_article_sender_id { get; set; }
        public int article_count { get; set; }
        public DateTime? escalation_at { get; set; }
        public DateTime? pending_time { get; set; }
        public string? type { get; set; }
        public string? time_unit { get; set; }
        public Dictionary<string, object>? preferences { get; set; }
        public int updated_by_id { get; set; }
        public int created_by_id { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
