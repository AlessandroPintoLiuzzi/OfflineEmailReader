namespace OfflineEmailManager.Model
{
    // This class represents a single email record in our database.
    public class Email
    {
        public int Id { get; set; } // Primary Key for the database
        public string Subject { get; set; }
        public string From { get; set; }
        public DateTime Date { get; set; }
        public string BodyHtml { get; set; } // We'll store the HTML body
        public string BodyText { get; set; } // And the plain text body
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
