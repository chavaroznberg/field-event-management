using FieldEvents.Domain.Enums;

namespace FieldEvents.Domain.Exceptions;

public sealed class InvalidEventStatusTransitionException : DomainException
{
    public EventStatus From { get; }
    public EventStatus To { get; }

    public InvalidEventStatusTransitionException(EventStatus from, EventStatus to)
        : base($"Cannot transition event status from '{from}' to '{to}'.")
    {
        From = from;
        To = to;
    }
}
