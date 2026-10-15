using Bogus;
using Promo.Worker.Domain.Entities;

namespace Promo.Worker.Domain.Tests.Builders;

public class BuyerBuilder
{
    private static readonly Faker Faker = new();

    private Guid _buyerGroupId = Guid.NewGuid();
    private string _name = Faker.Name.FullName();
    private string _email = Faker.Internet.Email();

    public BuyerBuilder WithBuyerGroup(Guid buyerGroupId)
    {
        _buyerGroupId = buyerGroupId;
        return this;
    }

    public BuyerBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public BuyerBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public Buyer Build() => new(_buyerGroupId, _name, _email);
}
