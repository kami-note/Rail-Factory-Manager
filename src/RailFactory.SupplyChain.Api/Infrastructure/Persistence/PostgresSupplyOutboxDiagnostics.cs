using Microsoft.EntityFrameworkCore;
using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public sealed class PostgresSupplyOutboxDiagnostics(SupplyChainDbContext dbContext) : ISupplyOutboxDiagnostics
{
    public async Task<IReadOnlyList<SupplyOutboxDeadLetterInfo>> ListDeadLettersAsync(
        int take,
        CancellationToken cancellationToken)
    {
        return await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => x.Status == SupplyOutboxMessageStatus.DeadLetter)
            .OrderByDescending(x => x.DeadLetteredAt ?? x.LastAttemptAt ?? x.CreatedAt)
            .Take(take)
            .Select(x => new SupplyOutboxDeadLetterInfo(
                x.Id,
                x.EventType,
                x.CorrelationId,
                x.CreatedAt,
                x.AttemptCount,
                x.LastAttemptAt,
                x.DeadLetteredAt,
                x.LastError))
            .ToListAsync(cancellationToken);
    }
}
