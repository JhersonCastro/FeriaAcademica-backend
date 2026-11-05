using System.Net.Mail;
using System.Text.RegularExpressions;
using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Application.Features.Users;

/// <summary>Input validation shared by the user handlers (format and required fields, not business rules).</summary>
internal static partial class UserInput
{
    public static string Email(string? email)
    {
        var trimmed = email?.Trim();
        if (string.IsNullOrEmpty(trimmed)) throw new DomainException("Email is required.");
        if (!IsEmail(trimmed)) throw new DomainException("Email is not valid.");
        return trimmed;
    }

    public static UserProfileInput Profile(string? fullName, string? phoneNumber, DateOnly? birthDate, string? guardianName, string? guardianEmail)
    {
        var name = fullName?.Trim();
        if (string.IsNullOrEmpty(name)) throw new DomainException("Full name is required.");
        if (name.Length > 200) throw new DomainException("Full name cannot exceed 200 characters.");

        var phone = Optional(phoneNumber);
        if (phone is not null && !PhonePattern().IsMatch(phone)) throw new DomainException("Phone number is not valid.");

        if (birthDate is { } date && date > DateOnly.FromDateTime(DateTime.UtcNow)) throw new DomainException("Birth date cannot be in the future.");

        var guardianMail = Optional(guardianEmail);
        if (guardianMail is not null && !IsEmail(guardianMail)) throw new DomainException("Guardian email is not valid.");

        return new UserProfileInput(name, phone, birthDate, Optional(guardianName), guardianMail);
    }

    public static string Password(string? password) =>
        string.IsNullOrEmpty(password) ? throw new DomainException("Password is required.") : password;

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsEmail(string value) => MailAddress.TryCreate(value, out var address) && address.Address == value;

    // Digits with optional leading +, spaces, dashes and parentheses; 7 to 15 digits in total.
    [GeneratedRegex(@"^\+?(?:[\s\-()]*\d){7,15}[\s\-()]*$")]
    private static partial Regex PhonePattern();
}
