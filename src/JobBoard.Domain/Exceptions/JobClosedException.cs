namespace JobBoard.Domain.Exceptions;

public sealed class JobClosedException(string message) : DomainException(message);
