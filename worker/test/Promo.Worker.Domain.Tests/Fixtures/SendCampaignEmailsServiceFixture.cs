using Promo.Worker.Domain.Email;
using Promo.Worker.Domain.Entities;
using Promo.Worker.Domain.Repositories;
using Promo.Worker.Domain.Services;

namespace Promo.Worker.Domain.Tests.Fixtures;

public class SendCampaignEmailsServiceFixture
{
    public Mock<IBuyerRepository> BuyerRepository { get; private set; } = new();
    public Mock<IEmailSender> EmailSender { get; private set; } = new();

    public void ConfigureMocks()
    {
        BuyerRepository = new();
        EmailSender = new();
    }

    public SendCampaignEmailsService NewInstance()
    {
        ConfigureMocks();
        return new(BuyerRepository.Object, EmailSender.Object);
    }

    public void Setup_BuyerRepository_GetByBuyerGroupId_Result(IReadOnlyList<Buyer> buyers) =>
        BuyerRepository
            .Setup(r => r.GetByBuyerGroupIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buyers);
}
