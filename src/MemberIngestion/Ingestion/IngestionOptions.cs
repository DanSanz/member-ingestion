namespace MemberIngestion.Ingestion;

/// <summary>The <c>Ingestion</c> section of appsettings.json.</summary>
public sealed class IngestionOptions
{
    public const string Section = "Ingestion";

    /// <summary>Service account recorded as the actor on every audit event the job raises.</summary>
    public string AuditActor { get; set; } = "";

    /// <summary>Reject detail lines with more fields than the feed contract defines.</summary>
    public bool StrictSchema { get; set; } = true;
}
