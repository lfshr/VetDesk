namespace VetDesk.Application.Clients;

public record ClientRow(
    int Id,
    string ClientNumber,
    string FullName,
    string Initials,
    string Phone,
    string? Email,
    IReadOnlyList<string> PetNames,
    DateTime? LastVisitUtc,
    decimal Balance,
    DateTime CreatedUtc);

public record ClientPet(int Id, string Name, string PatientNumber, string SpeciesAndBreed, string Age, bool IsDeceased);

public record ClientDetail(
    int Id,
    string ClientNumber,
    string FirstName,
    string LastName,
    string FullName,
    string ShortName,
    string Initials,
    string Phone,
    string? Email,
    string? Address,
    string? Postcode,
    bool RemindersOptIn,
    decimal Balance,
    IReadOnlyList<ClientPet> Pets,
    Guid Version);
