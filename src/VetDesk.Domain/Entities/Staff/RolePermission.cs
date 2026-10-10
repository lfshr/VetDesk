using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Staff;

/// <summary>One ticked box in the role permissions table. A missing row means the role doesn't have it.</summary>
public class RolePermission : BaseEntity
{
    public StaffRole Role { get; init; }
    public Permission Permission { get; init; }

    /// <summary>The matrix the design ships with; used to seed a new database.</summary>
    public static IEnumerable<RolePermission> Defaults()
    {
        var grid = new (Permission Permission, StaffRole[] Roles)[]
        {
            (Permission.ViewClinicalRecords, [StaffRole.Vet, StaffRole.Nurse, StaffRole.Reception, StaffRole.Admin]),
            (Permission.WriteClinicalNotes, [StaffRole.Vet, StaffRole.Nurse]),
            (Permission.Prescribe, [StaffRole.Vet]),
            (Permission.WitnessControlledDrugs, [StaffRole.Vet, StaffRole.Nurse]),
            (Permission.TakePayments, [StaffRole.Vet, StaffRole.Nurse, StaffRole.Reception, StaffRole.Admin]),
            (Permission.EditPriceList, [StaffRole.Admin]),
            (Permission.ManageSettings, [StaffRole.Admin]),
        };

        return grid.SelectMany(row => row.Roles.Select(role => new RolePermission { Role = role, Permission = row.Permission }));
    }
}
