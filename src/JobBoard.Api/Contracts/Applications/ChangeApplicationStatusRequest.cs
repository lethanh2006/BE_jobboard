using JobBoard.Domain.Entities;

namespace JobBoard.Api.Contracts.Applications;

public sealed record ChangeApplicationStatusRequest(int CompanyId, ApplicationStatus Status);
