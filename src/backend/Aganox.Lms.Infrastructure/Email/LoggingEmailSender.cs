using Aganox.Lms.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aganox.Lms.Infrastructure.Email;

public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email to {To} | Subject: {Subject} | Body: {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
