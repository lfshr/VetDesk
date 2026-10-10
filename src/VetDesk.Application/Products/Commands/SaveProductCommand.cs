using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Products.Commands;

public enum SaveProductError { NotAllowed, InvalidRequest, NotFound, DuplicateCode }

/// <summary>Adds or edits a price list entry. Prices are entered net of VAT.</summary>
public class SaveProductCommand : IRequest<Result<int>>
{
    public int? Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public ProductCategory Category { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal CostPrice { get; init; }
    public VatRate VatRate { get; init; } = VatRate.Standard;
    public string? Form { get; init; }
    public string Unit { get; init; } = "item";
    public string? StockCategory { get; init; }
    public bool IsControlledDrug { get; init; }
    public string? DrugClass { get; init; }
    public string? Ingredients { get; init; }
    public bool IsStocked { get; init; }
    public decimal ReorderLevel { get; init; }
    public string? Location { get; init; }
    public int? BoosterMonths { get; init; }
    public bool IsActive { get; init; } = true;
}

public class SaveProductCommandHandler : IRequestHandler<SaveProductCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public SaveProductCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<int>> Handle(SaveProductCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.EditPriceList))
            return Result.Failure(SaveProductError.NotAllowed, Access.Denied(Permission.EditPriceList));
        if (!Text.Has(request.Name) || !Text.Has(request.Code))
            return Result.Failure(SaveProductError.InvalidRequest, "Enter a code and a name.");
        if (request.UnitPrice < 0 || request.CostPrice < 0 || request.ReorderLevel < 0)
            return Result.Failure(SaveProductError.InvalidRequest, "Prices and reorder levels can't be negative.");
        if (request.IsControlledDrug && !request.IsStocked)
            return Result.Failure(SaveProductError.InvalidRequest, "Controlled drugs must be stocked so the register balances.");
        if (request.Category == ProductCategory.Service && request.IsStocked)
            return Result.Failure(SaveProductError.InvalidRequest, "Services can't be stocked.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _dbContext.Products.AnyAsync(p => p.Code == code && p.Id != request.Id, cancellationToken))
            return Result.Failure(SaveProductError.DuplicateCode, $"Another product already uses the code {code}.");

        Product product;
        if (request.Id is { } id)
        {
            var existing = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (existing is null)
                return Result.Failure(SaveProductError.NotFound, "That product no longer exists.");
            product = existing;
            product.Code = code;
            product.Name = request.Name.Trim();
        }
        else
        {
            product = new Product { Code = code, Name = request.Name.Trim() };
            _dbContext.Products.Add(product);
        }

        product.Category = request.Category;
        product.UnitPrice = Money.Round(request.UnitPrice);
        product.CostPrice = Money.Round(request.CostPrice);
        product.VatRate = request.VatRate;
        product.Form = Text.Clean(request.Form);
        product.Unit = Text.Clean(request.Unit) ?? "item";
        product.StockCategory = Text.Clean(request.StockCategory);
        product.IsControlledDrug = request.IsControlledDrug;
        product.DrugClass = Text.Clean(request.DrugClass);
        product.Ingredients = Text.Clean(request.Ingredients);
        product.IsStocked = request.IsStocked;
        product.ReorderLevel = request.ReorderLevel;
        product.Location = Text.Clean(request.Location);
        product.BoosterMonths = request.Category == ProductCategory.Vaccine ? request.BoosterMonths : null;
        product.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(product.Id);
    }
}
