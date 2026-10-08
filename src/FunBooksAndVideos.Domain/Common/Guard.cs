namespace FunBooksAndVideos.Domain.Common;

/// <summary>Reusable precondition checks that translate into <see cref="DomainValidationException"/>.</summary>
public static class Guard
{
    /// <summary>Returns the trimmed value or throws when it is blank or longer than <paramref name="maxLength"/>.</summary>
    public static string RequiredText(string? value, int maxLength, string code, string fieldDescription)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(code, $"{fieldDescription} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainValidationException(code, $"{fieldDescription} must not exceed {maxLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Returns the trimmed value, <see langword="null"/> for blank input, or throws when too long.</summary>
    public static string? OptionalText(string? value, int maxLength, string code, string fieldDescription)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainValidationException(code, $"{fieldDescription} must not exceed {maxLength} characters.");
        }

        return trimmed;
    }

    /// <summary>
    /// Returns the value or throws when it equals <c>default(T)</c>. Used for strongly typed ids, whose
    /// constructor validation is bypassed by <c>default</c>.
    /// </summary>
    public static T NotDefault<T>(T value, string code, string description)
        where T : struct, IEquatable<T>
    {
        if (value.Equals(default))
        {
            throw new DomainValidationException(code, $"{description} must be specified.");
        }

        return value;
    }

    /// <summary>Throws when the enum value is not one of the declared members.</summary>
    public static TEnum DefinedEnum<TEnum>(TEnum value, string code)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainValidationException(code, $"{value} is not a valid {typeof(TEnum).Name}.");
        }

        return value;
    }
}
