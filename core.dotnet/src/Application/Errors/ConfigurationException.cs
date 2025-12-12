using FluentValidation.Results;

namespace Subscrio.Core.Application.Errors;

/// <summary>
/// Configuration error
/// </summary>
public class ConfigurationException : Exception
{
    public IEnumerable<ValidationFailure>? Errors { get; }

    public ConfigurationException(string message) : base(message)
    {
    }

    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ConfigurationException(string message, IEnumerable<ValidationFailure> errors) : base(message)
    {
        Errors = errors;
    }
}

