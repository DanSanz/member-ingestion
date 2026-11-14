using MemberIngestion.Ingestion;
using MemberIngestion.Members;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scheme.Common.Audit;

namespace MemberIngestion.TestSupport;

/// <summary>
/// The service wired as the job wires it, with in-memory audit, store and logs. Resolve what you
/// test from it, so a new constructor dependency does not break existing tests.
/// </summary>
public sealed class IngestionHarness : IDisposable
{
    public const string AuditActor = "svc-member-ingestion";

    private readonly ServiceProvider provider;

    public IngestionHarness(bool strictSchema = true)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace).AddProvider(Logs));
        services.AddSingleton<IAuditLog>(Audit);
        services.AddSingleton<IMemberStore>(Store);
        services.AddSingleton(Register);
        services.Configure<IngestionOptions>(o =>
        {
            o.AuditActor = AuditActor;
            o.StrictSchema = strictSchema;
        });
        services.AddMemberIngestion();
        provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }

    public InMemoryAuditLog Audit { get; } = new();

    public InMemoryMemberStore Store { get; } = new();

    public FileRegister Register { get; } = new();

    /// <summary>Everything logged at any level, with exceptions.</summary>
    public CapturingLoggerProvider Logs { get; } = new();

    public IngestionOptions Options => Get<IOptions<IngestionOptions>>().Value;

    public MemberFileIngestor Ingestor => Get<MemberFileIngestor>();

    public T Get<T>() where T : notnull => provider.GetRequiredService<T>();

    public void Dispose() => provider.Dispose();
}
