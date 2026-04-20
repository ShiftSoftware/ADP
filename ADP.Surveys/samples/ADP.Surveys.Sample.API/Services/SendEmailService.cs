using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;

namespace ShiftSoftware.ADP.Surveys.Sample.API.Services;

/// <summary>
/// Console-logging stub — ShiftIdentity-internal hosting requires these interfaces to
/// be registered; real consumer apps wire SendGrid / SMTP / whatever they use here.
/// </summary>
public class SendEmailService : ISendEmailVerification, ISendEmailResetPassword
{
    public Task SendEmailResetPasswordAsync(string url, UserDataDTO user)
    {
        Console.WriteLine($"[Surveys.Sample] Password reset link for {user.Email}: {url}");
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(string url, UserDataDTO user)
    {
        Console.WriteLine($"[Surveys.Sample] Email verification link for {user.Email}: {url}");
        return Task.CompletedTask;
    }
}
