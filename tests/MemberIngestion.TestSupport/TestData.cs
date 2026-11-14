using System.Globalization;
using MemberIngestion.Generated;

namespace MemberIngestion.TestSupport;

/// <summary>Builds in-memory member records. Every default value is synthetic.</summary>
public static class MemberRecords
{
    public static MemberRecord Make(
        string employerId = "E90001",
        string memberRef = "M900001",
        string niNumber = "QQ100001A",
        string surname = "Synthetic",
        string forenames = "Alex",
        string dateOfBirth = "14/03/1981",
        string dateJoined = "01/09/2015",
        string salary = "41250.00") =>
        new(employerId, memberRef, niNumber, surname, forenames, dateOfBirth, dateJoined, salary);
}

/// <summary>The synthetic files in <c>tests/fixtures</c>, copied next to each test assembly.</summary>
public static class Fixtures
{
    public static string Folder => Path.Combine(AppContext.BaseDirectory, "fixtures");

    public static string PathTo(string fileName) => Path.Combine(Folder, fileName);
}

/// <summary>
/// Runs the code inside a <c>using</c> block as if on a machine set to <paramref name="name"/>,
/// for example "en-US", then puts the culture back.
/// </summary>
public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo uiCulture = CultureInfo.CurrentUICulture;

    public CultureScope(string name)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;
    }
}
