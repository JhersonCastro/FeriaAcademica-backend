using Mediator;

namespace FeriaAcademica.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>Sends a password reset link if the email belongs to an account that may recover it. Always succeeds, so it does not reveal which emails exist.</summary>
public sealed record ForgotPasswordCommand(string Email) : ICommand<Unit>;
