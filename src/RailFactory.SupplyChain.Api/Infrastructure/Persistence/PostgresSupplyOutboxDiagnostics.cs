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

    public async Task<int> ReplayDeadLettersAsync(
        IEnumerable<Guid>? messageIds,
        CancellationToken cancellationToken)
    {
        var query = dbContext.OutboxMessages
            .Where(x => x.Status == SupplyOutboxMessageStatus.DeadLetter);

        if (messageIds?.Any() == true)
        {
            query = query.Where(x => messageIds.Contains(x.Id));
        }

        var messages = await query.ToListAsync(cancellationToken);
        if (messages.Count == 0)
        {
            return 0;
        }

        foreach (var message in messages)
        {
            message.ResetForReplay();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }
}
