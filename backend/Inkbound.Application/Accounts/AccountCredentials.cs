namespace Inkbound.Application.Accounts;

public sealed record AccountCredentials(long Id, string Username, string PasswordHash);
