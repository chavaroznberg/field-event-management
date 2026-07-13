namespace FieldEvents.Domain.Exceptions;

/// <summary>
/// Base class for all domain rule violations.
/// Catching this type separates business errors from unexpected system errors.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
