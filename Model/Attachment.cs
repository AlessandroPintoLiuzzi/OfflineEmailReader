namespace OfflineEmailManager.Model;

public class Attachment
{
    public int Id { get; set; }
    public int EmailId { get; set; }
    public Email Email { get; set; } = null!;
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
}