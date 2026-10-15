using Components.Messaging;
using Microsoft.Extensions.Logging;
using Promo.Worker.Application.Events;
using Promo.Worker.Domain.Services;

namespace Promo.Worker.Application.Handlers;

public class CampaignPublishedHandler(ISendCampaignEmailsService sendCampaignEmailsService, ILogger<CampaignPublishedHandler> logger) : IEventHandler<CampaignPublished>
{
    public async Task HandleAsync(CampaignPublished @event, CancellationToken cancellationToken = default)
    {
        var sentCount = await sendCampaignEmailsService.SendAsync(@event.BuyerGroupId, @event.Name, cancellationToken);
        logger.LogInformation("Dispatched campaign {CampaignName} ({CampaignId}) to {BuyerCount} buyers.", @event.Name, @event.CampaignId, sentCount);
    }
}
