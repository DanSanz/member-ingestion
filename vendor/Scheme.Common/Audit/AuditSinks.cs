using System.Net.Http.Json;
using System.Text.Json;

namespace Scheme.Common.Audit;

/// <summary>Appends one JSON object per event to a file. For local runs.</summary>
public sealed class JsonLinesAuditLog(string path, TimeProvider? clock = null) : AuditLogBase(clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Lock gate = new();

    protected override void Write(AuditEvent auditEvent)
    {
        var line = JsonSerializer.Serialize(auditEvent, Json) + Environment.NewLine;
        lock (gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.AppendAllText(path, line);
        }
    }
}

/// <summary>
/// Sends each event to the scheme's audit service. Production and the shared environments use this; the
/// endpoint and key come from configuration (<c>AuditService</c> section), never from code.
/// </summary>
public sealed class HttpAuditLog(HttpClient client, TimeProvider? clock = null) : AuditLogBase(clock)
{
    protected override void Write(AuditEvent auditEvent)
    {
        using var response = client.PostAsJsonAsync("events", auditEvent).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
    }
}
