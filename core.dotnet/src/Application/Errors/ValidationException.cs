namespace Subscrio.Core.Application.Errors;

/// <summary>
/// Input validation error (FluentValidation validation failures)
/// </summary>
public class ValidationException : Exception
{
    public object? Errors { get; }

    public ValidationException(string message, object? errors = null) : base(message)
    {
        Errors = errors;
    }

    public ValidationException(string message, Exception innerException, object? errors = null) : base(message, innerException)
    {
        Errors = errors;
    }
}

