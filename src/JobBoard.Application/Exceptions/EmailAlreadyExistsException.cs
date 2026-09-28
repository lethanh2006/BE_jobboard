namespace JobBoard.Application.Exceptions;

public sealed class EmailAlreadyExistsException(string message) : Exception(message);
