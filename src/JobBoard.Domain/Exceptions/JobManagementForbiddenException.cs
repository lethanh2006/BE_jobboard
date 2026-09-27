namespace JobBoard.Domain.Exceptions;

public sealed class JobManagementForbiddenException(string message) : DomainException(message);
