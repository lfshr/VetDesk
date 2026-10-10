using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Clients.Commands;
using VetDesk.Application.Patients.Commands;
using VetDesk.Application.Staff.Commands;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.UnitTests.Handlers;

[TestFixture]
public class StaffAndClientHandlerTests
{
    private TestDatabase _database = null!;

    [SetUp]
    public async Task SetUp() => _database = await TestDatabase.CreateAsync();

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task FirstAdministrator_OnlyWhileNoStaffExist()
    {
        var handler = new CreateFirstAdministratorCommandHandler(_database.Db, _database.Hasher, _database.Clock);
        var command = new CreateFirstAdministratorCommand { FirstName = "Asha", LastName = "Patel", Email = "Asha@Example.com ", Password = "long-enough-pw", Role = StaffRole.Vet };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        first.IsSuccess.ShouldBeTrue(first.ToString());
        first.Value.Permissions.ShouldContain(Permission.ManageSettings);
        first.Value.Permissions.ShouldContain(Permission.Prescribe);
        second.ErrorCode.ShouldBe(CreateFirstAdministratorError.AlreadySetUp);
    }

    [Test]
    public async Task SignIn_IsCaseInsensitiveOnEmail_AndRejectsWrongPassword()
    {
        await _database.AddStaffAsync("Mia", "Reid", StaffRole.Nurse, "nurse-password");
        var handler = new SignInCommandHandler(_database.Db, _database.Hasher, _database.Clock);

        var ok = await handler.Handle(new SignInCommand { Email = "MIA.REID@test.example", Password = "nurse-password" }, CancellationToken.None);
        var wrong = await handler.Handle(new SignInCommand { Email = "mia.reid@test.example", Password = "nope" }, CancellationToken.None);

        ok.IsSuccess.ShouldBeTrue(ok.ToString());
        ok.Value.Permissions.ShouldNotContain(Permission.Prescribe);
        wrong.ErrorCode.ShouldBe(SignInError.InvalidCredentials);
    }

    [Test]
    public async Task SignIn_DeactivatedAccount_IsRejected()
    {
        var staff = await _database.AddStaffAsync("Mia", "Reid", StaffRole.Nurse, "nurse-password");
        staff.IsActive = false;
        await _database.Db.SaveChangesAsync();

        var result = await new SignInCommandHandler(_database.Db, _database.Hasher, _database.Clock)
            .Handle(new SignInCommand { Email = "mia.reid@test.example", Password = "nurse-password" }, CancellationToken.None);

        result.ErrorCode.ShouldBe(SignInError.InvalidCredentials);
    }

    [Test]
    public async Task RemovingTheLastAdministrator_IsRefused()
    {
        var admin = await _database.AddStaffAsync("Asha", "Patel", StaffRole.Admin);
        await _database.SignInAsync(admin);

        var result = await new SaveStaffMemberCommandHandler(_database.Db, _database.User, _database.Hasher).Handle(new SaveStaffMemberCommand
        {
            Id = admin.Id, FirstName = "Asha", LastName = "Patel", Email = admin.Email, Role = StaffRole.Vet, IsAdministrator = false,
        }, CancellationToken.None);

        result.ErrorCode.ShouldBe(SaveStaffMemberError.LastAdministrator);
    }

    [Test]
    public async Task SettingsChanges_NeedManageSettings()
    {
        await _database.SignInAsync(await _database.AddStaffAsync("Mia", "Reid", StaffRole.Nurse));

        var result = await new SetRolePermissionCommandHandler(_database.Db, _database.User)
            .Handle(new SetRolePermissionCommand { Role = StaffRole.Nurse, Permission = Permission.Prescribe, Granted = true }, CancellationToken.None);

        result.ErrorCode.ShouldBe(SetRolePermissionError.NotAllowed);
    }

    [Test]
    public async Task NewClient_WithAnExistingPhoneNumber_IsBlocked()
    {
        await _database.SignInAsync(await _database.AddStaffAsync("Jack", "Bennett", StaffRole.Reception));
        var handler = new SaveClientCommandHandler(_database.Db, _database.User);
        await handler.Handle(new SaveClientCommand { FirstName = "Hannah", LastName = "Clarke", Phone = "07700 900412" }, CancellationToken.None);

        var result = await handler.Handle(new SaveClientCommand { FirstName = "H", LastName = "Clarke", Phone = "+44 7700 900 412" }, CancellationToken.None);

        result.ErrorCode.ShouldBe(SaveClientError.DuplicatePhone);
    }

    [Test]
    public async Task NewClientAndPatient_WithKnownPhone_AddsPetToExistingAccount()
    {
        await _database.SignInAsync(await _database.AddStaffAsync("Jack", "Bennett", StaffRole.Reception));
        var handler = new CreateClientAndPatientCommandHandler(_database.Db, _database.User, _database.Clock);
        var first = await handler.Handle(new CreateClientAndPatientCommand { OwnerName = "Hannah Clarke", Phone = "07700 900412", PetName = "Biscuit", Species = "dog" }, CancellationToken.None);

        var second = await handler.Handle(new CreateClientAndPatientCommand { OwnerName = "Hannah Clarke", Phone = "07700900412", PetName = "Mabel", Species = "Cat" }, CancellationToken.None);

        second.Value.AddedToExistingClient.ShouldBeTrue();
        second.Value.ClientId.ShouldBe(first.Value.ClientId);
        (await _database.Db.Clients.CountAsync()).ShouldBe(1);
        (await _database.Db.Patients.FirstAsync(p => p.Name == "Biscuit")).Species.ShouldBe("Dog");
    }
}
