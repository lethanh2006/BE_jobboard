namespace JobBoard.Application.Exceptions;

public sealed class InvalidRefreshTokenException(string message) : Exception(message);
