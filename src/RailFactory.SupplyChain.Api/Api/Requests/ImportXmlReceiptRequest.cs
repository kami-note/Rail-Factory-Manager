using System.ComponentModel.DataAnnotations;

namespace RailFactory.SupplyChain.Api.Api.Requests;

public sealed class ImportXmlReceiptRequest
{
    [Required]
    public string XmlContent { get; init; } = string.Empty;
}
