using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Scheme.Common.Dates;

/// <summary>
/// Strict DD/MM/YYYY dates, the format of every employer feed of the scheme.
/// </summary>
/// <remarks>
/// Nothing here reads the machine's culture. <see cref="DateTime.Parse(string)"/> and
/// <see cref="DateTime.TryParse(string, out DateTime)"/> do: on an en-GB machine "02/11/2026" is
/// 2 November, on an en-US machine it is 11 February. Since 4.0 this namespace also holds what was in
/// <c>Scheme.Common.Calendar</c>, which was removed.
/// </remarks>
public static partial class UkDate
{
    public const string Format = "dd/MM/yyyy";

    /// <summary>Parses two-digit day, two-digit month, four-digit year, and nothing else.</summary>
    /// <exception cref="InvalidDateException">
    /// The text is not a real DD/MM/YYYY date. The message never repeats the text, because the text may be
    /// personal data (a date of birth, for example).
    /// </exception>
    public static DateOnly Parse(string? text) =>
        TryParse(text, out var value) ? value : throw new InvalidDateException();

    /// <summary>Like <see cref="Parse"/>, but returns false instead of throwing.</summary>
    public static bool TryParse([NotNullWhen(true)] string? text, out DateOnly value)
    {
        value = default;
        return text is not null
            && Shape().IsMatch(text)
            && DateOnly.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    /// <summary>Formats <paramref name="value"/> as DD/MM/YYYY.</summary>
    public static string ToUkString(this DateOnly value) => value.ToString(Format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Age in whole years on <paramref name="on"/>. Someone born on 29 February has their birthday on
    /// 1 March in years that are not leap years.
    /// </summary>
    public static int AgeOn(DateOnly born, DateOnly on)
    {
        var years = on.Year - born.Year;
        if ((on.Month, on.Day).CompareTo((born.Month, born.Day)) < 0)
        {
            years--;
        }
        return years;
    }

    [GeneratedRegex("^[0-9]{2}/[0-9]{2}/[0-9]{4}$")]
    private static partial Regex Shape();
}

/// <summary>Text that is not a real DD/MM/YYYY date. The message never repeats the text.</summary>
public sealed class InvalidDateException() : FormatException("Not a real date in DD/MM/YYYY form.");
