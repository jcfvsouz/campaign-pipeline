using Promo.Worker.Domain.Entities;

namespace Promo.Worker.Domain.Repositories;

public interface IBuyerRepository
{
    Task<IReadOnlyList<Buyer>> GetByBuyerGroupIdAsync(Guid buyerGroupId, CancellationToken cancellationToken = default);
}
