using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace ThereforeWorkflowRunner;

public class WebhookController
{
    public static string GetHMAC256Hash(string webhookKey, string payload)
    {
        string base64Hash = "";
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookKey)))
        {
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
            byte[] payloadHash = hmac.ComputeHash(payloadBytes);
            base64Hash = System.Convert.ToBase64String(payloadHash);
        }
        return base64Hash;
    }
}

public class XeroEvent
{
    public string? resourceUrl { get; set; }
    public string? resourceId { get; set; }
    public DateTime eventDateUtc { get; set; }
    public string? eventType { get; set; }
    public string? eventCategory { get; set; }
    public string? tenantId { get; set; }
    public string? tenantType { get; set; }
}

public class XeroWebhookEvent
{
    public List<XeroEvent> events { get; set; } = new List<XeroEvent>();
    public int lastEventSequence { get; set; }
    public int firstEventSequence { get; set; }
    public string? entropy { get; set; }
}