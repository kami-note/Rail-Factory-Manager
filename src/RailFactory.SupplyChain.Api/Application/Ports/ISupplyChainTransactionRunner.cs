namespace RailFactory.SupplyChain.Api.Application.Ports;

public interface ISupplyChainTransactionRunner
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}
