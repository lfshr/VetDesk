using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Services;

namespace VetDesk.Domain.UnitTests.Services;

[TestFixture]
public class AllergyCheckTests
{
    private static Product Product(string name, string? ingredients = null, string? drugClass = null) =>
        new() { Code = "X", Name = name, Ingredients = ingredients, DrugClass = drugClass };

    private static PatientAllergy Allergy(string substance, string? drugClass = null) =>
        new() { Substance = substance, DrugClass = drugClass };

    [Test]
    public void SubstanceInName_Conflicts()
    {
        AllergyCheck.ConflictsWith(Product("Amoxicillin/clavulanate 250 mg"), Allergy("amoxicillin")).ShouldBeTrue();
    }

    [Test]
    public void SubstanceInIngredients_Conflicts()
    {
        AllergyCheck.ConflictsWith(Product("Synulox", ingredients: "Amoxicillin, clavulanic acid"), Allergy("amoxicillin")).ShouldBeTrue();
    }

    [Test]
    public void SameDrugClass_Conflicts()
    {
        AllergyCheck.ConflictsWith(Product("Ampicillin injection", drugClass: "Penicillin"), Allergy("amoxicillin", "penicillin")).ShouldBeTrue();
    }

    [Test]
    public void UnrelatedProduct_DoesNotConflict()
    {
        AllergyCheck.ConflictsWith(Product("Meloxicam", "meloxicam", "NSAID"), Allergy("amoxicillin", "Penicillin")).ShouldBeFalse();
    }

    [Test]
    public void ProductWithClassButAllergyWithout_DoesNotConflictOnClassAlone()
    {
        AllergyCheck.ConflictsWith(Product("Ampicillin injection", drugClass: "Penicillin"), Allergy("amoxicillin")).ShouldBeFalse();
    }

    [Test]
    public void Conflicts_ReturnsOnlyMatchingAllergies()
    {
        var conflicts = AllergyCheck.Conflicts(Product("Amoxicillin"), [Allergy("amoxicillin"), Allergy("chicken")]);

        conflicts.Count.ShouldBe(1);
        conflicts[0].Substance.ShouldBe("amoxicillin");
    }

    [Test]
    public void FreeTextNamingSubstance_Conflicts()
    {
        AllergyCheck.Conflicts("Amoxicillin ear drops (bought in)", [Allergy("amoxicillin")]).Count.ShouldBe(1);
    }

    [Test]
    public void FreeTextNotNamingSubstance_DoesNotConflict()
    {
        AllergyCheck.Conflicts("Hydrotherapy session", [Allergy("amoxicillin", drugClass: "Penicillin")]).ShouldBeEmpty();
    }
}
