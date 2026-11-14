using MemberIngestion.Feeds;
using MemberIngestion.Members;
using MemberIngestion.Reports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scheme.Common.Audit;

namespace MemberIngestion.Ingestion;

public sealed record IngestSummary(string FileId, string EmployerId, DateOnly FileDate, int RowsRead, int Loaded)
{
    public override string ToString() => IngestReport.For(this);
}

/// <summary>The nightly member load: one employer member file into the member store, audited.</summary>
public sealed partial class MemberFileIngestor(
    MemberFeedReader reader,
    IMemberStore store,
    IAuditLog audit,
    FileRegister register,
    IOptions<IngestionOptions> options,
    ILogger<MemberFileIngestor> logger)
{
    /// <exception cref="Scheme.Common.Feeds.FeedFormatException">The file breaks the envelope or the contract. Nothing is loaded.</exception>
    public IngestSummary IngestFile(string path)
    {
        var file = reader.Read(path);
        var header = file.Header;

        foreach (var row in file.Rows)
        {
            store.Upsert(Member.FromRecord(row, header.FileId));
        }
        var loaded = file.Rows.Count;

        audit.Record(
            "member_file_loaded",
            header.FileId,
            $"{loaded} of {file.Rows.Count} member rows loaded",
            actor: options.Value.AuditActor);
        var daysSinceLast = register.DaysSinceLatest(header.SenderId);
        if (daysSinceLast >= 0)
        {
            LogSinceLast(header.SenderId, daysSinceLast);
        }
        register.Record(header);
        LogLoaded(loaded, file.Rows.Count, header.FileId);
        return new IngestSummary(header.FileId, header.SenderId, header.FileDate, file.Rows.Count, loaded);
    }

    // Counts and file IDs only: rows carry personal data.
    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded {Loaded} of {RowsRead} member rows from {FileId}")]
    private partial void LogLoaded(int loaded, int rowsRead, string fileId);

    [LoggerMessage(Level = LogLevel.Information, Message = "The last file from {EmployerId} was received {Days} days ago")]
    private partial void LogSinceLast(string employerId, int days);
}
