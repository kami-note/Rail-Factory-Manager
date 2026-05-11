namespace RailFactory.SupplyChain.Api.Api.Requests;

public sealed record ControlledAssociationDecisionRequest(
    DateTimeOffset ExpectedVersion,
    string Reason);
