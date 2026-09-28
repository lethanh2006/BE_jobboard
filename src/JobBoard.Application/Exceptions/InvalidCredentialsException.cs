namespace JobBoard.Application.Exceptions;

public sealed class InvalidCredentialsException(string message) : Exception(message);
