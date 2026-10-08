namespace FunBooksAndVideos.Application.Exceptions;

/// <summary>Collects validation errors so that a request can report all problems at once.</summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    public bool HasErrors => _errors.Count > 0;

    public ValidationErrors Add(string member, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(member);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (!_errors.TryGetValue(member, out var messages))
        {
            messages = [];
            _errors[member] = messages;
        }

        messages.Add(message);
        return this;
    }

    public ValidationException ToException() =>
        new(_errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal));

    public void ThrowIfAny()
    {
        if (HasErrors)
        {
            throw ToException();
        }
    }
}
