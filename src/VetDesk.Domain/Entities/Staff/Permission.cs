namespace VetDesk.Domain.Entities.Staff;

/// <summary>The rows of the Settings › Role permissions table.</summary>
public enum Permission
{
    ViewClinicalRecords,
    WriteClinicalNotes,
    Prescribe,
    WitnessControlledDrugs,
    TakePayments,
    EditPriceList,
    ManageSettings
}

public static class PermissionLabels
{
    public static string Label(this Permission permission) => permission switch
    {
        Permission.ViewClinicalRecords => "View clinical records",
        Permission.WriteClinicalNotes => "Write clinical notes",
        Permission.Prescribe => "Prescribe medicines",
        Permission.WitnessControlledDrugs => "Witness controlled drugs",
        Permission.TakePayments => "Take payments and refunds",
        Permission.EditPriceList => "Edit price list",
        Permission.ManageSettings => "Manage users and settings",
        _ => permission.ToString()
    };
}
