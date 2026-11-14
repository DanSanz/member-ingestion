using MemberIngestion.Feeds;
using MemberIngestion.Ingestion;
using MemberIngestion.Members;
using Microsoft.Extensions.DependencyInjection;

namespace MemberIngestion;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ingestion services. The host registers <c>IMemberStore</c>, <c>IAuditLog</c>, the
    /// <see cref="FileRegister"/>, logging and <see cref="IngestionOptions"/>. The test harness registers in-memory ones.
    /// </summary>
    public static IServiceCollection AddMemberIngestion(this IServiceCollection services)
    {
        services.AddSingleton<MemberFeedReader>();
        services.AddSingleton<MemberFileIngestor>();
        services.AddSingleton<MemberService>();
        return services;
    }
}
