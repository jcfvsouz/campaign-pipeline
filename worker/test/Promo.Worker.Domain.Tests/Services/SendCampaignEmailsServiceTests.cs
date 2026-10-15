using Promo.Worker.Domain.Email;
using Promo.Worker.Domain.Entities;
using Promo.Worker.Domain.Tests.Builders;
using Promo.Worker.Domain.Tests.Fixtures;

namespace Promo.Worker.Domain.Tests.Services;

public class SendCampaignEmailsServiceTests(SendCampaignEmailsServiceFixture fixture) : IClassFixture<SendCampaignEmailsServiceFixture>
{
    [Fact]
    public async Task SendAsync_sends_one_email_per_buyer_and_returns_the_count()
    {
        // Arrange
        var sut = fixture.NewInstance();
        var buyerGroupId = Guid.NewGuid();
        var buyers = new[]
        {
            new BuyerBuilder().WithBuyerGroup(buyerGroupId).Build(),
            new BuyerBuilder().WithBuyerGroup(buyerGroupId).Build(),
            new BuyerBuilder().WithBuyerGroup(buyerGroupId).Build()
        };
        fixture.Setup_BuyerRepository_GetByBuyerGroupId_Result(buyers);

        // Act
        var sentCount = await sut.SendAsync(buyerGroupId, "Black Friday");

        // Assert
        sentCount.Should().Be(3);
        fixture.EmailSender.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task SendAsync_sends_nothing_for_an_empty_group()
    {
        // Arrange
        var sut = fixture.NewInstance();
        fixture.Setup_BuyerRepository_GetByBuyerGroupId_Result(Array.Empty<Buyer>());

        // Act
        var sentCount = await sut.SendAsync(Guid.NewGuid(), "Black Friday");

        // Assert
        sentCount.Should().Be(0);
        fixture.EmailSender.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_addresses_each_email_to_the_right_buyer_with_the_campaign_name_in_the_subject()
    {
        // Arrange
        var sut = fixture.NewInstance();
        var buyerGroupId = Guid.NewGuid();
        var buyer = new BuyerBuilder().WithBuyerGroup(buyerGroupId).WithName("Ana Souza").WithEmail("ana.souza@example.com").Build();
        fixture.Setup_BuyerRepository_GetByBuyerGroupId_Result([buyer]);

        // Act
        await sut.SendAsync(buyerGroupId, "Black Friday");

        // Assert
        fixture.EmailSender.Verify(s => s.SendAsync(
            It.Is<EmailMessage>(m => m.ToEmail == buyer.Email && m.ToName == buyer.Name && m.Subject.Contains("Black Friday")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
