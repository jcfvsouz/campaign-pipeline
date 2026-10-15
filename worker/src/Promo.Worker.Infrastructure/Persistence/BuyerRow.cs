using Promo.Worker.Domain.Entities;

namespace Promo.Worker.Infrastructure.Persistence;

internal class BuyerRow
{
    public Guid Id { get; set; }
    public Guid BuyerGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public static explicit operator Buyer(BuyerRow row) =>
        new(row.BuyerGroupId, row.Name, row.Email) { Id = row.Id };
}
