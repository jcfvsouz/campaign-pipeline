namespace Promo.Worker.Infrastructure.Email;

public class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string From { get; set; } = "noreply@campaign-pipeline.local";
}
