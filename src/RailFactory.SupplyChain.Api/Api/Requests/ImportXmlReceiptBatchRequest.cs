using System.ComponentModel.DataAnnotations;

namespace RailFactory.SupplyChain.Api.Api.Requests;

public sealed class ImportXmlReceiptBatchRequest
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<ImportXmlReceiptBatchDocumentRequest> Documents { get; init; } = [];
}

public sealed class ImportXmlReceiptBatchDocumentRequest
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string FileName { get; init; } = string.Empty;

    [Required]
    public string XmlContent { get; init; } = string.Empty;
}
