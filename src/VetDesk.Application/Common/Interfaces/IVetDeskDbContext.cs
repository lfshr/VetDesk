using Microsoft.EntityFrameworkCore;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Consultations;
using VetDesk.Domain.Entities.Dispensing;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Practice;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Purchasing;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Domain.Entities.Tasks;

namespace VetDesk.Application.Common.Interfaces;

public interface IVetDeskDbContext
{
    DbSet<StaffMember> StaffMembers { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Client> Clients { get; }
    DbSet<Patient> Patients { get; }
    DbSet<PatientAlert> PatientAlerts { get; }
    DbSet<PatientAllergy> PatientAllergies { get; }
    DbSet<WeightRecord> WeightRecords { get; }
    DbSet<Vaccination> Vaccinations { get; }
    DbSet<ClinicalNote> ClinicalNotes { get; }
    DbSet<Product> Products { get; }
    DbSet<StockBatch> StockBatches { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<ControlledDrugEntry> ControlledDrugEntries { get; }
    DbSet<AppointmentType> AppointmentTypes { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<Consultation> Consultations { get; }
    DbSet<ConsultationCharge> ConsultationCharges { get; }
    DbSet<DispenseItem> DispenseItems { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLine> InvoiceLines { get; }
    DbSet<Payment> Payments { get; }
    DbSet<InsuranceClaim> InsuranceClaims { get; }
    DbSet<DayClose> DayCloses { get; }
    DbSet<ClinicSettings> ClinicSettings { get; }
    DbSet<NumberSequence> NumberSequences { get; }
    DbSet<BackupRecord> BackupRecords { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }
    DbSet<FollowUp> FollowUps { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
