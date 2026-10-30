using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace CyberCacti.Services;

public static class AccountService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CyberCacti");

    private static string FilePath => Path.Combine(Folder, "accounts.json");

    public static bool Authenticate(
        string name,
        string password,
        bool register,
        out string username,
        out string message)
    {
        username = "";
        name = name.Trim();

        if (register &&
            (name.Length < 3 || name.Length > 20 ||
             name.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-')))
        {
            message = "Your username must be 3–20 characters. Use letters, numbers, _ or -.";
            return false;
        }

        if (register && password.Length < 8)
        {
            message = "Your password must contain at least 8 characters.";
            return false;
        }

        var accounts = GetAccounts(out string? readError);
        if (readError != null)
        {
            message = readError;
            return false;
        }

        var account = accounts.FirstOrDefault(a =>
            string.Equals(a.Username, name, StringComparison.OrdinalIgnoreCase));

        if (register)
        {
            if (account != null)
            {
                message = "That username is already taken. Please choose another.";
                return false;
            }

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                120000,
                HashAlgorithmName.SHA256,
                32);

            accounts.Add(new Account
            {
                Username = name,
                Salt = Convert.ToBase64String(salt),
                Hash = Convert.ToBase64String(hash)
            });

            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(accounts, JsonOptions));
            }
            catch
            {
                message = "Your account could not be saved. Please try again.";
                return false;
            }

            username = name;
            message = "Your account has been created.";
            return true;
        }

        if (account == null)
        {
            message = "We couldn't find that account on this computer.";
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(account.Salt);
            byte[] stored = Convert.FromBase64String(account.Hash);
            byte[] entered = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                120000,
                HashAlgorithmName.SHA256,
                32);

            if (!CryptographicOperations.FixedTimeEquals(stored, entered))
            {
                message = "That password isn't correct. Please try again.";
                return false;
            }
        }
        catch
        {
            message = "This account's saved details could not be read.";
            return false;
        }

        username = account.Username;
        message = "Welcome back.";
        return true;
    }

    private static List<Account> GetAccounts(out string? error)
    {
        error = null;

        try
        {
            Directory.CreateDirectory(Folder);

            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<List<Account>>(File.ReadAllText(FilePath))
                       ?? new List<Account>();
            }
        }
        catch
        {
            error = "The account file could not be read.";
        }

        return new List<Account>();
    }

    private sealed class Account
    {
        public string Username { get; set; } = "";
        public string Salt { get; set; } = "";
        public string Hash { get; set; } = "";
    }
}
