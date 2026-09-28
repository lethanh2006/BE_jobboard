using System.Collections.Concurrent;
using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Authentication;
using JobBoard.Domain.Entities;

namespace JobBoard.Infrastructure.Authentication;

public sealed class InMemoryUserAccountRepository :
    IUserAccountRepository,
    IAccountRegistrationRepository
{
    private readonly ConcurrentDictionary<string, UserAccount> _accounts;
    private readonly ICandidateRepository? _candidates;
    private int _lastId;

    public InMemoryUserAccountRepository(
        IPasswordHasher passwordHasher,
        ICandidateRepository? candidates = null)
    {
        ArgumentNullException.ThrowIfNull(passwordHasher);

        const string demoPassword = "JobBoard@123";
        var accounts = new[]
        {
            new UserAccount(
                20,
                "Nguyễn Minh An",
                "candidate@jobboard.vn",
                passwordHasher.Hash(demoPassword),
                AccountRole.Candidate),
            new UserAccount(
                1,
                "Trần Bình",
                "employer@jobboard.vn",
                passwordHasher.Hash(demoPassword),
                AccountRole.Employer,
                CompanyId: 1,
                CompanyName: "Nova Technology"),
            new UserAccount(
                100,
                "Quản trị viên",
                "admin@jobboard.vn",
                passwordHasher.Hash(demoPassword),
                AccountRole.Admin)
        };

        _accounts = new ConcurrentDictionary<string, UserAccount>(
            accounts.ToDictionary(account => account.Email),
            StringComparer.OrdinalIgnoreCase);
        _candidates = candidates;
        _lastId = accounts.Max(account => account.Id);
    }

    public Task<UserAccount?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _accounts.TryGetValue(normalizedEmail, out var account);
        return Task.FromResult(account);
    }

    public Task<UserAccount?> FindByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_accounts.Values.SingleOrDefault(account => account.Id == id));
    }

    public Task<int> GetNextUserIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Interlocked.Increment(ref _lastId));
    }

    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_accounts.ContainsKey(normalizedEmail));
    }

    public async Task<bool> RegisterCandidateAsync(
        UserAccount account,
        Candidate candidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(candidate);

        if (_candidates is null)
        {
            throw new InvalidOperationException("Candidate repository chưa được cấu hình.");
        }

        if (!_accounts.TryAdd(account.Email, account))
        {
            return false;
        }

        try
        {
            await _candidates.AddAsync(candidate, cancellationToken);
            return true;
        }
        catch
        {
            _accounts.TryRemove(account.Email, out _);
            throw;
        }
    }
}
