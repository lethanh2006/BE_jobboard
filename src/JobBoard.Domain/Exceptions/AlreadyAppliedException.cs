namespace JobBoard.Domain.Exceptions;

public sealed class AlreadyAppliedException(string message) : DomainException(message);
