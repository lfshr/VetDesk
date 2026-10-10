using VetDesk.Domain.Entities.Clients;

namespace VetDesk.Application.Clients;

internal static class ClientMapping
{
    public static ClientDetail ToDetail(Client c, decimal balance, DateOnly today) => new(
        c.Id, c.ClientNumber, c.FirstName, c.LastName, c.FullName, c.ShortName, c.Initials, c.Phone, c.Email,
        c.Address, c.Postcode, c.RemindersOptIn, balance,
        c.Patients.OrderBy(p => p.IsDeceased).ThenBy(p => p.Name)
            .Select(p => new ClientPet(p.Id, p.Name, p.PatientNumber, p.SpeciesAndBreed, p.AgeLabel(today), p.IsDeceased))
            .ToList(),
        c.Version);
}
