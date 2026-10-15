using Promo.Worker.Application.Events;
using Promo.Worker.Application.Tests.Fixtures;

namespace Promo.Worker.Application.Tests.Handlers;

public class CampaignPublishedHandlerTests(CampaignPublishedHandlerFixture fixture) : IClassFixture<CampaignPublishedHandlerFixture>
{
    [Fact]
    public async Task HandleAsync_sends_campaign_emails_for_the_events_buyer_group_and_name()
    {
        // Arrange
        var sut = fixture.NewInstance();
        fixture.Setup_SendCampaignEmailsService_SendAsync_Result(3);
        var @event = new CampaignPublished(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Black Friday", DateTime.UtcNow);

        // Act
        await sut.HandleAsync(@event);

        // Assert
        fixture.SendCampaignEmailsService.Verify(s => s.SendAsync(@event.BuyerGroupId, @event.Name, It.IsAny<CancellationToken>()), Times.Once);
    }
}
