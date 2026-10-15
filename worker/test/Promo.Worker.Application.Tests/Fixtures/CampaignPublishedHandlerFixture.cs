using Microsoft.Extensions.Logging.Abstractions;
using Promo.Worker.Application.Handlers;
using Promo.Worker.Domain.Services;

namespace Promo.Worker.Application.Tests.Fixtures;

public class CampaignPublishedHandlerFixture
{
    public Mock<ISendCampaignEmailsService> SendCampaignEmailsService { get; private set; } = new();

    public void ConfigureMocks()
    {
        SendCampaignEmailsService = new();
    }

    public CampaignPublishedHandler NewInstance()
    {
        ConfigureMocks();
        return new(SendCampaignEmailsService.Object, NullLogger<CampaignPublishedHandler>.Instance);
    }

    public void Setup_SendCampaignEmailsService_SendAsync_Result(int sentCount) =>
        SendCampaignEmailsService
            .Setup(s => s.SendAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sentCount);
}
