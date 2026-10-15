namespace Promo.Worker.Domain.Services;

public interface ISendCampaignEmailsService
{
    Task<int> SendAsync(Guid buyerGroupId, string campaignName, CancellationToken cancellationToken = default);
}
