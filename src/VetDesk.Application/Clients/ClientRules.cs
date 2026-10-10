using VetDesk.Application.Common;
using VetDesk.Domain.Entities.Clients;

namespace VetDesk.Application.Clients;

internal static class ClientRules
{
    public static string? Validate(string lastName, string phone, string? email)
    {
        if (!Text.Has(lastName))
            return "Enter the owner's name.";
        if (Client.DigitsOf(phone).Length < 10)
            return "Enter a phone number with at least 10 digits.";
        if (Text.Has(email) && (!email!.Contains('@') || email.Trim().Contains(' ')))
            return "That email address doesn't look right.";
        return null;
    }
}
