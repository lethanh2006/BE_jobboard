namespace JobBoard.Domain.Exceptions;

public sealed class InvalidJobStateException(string message) : DomainException(message);