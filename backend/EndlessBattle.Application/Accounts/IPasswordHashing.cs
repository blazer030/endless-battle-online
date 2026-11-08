namespace EndlessBattle.Application.Accounts;

public interface IPasswordHashing
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}
