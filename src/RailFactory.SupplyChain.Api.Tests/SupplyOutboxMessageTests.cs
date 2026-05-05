using RailFactory.SupplyChain.Api.Infrastructure.Persistence;
using Xunit;

namespace RailFactory.SupplyChain.Api.Tests;

public sealed class SupplyOutboxMessageTests
{
    [Fact]
    public void MarkDeadLetter_persists_operational_failure_state()
    {
        var message = new SupplyOutboxMessage(Guid.NewGuid(), "supply.receipt_item_registered", "corr-1", "{}");

        message.MarkDeadLetter("Inventory returned 400 Bad Request.");

        Assert.Equal(SupplyOutboxMessageStatus.DeadLetter, message.Status);
        Assert.Equal(1, message.AttemptCount);
        Assert.NotNull(message.LastAttemptAt);
        Assert.NotNull(message.DeadLetteredAt);
        Assert.Equal("Inventory returned 400 Bad Request.", message.LastError);
        Assert.Null(message.DispatchedAt);
    }

    [Fact]
    public void MarkTransientFailure_keeps_message_pending_for_retry()
    {
        var message = new SupplyOutboxMessage(Guid.NewGuid(), "supply.receipt_item_registered", "corr-1", "{}");

        message.MarkTransientFailure("Inventory timeout.");

        Assert.Equal(SupplyOutboxMessageStatus.Pending, message.Status);
        Assert.Equal(1, message.AttemptCount);
        Assert.NotNull(message.LastAttemptAt);
        Assert.Equal("Inventory timeout.", message.LastError);
        Assert.Null(message.DeadLetteredAt);
    }
}
