using Microsoft.AspNetCore.Identity;
using VetDesk.Application.Common.Interfaces;

namespace VetDesk.Infrastructure.Services;

/// <summary>ASP.NET Core Identity's hasher (PBKDF2, salted, versioned format) without the rest of Identity.</summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();
    private static readonly object User = new();

    public string Hash(string password) => _inner.HashPassword(User, password);

    public bool Verify(string hash, string password)
    {
        if (string.IsNullOrEmpty(hash))
            return false;
        try
        {
            return _inner.VerifyHashedPassword(User, hash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // A corrupt hash means nobody can sign in with it; treat it as a wrong password rather than crash sign-in.
            return false;
        }
    }
}
