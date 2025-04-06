using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DDDProject.Infrastructure.Services;

public class LoggerEmailSender : IEmailSender
{
    private readonly ILogger<LoggerEmailSender> _logger;

    public LoggerEmailSender(ILogger<LoggerEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogWarning("--- SIMULATING EMAIL SEND ---");
        _logger.LogInformation("To: {Email}", email);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("Body: {HtmlMessage}", htmlMessage);
        _logger.LogWarning("--- END EMAIL SIMULATION ---");

        // In a real implementation, you would use an email client library here.
        return Task.CompletedTask;
    }
} 