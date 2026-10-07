using Microsoft.AspNetCore.Identity;

namespace NotesAPI.Services;

public class PasswordService
{
    private readonly PasswordHasher<object> _hasher = new();

    public string HashPassword(string password)
    {
        // Store a secure hash instead of the original password.
        return _hasher.HashPassword(
            null!,
            password);
    }

    public bool VerifyPassword(
        string password,
        string passwordHash)
    {
        // Compare the entered password with the stored hash.
        var result = _hasher.VerifyHashedPassword(
            null!,
            passwordHash,
            password);

        // SuccessRehashNeeded is also valid because the hash should be upgraded later.
        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}