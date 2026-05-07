namespace RailFactory.SupplyChain.Api.Api.Requests;

public sealed record ReplayOutboxDeadLettersRequest(IEnumerable<Guid>? MessageIds);
