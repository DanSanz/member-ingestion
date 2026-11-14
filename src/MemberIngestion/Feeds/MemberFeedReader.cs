using MemberIngestion.Generated;
using MemberIngestion.Ingestion;
using Microsoft.Extensions.Options;
using Scheme.Common.Feeds;

namespace MemberIngestion.Feeds;

/// <summary>An employer member file: its header and one record per detail line, in file order.</summary>
public sealed record MemberFile(FeedHeader Header, IReadOnlyList<MemberRecord> Rows);

/// <summary>Reads MEMBERS files. All file parsing lives here and in <see cref="FeedReader"/>.</summary>
public sealed class MemberFeedReader(IOptions<IngestionOptions> options)
{
    public MemberFile Read(string path)
    {
        var file = FeedReader.Read(path);
        var header = file.Header;
        if (header.FeedType != MemberRecord.FeedType)
        {
            throw new FeedFormatException($"{header.FileId} is a {header.FeedType} file, not {MemberRecord.FeedType}");
        }

        var rows = new List<MemberRecord>(file.Details.Count);
        for (var i = 0; i < file.Details.Count; i++)
        {
            var fields = file.Details[i];
            var tooFew = fields.Count < MemberRecord.FieldCount;
            var tooMany = fields.Count > MemberRecord.FieldCount && options.Value.StrictSchema;
            if (tooFew || tooMany)
            {
                throw new FeedFormatException(
                    $"{header.FileId}: detail line {i + 1} has {fields.Count} fields, the contract has {MemberRecord.FieldCount}");
            }
            rows.Add(MemberRecord.FromFields(fields));
        }
        return new MemberFile(header, rows);
    }
}
