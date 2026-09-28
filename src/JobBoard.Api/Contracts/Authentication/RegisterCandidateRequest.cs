namespace JobBoard.Api.Contracts.Authentication;

public sealed record RegisterCandidateRequest(string Name, string Email, string Password);
