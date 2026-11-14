using System.Globalization;
using Scheme.Common.Dates;

namespace Scheme.Common.Feeds;

/// <summary>The HDR line of an employer feed file.</summary>
public sealed record FeedHeader(string FeedType, string SenderId, string FileId, DateOnly FileDate, string ContractVersion);

/// <summary>A whole feed file: its header and the fields of each DTL line, trimmed, in file order.</summary>
public sealed record FeedFile(FeedHeader Header, IReadOnlyList<IReadOnlyList<string>> Details);

/// <summary>
/// The file breaks the shared envelope. Messages give line numbers and counts, never field values,
/// because detail lines carry personal data.
/// </summary>
public sealed class FeedFormatException(string message) : Exception(message);

/// <summary>
/// Reads the envelope shared by every employer feed:
/// <c>HDR|type|sender|file id|DD/MM/YYYY|contract</c>, then <c>DTL|...</c> lines, then <c>TRL|count</c>.
/// </summary>
public static class FeedReader
{
    public const char Separator = '|';

    public static FeedHeader ReadHeader(string path)
    {
        using var reader = new StreamReader(path);
        return ParseHeader(reader.ReadLine());
    }

    public static FeedFile Read(string path)
    {
        var lines = File.ReadAllLines(path).Where(line => line.Length > 0).ToList();
        var header = ParseHeader(lines.FirstOrDefault());
        if (lines.Count < 2 || !lines[^1].StartsWith("TRL|", StringComparison.Ordinal))
        {
            throw new FeedFormatException($"{header.FileId}: the last line is not a TRL trailer");
        }

        var details = new List<IReadOnlyList<string>>(lines.Count - 2);
        for (var i = 1; i < lines.Count - 1; i++)
        {
            var fields = Split(lines[i]);
            if (fields[0] != "DTL")
            {
                throw new FeedFormatException($"{header.FileId}: line {i + 1} is not a DTL line");
            }
            details.Add(fields[1..]);
        }

        var trailer = Split(lines[^1]);
        if (trailer.Length != 2 || !int.TryParse(trailer[1], NumberStyles.None, CultureInfo.InvariantCulture, out var declared) || declared != details.Count)
        {
            throw new FeedFormatException(
                $"{header.FileId}: the trailer does not match the {details.Count} detail lines");
        }
        return new FeedFile(header, details);
    }

    private static FeedHeader ParseHeader(string? line)
    {
        var fields = line is null ? [] : Split(line);
        if (fields.Length != 6 || fields[0] != "HDR")
        {
            throw new FeedFormatException("The first line is not an HDR header with 6 fields");
        }
        if (!UkDate.TryParse(fields[4], out var fileDate))
        {
            throw new FeedFormatException($"{fields[3]}: the header file date is not DD/MM/YYYY");
        }
        return new FeedHeader(fields[1], fields[2], fields[3], fileDate, fields[5]);
    }

    private static string[] Split(string line) =>
        line.Split(Separator, StringSplitOptions.TrimEntries);
}
