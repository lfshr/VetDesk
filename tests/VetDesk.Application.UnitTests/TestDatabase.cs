using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Common.Live;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Infrastructure;
using VetDesk.Infrastructure.Persistence;
using VetDesk.Infrastructure.Seeding;
using VetDesk.Infrastructure.Services;

namespace VetDesk.Application.UnitTests;

/// <summary>A fresh in-memory SQLite database with the required rows, a test clock and a switchable signed-in user.</summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestUser User { get; } = new();
    public TestClock Clock { get; } = new();
    public IPasswordHasher Hasher { get; } = new PasswordHasher();
    public TestDevice Device { get; } = new();
    public WrongPinTracker WrongPins { get; } = new(NullLogger<WrongPinTracker>.Instance);
    public IDevicePinLimit DeviceLimit => new DevicePinLimit(Device, WrongPins);
    public VetDeskDbContext Db { get; }

    private TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        Db = NewContext();
    }

    public VetDeskDbContext NewContext() =>
        new(new DbContextOptionsBuilder<VetDeskDbContext>().UseSqlite(_connection).Options, User, Clock);

    /// <summary>A context that tells <paramref name="feed"/> about each save, as the app's do, made at <paramref name="desk"/>.</summary>
    public VetDeskDbContext NewContext(RecordChangeFeed feed, IDesk desk) =>
        new(new DbContextOptionsBuilder<VetDeskDbContext>().UseSqlite(_connection).Options, User, Clock, feed, desk);

    public static async Task<TestDatabase> CreateAsync(bool demoData = false)
    {
        var database = new TestDatabase();
        var seeder = new DemoDataSeeder(database.Db, database.Clock, database.Hasher);
        await new DatabaseInitializer(database.Db, seeder, new DatabaseOptions(demoData), NullLogger<DatabaseInitializer>.Instance).InitialiseAsync();
        return database;
    }

    public const string Pin = "2468";
    public const string Password = "correct-horse-battery";

    /// <summary>Adds someone who signs in with <see cref="Pin"/>; administrators (by role) also get <see cref="Password"/>.</summary>
    public async Task<StaffMember> AddStaffAsync(string first, string last, StaffRole role, string pin = Pin)
    {
        var staff = new StaffMember
        {
            FirstName = first, LastName = last, Email = $"{first}.{last}@test.example".ToLowerInvariant(), Role = role, PinHash = Hasher.Hash(pin),
            PasswordHash = role == StaffRole.Admin ? Hasher.Hash(Password) : string.Empty,
        };
        Db.StaffMembers.Add(staff);
        await Db.SaveChangesAsync();
        return staff;
    }

    /// <summary>Signs a member of staff in with their role's permissions from the database.</summary>
    public async Task SignInAsync(StaffMember staff)
    {
        var roles = staff.IsAdministrator ? new[] { staff.Role, StaffRole.Admin } : new[] { staff.Role };
        var permissions = await Db.RolePermissions.Where(rp => roles.Contains(rp.Role)).Select(rp => rp.Permission).ToListAsync();
        User.Set(staff.Id, staff.ShortName, permissions);
    }

    public async Task<Patient> AddPatientAsync(string name = "Biscuit", string? allergy = null, string? allergyClass = null)
    {
        var client = new Client { FirstName = "Hannah", LastName = "Clarke", Phone = $"07700 9{Random.Shared.Next(10000, 99999)}" };
        var patient = new Patient { Name = name, Species = "Dog", Client = client };
        if (allergy is not null)
            patient.Allergies.Add(new PatientAllergy { Patient = patient, Substance = allergy, DrugClass = allergyClass });
        Db.Patients.Add(patient);
        await Db.SaveChangesAsync();
        return patient;
    }

    public async Task<Product> AddProductAsync(string name, ProductCategory category, decimal price, decimal stock = 0, bool controlled = false,
        string? drugClass = null, VatRate vat = VatRate.Standard, int? boosterMonths = null)
    {
        var product = new Product
        {
            Code = name.ToUpperInvariant().Replace(" ", "")[..Math.Min(8, name.Replace(" ", "").Length)] + Random.Shared.Next(100, 999),
            Name = name, Category = category, UnitPrice = price, VatRate = vat, IsControlledDrug = controlled, DrugClass = drugClass,
            IsStocked = category != ProductCategory.Service, Unit = "ml", BoosterMonths = boosterMonths, ReorderLevel = 5,
        };
        if (stock > 0)
            product.Batches.Add(new StockBatch { Product = product, BatchNumber = "B1", Quantity = stock, Expiry = new DateOnly(2030, 1, 1) });
        Db.Products.Add(product);
        await Db.SaveChangesAsync();
        return product;
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

public class TestUser : ICurrentUser
{
    private HashSet<Permission> _permissions = new();

    public int? StaffId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool Has(Permission permission) => _permissions.Contains(permission);

    public void Set(int staffId, string name, IEnumerable<Permission> permissions)
    {
        StaffId = staffId;
        Name = name;
        _permissions = permissions.ToHashSet();
    }

    public void SignOut()
    {
        StaffId = null;
        Name = string.Empty;
        _permissions.Clear();
    }
}

/// <summary>The device requests come from: a desk on the practice network unless a test changes it.</summary>
public class TestDevice : IDeviceAddress
{
    public string? Address { get; set; } = "192.168.1.20";
}

public class TestDesk : IDesk
{
    public Guid? DeskId { get; } = Guid.NewGuid();
}

public class TestClock : IClock
{
    /// <summary>A Friday morning in October (British Summer Time, so local time is UTC+1).</summary>
    public DateTime UtcNow { get; set; } = new(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);
}
