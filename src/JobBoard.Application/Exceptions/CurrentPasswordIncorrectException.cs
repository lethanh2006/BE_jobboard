namespace JobBoard.Application.Exceptions;

public sealed class CurrentPasswordIncorrectException(string message) : Exception(message);
