namespace Promo.Worker.Domain.Entities;

public class Buyer
{
    public Guid Id { get; internal set; }
    public Guid BuyerGroupId { get; internal set; }
    public string Name { get; internal set; } = string.Empty;
    public string Email { get; internal set; } = string.Empty;

    public Buyer(Guid buyerGroupId, string name, string email)
    {
        Id = Guid.NewGuid();
        BuyerGroupId = buyerGroupId;
        Name = name;
        Email = email;
    }
}
