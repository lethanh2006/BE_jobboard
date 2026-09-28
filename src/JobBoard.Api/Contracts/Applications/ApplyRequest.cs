namespace JobBoard.Api.Contracts.Applications;

public sealed record ApplyRequest(int CandidateId, int JobId);
