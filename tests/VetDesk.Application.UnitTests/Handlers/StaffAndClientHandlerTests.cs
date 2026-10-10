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

    private SignInCommandHandler SignInHandler() => new(_database.Db, _database.Hasher, _database.Clock);

    [Test]
    public async Task FirstAdministrator_OnlyWhileNoStaffExist_AndStartsWithSettingsOpen()
    {
        var handler = new CreateFirstAdministratorCommandHandler(_database.Db, _database.Hasher, _database.Clock);
        var command = new CreateFirstAdministratorCommand { FirstName = "Douglas", Pin = "1357", Password = "long-enough-pw", Role = StaffRole.Vet };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        first.IsSuccess.ShouldBeTrue(first.ToString());
        first.Value.Permissions.ShouldContain(Permission.ManageSettings);
        first.Value.Permissions.ShouldContain(Permission.Prescribe);
        first.Value.ShortName.ShouldBe("Douglas");
        second.ErrorCode.ShouldBe(CreateFirstAdministratorError.AlreadySetUp);
    }

    [TestCase("123")]
    [TestCase("12a4")]
    [TestCase("12345")]
    public async Task FirstAdministrator_NeedsAFourDigitPin(string pin)
    {
        var result = await new CreateFirstAdministratorCommandHandler(_database.Db, _database.Hasher, _database.Clock)
            .Handle(new CreateFirstAdministratorCommand { FirstName = "Liam", Pin = pin, Password = "long-enough-pw" }, CancellationToken.None);

        result.ErrorCode.ShouldBe(CreateFirstAdministratorError.InvalidRequest);
    }

    [Test]
    public async Task SignIn_WithPin_GivesRolePermissions_AndRejectsAWrongPin()
    {
        var nurse = await _database.AddStaffAsync("Janet", "", StaffRole.Nurse);

        var ok = await SignInHandler().Handle(new SignInCommand { StaffId = nurse.Id, Pin = TestDatabase.Pin }, CancellationToken.None);
        var wrong = await SignInHandler().Handle(new SignInCommand { StaffId = nurse.Id, Pin = "0000" }, CancellationToken.None);

        ok.IsSuccess.ShouldBeTrue(ok.ToString());
        ok.Value.Permissions.ShouldContain(Permission.WitnessControlledDrugs);
        ok.Value.Permissions.ShouldNotContain(Permission.Prescribe);
        wrong.ErrorCode.ShouldBe(SignInError.InvalidCredentials);
    }

    [Test]
    public async Task SignIn_AfterTooManyWrongPins_RefusesEvenTheRightOne_UntilTheLockoutEnds()
    {
        var nurse = await _database.AddStaffAsync("Janet", "", StaffRole.Nurse);
        for (var i = 0; i < StaffMember.MaxPinAttempts; i++)
            await SignInHandler().Handle(new SignInCommand { StaffId = nurse.Id, Pin = "0000" }, CancellationToken.None);

        var locked = await SignInHandler().Handle(new SignInCommand { StaffId = nurse.Id, Pin = TestDatabase.Pin }, CancellationToken.None);
        _database.Clock.UtcNow += StaffMember.PinLockout;
        var later = await SignInHandler().Handle(new SignInCommand { StaffId = nurse.Id, Pin = TestDatabase.Pin }, CancellationToken.None);

        locked.IsFailure.ShouldBeTrue();
        locked.ToString().ShouldContain("Too many wrong PINs");
        later.IsSuccess.ShouldBeTrue(later.ToString());
    }

    [Test]
    public async Task SignIn_DeactivatedAccount_IsRejected()
    {
        var staff = await _database.AddStaffAsync("Janet", "", StaffRole.Nurse);
        staff.IsActive = false;
        await _database.Db.SaveChangesAsync();

        var result = await SignInHandler().Handle(new SignInCommand { StaffId = staff.Id, Pin = TestDatabase.Pin }, CancellationToken.None);

        result.ErrorCode.ShouldBe(SignInError.InvalidCredentials);
    }

    [Test]
    public async Task Administrator_SignedInByPin_NeedsThePasswordForSettings()
    {
        var admin = await _database.AddStaffAsync("Liam", "", StaffRole.Admin);

        var byPin = await SignInHandler().Handle(new SignInCommand { StaffId = admin.Id, Pin = TestDatabase.Pin }, CancellationToken.None);
        _database.User.Set(admin.Id, admin.ShortName, byPin.Value.Permissions);
        var unlock = new UnlockSettingsCommandHandler(_database.Db, _database.User, _database.Hasher);
        var wrong = await unlock.Handle(new UnlockSettingsCommand { Password = "not-the-password" }, CancellationToken.None);
        var right = await unlock.Handle(new UnlockSettingsCommand { Password = TestDatabase.Password }, CancellationToken.None);

        byPin.Value.Permissions.ShouldNotContain(Permission.ManageSettings);
        byPin.Value.CanUnlockSettings.ShouldBeTrue();
        wrong.ErrorCode.ShouldBe(UnlockSettingsError.WrongPassword);
        right.Value.Permissions.ShouldContain(Permission.ManageSettings);
    }

    [Test]
    public async Task UnlockSettings_ForSomeoneWhoIsNotAnAdministrator_IsRefused()
    {
        var nurse = await _database.AddStaffAsync("Janet", "", StaffRole.Nurse);
        await _database.SignInAsync(nurse);

        var result = await new UnlockSettingsCommandHandler(_database.Db, _database.User, _database.Hasher)
            .Handle(new UnlockSettingsCommand { Password = TestDatabase.Password }, CancellationToken.None);

        result.ErrorCode.ShouldBe(UnlockSettingsError.NotAdministrator);
    }

    [Test]
    public async Task AccountFromBeforePins_ChoosesAPinWithItsPassword()
    {
        var admin = await _database.AddStaffAsync("Liam", "", StaffRole.Admin);
        admin.PinHash = string.Empty;
        await _database.Db.SaveChangesAsync();
        var handler = new SetUpPinCommandHandler(_database.Db, _database.Hasher, _database.Clock);

        var wrong = await handler.Handle(new SetUpPinCommand { StaffId = admin.Id, Password = "nope", NewPin = "9753" }, CancellationToken.None);
        var ok = await handler.Handle(new SetUpPinCommand { StaffId = admin.Id, Password = TestDatabase.Password, NewPin = "9753" }, CancellationToken.None);
        var again = await handler.Handle(new SetUpPinCommand { StaffId = admin.Id, Password = TestDatabase.Password, NewPin = "1111" }, CancellationToken.None);
        var signIn = await SignInHandler().Handle(new SignInCommand { StaffId = admin.Id, Pin = "9753" }, CancellationToken.None);

        wrong.ErrorCode.ShouldBe(SetUpPinError.WrongPassword);
        ok.IsSuccess.ShouldBeTrue(ok.ToString());
        again.ErrorCode.ShouldBe(SetUpPinError.NotAllowed);
        signIn.IsSuccess.ShouldBeTrue(signIn.ToString());
    }

    [Test]
    public async Task NewStaff_NeedOnlyAFirstNameAndPin_ButAdministratorsAlsoNeedAPassword()
    {
        await _database.SignInAsync(await _database.AddStaffAsync("Liam", "", StaffRole.Admin));
        var handler = new SaveStaffMemberCommandHandler(_database.Db, _database.User, _database.Hasher);

        var nurse = await handler.Handle(new SaveStaffMemberCommand { FirstName = "Lorna", Role = StaffRole.Nurse, Pin = "4321" }, CancellationToken.None);
        var vetAdmin = await handler.Handle(new SaveStaffMemberCommand { FirstName = "Jen", Role = StaffRole.Vet, IsAdministrator = true, Pin = "4321" }, CancellationToken.None);

        nurse.IsSuccess.ShouldBeTrue(nurse.ToString());
        (await _database.Db.StaffMembers.SingleAsync(s => s.Id == nurse.Value)).Email.ShouldBeNull();
        vetAdmin.ErrorCode.ShouldBe(SaveStaffMemberError.InvalidRequest);
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
