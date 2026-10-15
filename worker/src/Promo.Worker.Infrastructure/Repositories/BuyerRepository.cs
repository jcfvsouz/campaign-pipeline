using Components.SQLRepository;
using Dapper;
using Promo.Worker.Domain.Entities;
using Promo.Worker.Domain.Repositories;
using Promo.Worker.Infrastructure.Persistence;

namespace Promo.Worker.Infrastructure.Repositories;

public class BuyerRepository(ISqlRepository sqlRepository) : IBuyerRepository
{
    public async Task<IReadOnlyList<Buyer>> GetByBuyerGroupIdAsync(Guid buyerGroupId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, BuyerGroupId, Name, Email FROM Buyers WHERE BuyerGroupId = @BuyerGroupId";
        var command = new CommandDefinition(sql, new { BuyerGroupId = buyerGroupId }, cancellationToken: cancellationToken);
        var rows = await sqlRepository.Connection.QueryAsync<BuyerRow>(command);
        return rows.Select(row => (Buyer)row).ToList();
    }
}
