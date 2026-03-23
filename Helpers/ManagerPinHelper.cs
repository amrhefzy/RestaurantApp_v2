using System.Text.RegularExpressions;

namespace RestaurantMS.Helpers;

/// <summary>
/// Requires NuGet: BCrypt.Net-Next
/// </summary>
public static class ManagerPinHelper
{
    private static readonly Regex _pinRegex = new(@"^\d{4}$", RegexOptions.Compiled);

    /// <summary>Returns a BCrypt hash of the plain-text PIN.</summary>
    public static string HashPin(string pin)
    {
        if (!IsValidPin(pin))
            throw new ArgumentException("PIN must be exactly 4 digits.", nameof(pin));

        return BCrypt.Net.BCrypt.HashPassword(pin, workFactor: 11);
    }

    /// <summary>Verifies a plain-text PIN against a stored BCrypt hash.</summary>
    public static bool VerifyPin(string pin, string hash)
    {
        if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(pin, hash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Returns true if the PIN is exactly 4 numeric digits.</summary>
    public static bool IsValidPin(string pin)
        => !string.IsNullOrWhiteSpace(pin) && _pinRegex.IsMatch(pin);
}
