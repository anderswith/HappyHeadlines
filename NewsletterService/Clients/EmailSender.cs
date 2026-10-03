using System.Diagnostics;
using System.Net.Mail;
using Monitoring;
using NewsletterService.Clients.Interfaces;

namespace NewsletterService.Clients;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "Newsletter.SendEmail",
                ActivityKind.Client);

        var host = _configuration["Smtp:Host"]
                   ?? throw new InvalidOperationException(
                       "Missing SMTP host.");

        var port = _configuration.GetValue<int>("Smtp:Port");
        var from = _configuration["Smtp:From"]
                   ?? "news@happyheadlines.test";

        activity?.SetTag("server.address", host);
        activity?.SetTag("server.port", port);

        using var mail = new MailMessage(
            from,
            recipient,
            subject,
            body);

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = false,
            UseDefaultCredentials = false
        };

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        try
        {
            await smtp.SendMailAsync(mail, timeout.Token);

            MonitorService.Log.Information(
                "Newsletter email accepted by SMTP server");
        }
        catch (Exception exception)
        {
            activity?.SetStatus(
                ActivityStatusCode.Error,
                exception.Message);

            throw;
        }
    }
}