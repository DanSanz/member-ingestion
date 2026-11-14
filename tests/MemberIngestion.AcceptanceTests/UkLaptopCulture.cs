using System.Globalization;
using System.Runtime.CompilerServices;

namespace MemberIngestion.AcceptanceTests;

/// <summary>
/// Every acceptance test runs as on a scheme laptop (en-GB), whatever machine runs it, so a result means
/// the same on every machine. A test that must also hold on an en-US machine (our build agents) says
/// so and switches culture with <see cref="CultureScope"/>.
/// </summary>
internal static class UkLaptopCulture
{
    [ModuleInitializer]
    internal static void Pin()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-GB");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-GB");
    }
}
