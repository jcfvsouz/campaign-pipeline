namespace Promo.Worker.Application.Events;

public record CampaignPublished(
    Guid CampaignId,
    Guid TenantId,
    Guid BuyerGroupId,
    string Name,
    DateTime PublishedAtUtc);
