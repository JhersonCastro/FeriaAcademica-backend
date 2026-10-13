using Mediator;

namespace FeriaAcademica.Application.Features.Auth.Commands.ResetPassword;

/// <summary>Sets a new password from an emailed link: both "forgot password" and the first password of an account created by the super admin.</summary>
public sealed record ResetPasswordCommand(Guid UserId, string Token, string NewPassword) : ICommand<Unit>;
