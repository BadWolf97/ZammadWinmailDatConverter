using MimeKit.Tnef;
using System.Text;
using ZammadWinmailDatConverter.Models;

namespace ZammadWinmailDatConverter
{
    internal class Program
    {
        static string zammadhost = "";
        static string zammadtoken = "";
        static DateTime lastCheck = DateTime.MinValue;

        /// <summary>
        /// Main Entry Point.
        /// </summary>
        static void Main(string[] args)
        {
            zammadhost = Environment.GetEnvironmentVariable("ZammadHost") ?? zammadhost;
            zammadtoken = Environment.GetEnvironmentVariable("ZammadToken") ?? zammadtoken;

            if (string.IsNullOrEmpty(zammadhost) || string.IsNullOrEmpty(zammadtoken))
            {
                Console.WriteLine("Please set the ZammadHost and ZammadToken environment variables.");
                return;
            }

            if (Path.Exists(Path.Combine(AppContext.BaseDirectory, "lastcheck")))
            {
                lastCheck = File.GetLastWriteTime(Path.Combine(AppContext.BaseDirectory, "lastcheck"));
            }

            BGCheck();
        }

        /// <summary>
        /// Background Check for new Tickets.
        /// </summary>
        private static void BGCheck()
        {
            while (true)
            {
                DateTime now = DateTime.Now;
                if (now - lastCheck > TimeSpan.FromMinutes(1))
                {
                    try
                    {
                        CheckTicketsForWinmailDat().Wait();
                        lastCheck = now;
                        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "lastcheck"), lastCheck.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Check all Tickets for winmail.dat attachments.
        /// </summary>
        /// <returns></returns>
        private static async Task CheckTicketsForWinmailDat()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", zammadtoken);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            List<ZammadTicket> tickets = await GetTickets(client);
            if (tickets == null || tickets.Count == 0)
            {
                Console.WriteLine("No tickets found.");
                return;
            }
            foreach (ZammadTicket? ticket in tickets.ToList().FindAll((x) => x.updated_at > lastCheck && x.state_id != 4))
            {
                await CheckTicket(client, ticket);
            }
        }

        /// <summary>
        /// Get all Tickets from Zammad.
        /// </summary>
        /// <returns></returns>
        private static async Task<List<ZammadTicket>> GetTickets(HttpClient client)
        {
            List<ZammadTicket> tickets = new List<ZammadTicket>();
            int currentPage = 1;
            while (true)
            {
                HttpResponseMessage ticketsresponse = await client.GetAsync($"{zammadhost}/api/v1/tickets?page={currentPage}");
                ticketsresponse.EnsureSuccessStatusCode();
                string ticketscontent = await ticketsresponse.Content.ReadAsStringAsync();
                ZammadTicket[]? currentTickets = System.Text.Json.JsonSerializer.Deserialize<ZammadTicket[]>(ticketscontent);
                if (currentTickets == null || currentTickets.Length == 0)
                {
                    break;
                }
                tickets.AddRange(currentTickets);
                currentPage++;
            }

            return tickets;
        }

        /// <summary>
        /// Check a Ticket for winmail.dat attachments.
        /// </summary>
        /// <returns></returns>
        private static async Task CheckTicket(HttpClient client, ZammadTicket ticket)
        {
            // Get all Articles for Ticket
            ZammadTicketArticle[]? articles = await GetArticlesByTicket(client, ticket);
            if (articles == null || articles.Length == 0)
            {
                Console.WriteLine($"No ticket articles found for ticket {ticket.id}.");
                return;
            }

            // Check for already done winmail.dat attachments
            List<int> alreadyDone = new List<int>();
            foreach (ZammadTicketArticle? article in articles.ToList().FindAll(x => (x.subject ?? "").StartsWith("winmail.dat|") && x.isinternal))
            {
                int articleId = int.Parse(article.subject!.Split('|')[1]);
                alreadyDone.Add(articleId);
            }

            // Check for winmail.dat attachments
            foreach (ZammadTicketArticle? article in articles.ToList().FindAll(x => x.created_at > lastCheck && (x.attachments?.Any(x => x.filename == "winmail.dat") ?? false)))
            {
                await HandleArticleWithWinmailDat(client, ticket, alreadyDone, article);
            }
        }

        /// <summary>
        /// Handle an Article with winmail.dat attachments.
        /// </summary>
        /// <returns></returns>
        private static async Task HandleArticleWithWinmailDat(HttpClient client, ZammadTicket ticket, List<int> alreadyDone, ZammadTicketArticle article)
        {
            if (alreadyDone.Contains(article.id))
            {
                Console.WriteLine($"winmail.dat attachments for article {article.id} already processed.");
                return;
            }

            ZammadAttachment? winmailDatAttachment = article.attachments?.FirstOrDefault(a => a.filename == "winmail.dat");
            if (winmailDatAttachment == null)
            {
                Console.WriteLine($"No winmail.dat attachment found for article {article.id}.");
                return;
            }
            TnefReader winmailDatReader = await GetTnefReaderByAttachment(client, ticket, article, winmailDatAttachment);
            List<ZammadAttachmentCreate> attachments = GetAttachmentsFromTnefReader(article, winmailDatReader);

            if (attachments.Count > 0)
            {
                await CreateNewArticleWithAttachments(client, ticket, article, attachments);
            }
        }

        /// <summary>
        /// Get all Articles for a Ticket.
        /// </summary>
        /// <returns></returns>
        private static async Task<ZammadTicketArticle[]?> GetArticlesByTicket(HttpClient client, ZammadTicket ticket)
        {
            HttpResponseMessage articlesresponse = await client.GetAsync($"{zammadhost}/api/v1/ticket_articles/by_ticket/{ticket.id}");
            articlesresponse.EnsureSuccessStatusCode();
            string articlescontent = await articlesresponse.Content.ReadAsStringAsync();
            ZammadTicketArticle[]? articles = System.Text.Json.JsonSerializer.Deserialize<ZammadTicketArticle[]>(articlescontent);
            return articles;
        }

        /// <summary>
        /// Download the winmail.dat attachment and create a TnefReader.
        /// </summary>
        /// <returns></returns>
        private static async Task<TnefReader> GetTnefReaderByAttachment(HttpClient client, ZammadTicket ticket, ZammadTicketArticle article, ZammadAttachment winmailDatAttachment)
        {
            HttpResponseMessage winmailDatResponse = await client.GetAsync($"{zammadhost}/api/v1/ticket_attachment/{ticket.id}/{article.id}/{winmailDatAttachment.id}");
            winmailDatResponse.EnsureSuccessStatusCode();
            byte[] winmailDatContent = await winmailDatResponse.Content.ReadAsByteArrayAsync();
            MemoryStream memoryStream = new MemoryStream(winmailDatContent);
            TnefReader winmailDatReader = new TnefReader(memoryStream, 65001, TnefComplianceMode.Loose);
            return winmailDatReader;
        }

        /// <summary>
        /// Get all Attachments from a winmail.dat.
        /// </summary>
        /// <returns></returns>
        private static List<ZammadAttachmentCreate> GetAttachmentsFromTnefReader(ZammadTicketArticle article, TnefReader winmailDatReader)
        {
            List<ZammadAttachmentCreate> attachments = new List<ZammadAttachmentCreate>();
            string? filename = null;
            DateTime creationDate = DateTime.MinValue;
            DateTime modificationTime = DateTime.MinValue;
            byte[]? content = null;

            while (winmailDatReader.ReadNextAttribute())
            {
                if (winmailDatReader.AttributeTag == TnefAttributeTag.AttachCreateDate)
                {
                    creationDate = winmailDatReader.TnefPropertyReader.ReadValueAsDateTime();
                }
                if (winmailDatReader.AttributeTag == TnefAttributeTag.AttachModifyDate)
                {
                    modificationTime = winmailDatReader.TnefPropertyReader.ReadValueAsDateTime();
                }
                if (winmailDatReader.AttributeTag == TnefAttributeTag.AttachData)
                {
                    content = winmailDatReader.TnefPropertyReader.ReadValueAsBytes();
                }
                if (winmailDatReader.AttributeTag == TnefAttributeTag.Attachment)
                {
                    while (winmailDatReader.TnefPropertyReader.ReadNextProperty())
                    {
                        switch (winmailDatReader.TnefPropertyReader.PropertyTag.Id)
                        {
                            case TnefPropertyId.AttachLongFilename:
                                filename = winmailDatReader.TnefPropertyReader.ReadValueAsString();
                                break;
                            case TnefPropertyId.AttachFilename:
                                filename ??= winmailDatReader.TnefPropertyReader.ReadValueAsString();
                                break;
                        }
                    }
                }
                if (creationDate != DateTime.MinValue && modificationTime != DateTime.MinValue && content != null && !string.IsNullOrEmpty(filename))
                {
                    Console.WriteLine($"Found attachment {filename} in winmail.dat for article {article.id}.");
                    attachments.Add(new ZammadAttachmentCreate
                    {
                        filename = filename,
                        data = Convert.ToBase64String(content),
                        mime_type = "application/octet-stream"
                    });

                    creationDate = DateTime.MinValue;
                    modificationTime = DateTime.MinValue;
                    content = null;
                    filename = null;
                }
            }

            return attachments;
        }

        /// <summary>
        /// Create a new Article with the extracted attachments from the winmail.dat.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ticket"></param>
        /// <param name="article"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        private static async Task CreateNewArticleWithAttachments(HttpClient client, ZammadTicket ticket, ZammadTicketArticle article, List<ZammadAttachmentCreate> attachments)
        {
            ZammadArticleCreate newArticle = new ZammadArticleCreate
            {
                ticket_id = ticket.id,
                to = "",
                cc = "",
                subject = "winmail.dat|" + article.id,
                body = "Inhalt der winmail.dat der Nachricht " + article.id,
                content_type = "text/plain",
                type = "note",
                isinternal = true,
                attachments = attachments
            };
            string newArticleJson = System.Text.Json.JsonSerializer.Serialize(newArticle);
            StringContent newArticleContent = new StringContent(newArticleJson, Encoding.UTF8, "application/json");
            HttpResponseMessage newArticleResponse = await client.PostAsync($"{zammadhost}/api/v1/ticket_articles", newArticleContent);
            newArticleResponse.EnsureSuccessStatusCode();
            Console.WriteLine($"Created new article for ticket {ticket.id}/{article.id} with winmail.dat attachments.");
        }

        /// <summary>
        /// Print all Attributes of a TnefReader. (Debugging)
        /// </summary>
        private static void PrintAllAttributes(TnefReader winmailDatReader)
        {
            while (winmailDatReader.ReadNextAttribute())
            {
                Console.WriteLine($"Attribute {winmailDatReader.AttributeTag}:");
                try
                {
                    object Value = winmailDatReader.TnefPropertyReader.ReadValue();

                    Console.WriteLine($"Property {winmailDatReader.TnefPropertyReader.PropertyTag.Id} ({winmailDatReader.TnefPropertyReader.RowCount}/{winmailDatReader.TnefPropertyReader.ValueCount}/{winmailDatReader.TnefPropertyReader.PropertyCount}) => {Value}");
                    if (Value.GetType() == typeof(byte[]))
                    {
                        Console.WriteLine(Convert.ToBase64String((byte[])Value));
                    }
                }
                catch
                { }

                while (winmailDatReader.TnefPropertyReader.ReadNextProperty())
                {
                    object Value = winmailDatReader.TnefPropertyReader.ReadValue();

                    Console.WriteLine($"Property {winmailDatReader.TnefPropertyReader.PropertyTag.Id} ({winmailDatReader.TnefPropertyReader.RowCount}/{winmailDatReader.TnefPropertyReader.ValueCount}/{winmailDatReader.TnefPropertyReader.PropertyCount}) => {Value}");
                    if (Value.GetType() == typeof(byte[]))
                    {
                        Console.WriteLine(Convert.ToBase64String((byte[])Value));
                    }
                }
            }
        }
    }
}
