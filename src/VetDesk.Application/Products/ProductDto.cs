using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Services;

namespace VetDesk.Application.Products;

public record ProductDto(
    int Id,
    string Code,
    string Name,
    ProductCategory Category,
    decimal UnitPrice,
    decimal GrossPrice,
    decimal CostPrice,
    VatRate VatRate,
    string? Form,
    string Unit,
    string? StockCategory,
    bool IsControlledDrug,
    string? DrugClass,
    string? Ingredients,
    bool IsStocked,
    decimal ReorderLevel,
    string? Location,
    int? BoosterMonths,
    bool IsActive,
    decimal OnHand,
    IReadOnlyList<string> AllergyConflicts)
{
    public static ProductDto From(Product p, IReadOnlyList<string>? conflicts = null) => new(
        p.Id, p.Code, p.Name, p.Category, p.UnitPrice, VatCalculator.Gross(p.UnitPrice, p.VatRate), p.CostPrice, p.VatRate,
        p.Form, p.Unit, p.StockCategory, p.IsControlledDrug, p.DrugClass, p.Ingredients, p.IsStocked, p.ReorderLevel,
        p.Location, p.BoosterMonths, p.IsActive, p.OnHand, conflicts ?? []);
}
