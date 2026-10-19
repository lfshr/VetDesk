using VetDesk.Application.Clients.Queries;
using VetDesk.Application.Patients.Queries;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.UnitTests.Handlers;

/// <summary>The search boxes on the Clients and Patients screens: postcode and microchip, typed however the desk types them.</summary>
[TestFixture]
public class SearchHandlerTests
{
    private TestDatabase _database = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = await TestDatabase.CreateAsync();
        await _database.SignInAsync(await _database.AddStaffAsync("Lesley", "", StaffRole.Reception));

        var clarke = new Client { FirstName = "Hannah", LastName = "Clarke", Phone = "07700 900412", Postcode = "BS6 5QT" };
        var osei = new Client { FirstName = "Rachel", LastName = "Osei", Phone = "07700 900188", Postcode = "BS7 8LP" };
        _database.Db.Patients.AddRange(
            new Patient { Name = "Biscuit", Species = "Dog", Client = clarke, Microchip = "826098100345219" },
            new Patient { Name = "Juniper", Species = "Cat", Client = osei });
        await _database.Db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    private async Task<List<string>> ClientsAsync(string text)
    {
        var result = await new SearchClientsQueryHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new SearchClientsQuery { Text = text }, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.ToString());
        return result.Value.Select(c => c.FullName).ToList();
    }

    private async Task<List<string>> PatientsAsync(string text)
    {
        var result = await new SearchPatientsQueryHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new SearchPatientsQuery { Text = text }, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.ToString());
        return result.Value.Select(p => p.Name).ToList();
    }

    [TestCase("BS6 5QT")]
    [TestCase("bs65qt")]
    [TestCase("BS6")]
    [TestCase("6 5Q")]
    public async Task Clients_FoundByPostcode_IgnoringCaseAndSpaces(string postcode) =>
        (await ClientsAsync(postcode)).ShouldBe(["Hannah Clarke"]);

    [TestCase("826098100345219")]
    [TestCase("826 098 100 345 219")]
    [TestCase("345219")]
    public async Task Clients_FoundByTheirPetsMicrochip(string chip) =>
        (await ClientsAsync(chip)).ShouldBe(["Hannah Clarke"]);

    [Test]
    public async Task Clients_FilterCounts_IncludeAMicrochipMatch()
    {
        var result = await new CountClientsQueryHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new CountClientsQuery { Text = "826 098 100 345 219" }, CancellationToken.None);

        result.Value[ClientFilter.All].ShouldBe(1);
    }

    [TestCase("BS6 5QT")]
    [TestCase("bs65qt")]
    [TestCase("BS6")]
    public async Task Patients_FoundByTheirOwnersPostcode_IgnoringCaseAndSpaces(string postcode) =>
        (await PatientsAsync(postcode)).ShouldBe(["Biscuit"]);

    [TestCase("826098100345219")]
    [TestCase("826 098 100 345 219")]
    [TestCase("345219")]
    public async Task Patients_FoundByMicrochip(string chip) =>
        (await PatientsAsync(chip)).ShouldBe(["Biscuit"]);

    [Test]
    public async Task Patients_FilterCounts_IncludeAPostcodeMatch()
    {
        var result = await new CountPatientsQueryHandler(_database.Db, _database.User)
            .Handle(new CountPatientsQuery { Text = "bs7 8lp" }, CancellationToken.None);

        result.Value[SpeciesFilter.All].ShouldBe(1);
        result.Value[SpeciesFilter.Cats].ShouldBe(1);
    }
}
