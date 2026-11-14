using System.Text.Json;
using Scheme.Common.Feeds;

namespace MemberIngestion.Ingestion;

/// <summary>One file drop: the file, its employer, the date in its header and the day it arrived.</summary>
public sealed record FileReceipt(string FileId, string EmployerId, DateOnly FileDate, DateOnly ReceivedOn);

/// <summary>Remembers each file drop the job has loaded. A refused file is never recorded.</summary>
/// <remarks>
/// Receipts are kept in a JSON file, so that tonight's run knows what earlier runs loaded. A rerun
/// of a file replaces its receipt. With no path they are kept in memory, for tests and dry runs.
/// </remarks>
public sealed class FileRegister(string? path = null)
{
    private readonly List<FileReceipt> receipts = Load(path);

    public FileReceipt Record(FeedHeader header)
    {
        var receipt = new FileReceipt(header.FileId, header.SenderId, header.FileDate, DateOnly.FromDateTime(DateTime.Now));
        receipts.RemoveAll(r => r.FileId == receipt.FileId);
        receipts.Add(receipt);
        Save();
        return receipt;
    }

    public FileReceipt? LatestFor(string employerId) => receipts.LastOrDefault(r => r.EmployerId == employerId);

    public int DaysSinceLatest(string employerId) =>
        LatestFor(employerId) is { } latest
            ? DateOnly.FromDateTime(DateTime.Now).DayNumber - latest.ReceivedOn.DayNumber
            : -1;

    private void Save()
    {
        if (path is null)
        {
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(receipts));
    }

    private static List<FileReceipt> Load(string? path) =>
        path is not null && File.Exists(path)
            ? JsonSerializer.Deserialize<List<FileReceipt>>(File.ReadAllText(path)) ?? []
            : [];
}
