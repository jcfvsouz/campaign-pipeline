using Promo.Worker.Domain.Email;
using Promo.Worker.Domain.Repositories;

namespace Promo.Worker.Domain.Services;

public class SendCampaignEmailsService(IBuyerRepository buyerRepository, IEmailSender emailSender) : ISendCampaignEmailsService
{
    public async Task<int> SendAsync(Guid buyerGroupId, string campaignName, CancellationToken cancellationToken = default)
    {
        var buyers = await buyerRepository.GetByBuyerGroupIdAsync(buyerGroupId, cancellationToken);

        foreach (var buyer in buyers)
        {
            var message = new EmailMessage(buyer.Email, buyer.Name, $"New campaign: {campaignName}", $"Hi {buyer.Name}, check out our new campaign: {campaignName}!");
            await emailSender.SendAsync(message, cancellationToken);
        }

        return buyers.Count;
    }
}
