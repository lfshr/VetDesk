using VetDesk.Domain.Entities.Products;

namespace VetDesk.Application.Pharmacy;

public enum StockStatus { InStock, Low, Out, Expiring, Expired }

public record StockRow(
    int ProductId,
    string Code,
    string Name,
    string? Form,
    string Unit,
    string? StockCategory,
    ProductCategory Category,
    bool IsControlledDrug,
    decimal OnHand,
    decimal ReorderLevel,
    string? NextBatch,
    DateOnly? NextExpiry,
    string? Location,
    StockStatus Status,
    int BatchCount);

public record DispenseRow(
    int Id,
    int PatientId,
    string PatientName,
    string OwnerName,
    int ProductId,
    string ProductName,
    decimal Quantity,
    string Unit,
    string? Directions,
    string PrescribedBy,
    DateTime CreatedUtc,
    bool IsControlledDrug,
    bool AwaitingAuthorisation,
    decimal OnHand,
    Guid Version);

public record ControlledDrugEntryDto(
    int Id,
    DateTime Utc,
    int ProductId,
    string ProductName,
    string Unit,
    ControlledDrugEntryKind Kind,
    decimal Change,
    decimal Balance,
    string Reason,
    string By,
    string? Witness);

public record LabelDto(
    string ClinicName,
    string? ClinicAddress,
    string? ClinicPhone,
    string PatientName,
    string OwnerName,
    string ProductName,
    decimal Quantity,
    string Unit,
    string? Directions,
    string PrescribedBy,
    DateTime DispensedOrCreatedUtc,
    bool IsControlledDrug);

public record ReorderLine(int ProductId, string Name, string Unit, bool IsControlledDrug, decimal OnHand, decimal ReorderLevel, decimal SuggestedQuantity);

public record DeliveryLineDto(int ProductId, string ProductName, string Unit, bool IsControlledDrug, decimal Quantity);

public record DeliveryDto(int OrderId, string Number, string Supplier, DateOnly? DeliverBy, DateTime? SentUtc, IReadOnlyList<DeliveryLineDto> Lines);

public record StockBatchDto(int Id, string BatchNumber, DateOnly? Expiry, decimal Quantity);
