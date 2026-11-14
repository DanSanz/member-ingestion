// The nightly member job: loads each employer member file named on the command line.
//   dotnet run --project src/MemberIngestion.Job -- <file> [<file> ...]
// Exit code 1 if any file was rejected.

using MemberIngestion;
using MemberIngestion.Ingestion;
using MemberIngestion.Members;
using MemberIngestion.Reports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scheme.Common.Audit;
using Scheme.Common.Feeds;

// appsettings*.json are read from the build output, so the job runs the same from any folder.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });
var config = builder.Configuration;

builder.Services.AddOptions<IngestionOptions>()
    .Bind(config.GetSection(IngestionOptions.Section))
    .Validate(o => o.AuditActor.Length > 0, "Ingestion:AuditActor is required");

builder.Services.AddSingleton<IMemberStore>(new JsonFileMemberStore(config["Local:MemberStorePath"] ?? "local/members.json"));
builder.Services.AddSingleton(new FileRegister(config["Local:FileRegisterPath"] ?? "local/file-register.json"));
if (config["AuditService:BaseUrl"] is { Length: > 0 } auditUrl)
{
    var client = new HttpClient { BaseAddress = new Uri(auditUrl) };
    client.DefaultRequestHeaders.Add("X-Api-Key", config["AuditService:ApiKey"]);
    builder.Services.AddSingleton<IAuditLog>(new HttpAuditLog(client));
}
else
{
    builder.Services.AddSingleton<IAuditLog>(new JsonLinesAuditLog(config["Local:AuditLogPath"] ?? "local/audit.jsonl"));
}
builder.Services.AddMemberIngestion();

using var host = builder.Build();
var ingestor = host.Services.GetRequiredService<MemberFileIngestor>();
var members = host.Services.GetRequiredService<MemberService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

var rejected = 0;
foreach (var path in args)
{
    try
    {
        var summary = ingestor.IngestFile(path);
        Console.WriteLine(IngestReport.For(summary));
        Console.WriteLine(JoinersReport.For(members.Everyone(), summary.EmployerId, summary.FileDate));
        Console.WriteLine(LongServiceReport.For(members.Everyone(), summary.EmployerId, summary.FileDate));
        Console.WriteLine(AgeProfileReport.For(members.Everyone(), summary.EmployerId, summary.FileDate));
    }
    catch (FeedFormatException ex)
    {
        logger.LogError("Rejected {File}: {Reason}", Path.GetFileName(path), ex.Message);
        rejected++;
    }
}
return rejected > 0 ? 1 : 0;
