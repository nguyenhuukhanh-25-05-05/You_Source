using FluentValidation;

namespace AppApi.DTOs;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string? RefreshToken { get; set; }
}

public record AuthResult(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    DateTime RefreshExpiresAt,
    string Username,
    string FullName,
    IList<string> Roles);

public class AuthResponse
{
    public DateTime ExpiresAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class UserInfo
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(100);
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50)
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Username contains invalid characters");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain a digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a non-alphanumeric character");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
    }
}
