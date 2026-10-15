namespace Promo.Worker.Domain.Email;

public record EmailMessage(string ToEmail, string ToName, string Subject, string Body);
