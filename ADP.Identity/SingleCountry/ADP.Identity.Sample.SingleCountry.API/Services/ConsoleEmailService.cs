using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;

namespace ShiftSoftware.ADP.Identity.Sample.SingleCountry.API.Services;

// The sample sends no email: verification and reset links are written to the log.
public class ConsoleEmailService(ILogger<ConsoleEmailService> logger) : ISendEmailVerification, ISendEmailResetPassword
{
    public Task SendEmailResetPasswordAsync(string url, UserDataDTO user)
    {
        logger.LogInformation("Password reset for {Email}: {Url}", user.Email, url);
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(string url, UserDataDTO user)
    {
        logger.LogInformation("Email verification for {Email}: {Url}", user.Email, url);
        return Task.CompletedTask;
    }
}
