using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
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
using VetDesk.Domain.Services;
using VetDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VetDesk.Infrastructure.Seeding;

/// <summary>
/// The practice, people and pets from the design mock-ups, so a demo or development database looks like the
/// screens. Only runs when VetDesk:SeedDemoData is true and the database has no staff.
/// </summary>
public class DemoDataSeeder
{
    public const string DemoPin = "1234";
    public const string DemoPassword = "vetdesk-demo";

    private readonly VetDeskDbContext _db;
    private readonly IClock _clock;
    private readonly IPasswordHasher _hasher;

    public DemoDataSeeder(VetDeskDbContext db, IClock clock, IPasswordHasher hasher)
    {
        _db = db;
        _clock = clock;
        _hasher = hasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var today = ClinicTime.Today(now);
        DateTime At(int daysFromToday, int hour, int minute = 0) => ClinicTime.ToUtc(today.AddDays(daysFromToday), new TimeOnly(hour, minute));

        var settings = await _db.ClinicSettings.FirstAsync(cancellationToken);
        settings.Name = "Northfield Veterinary Clinic";
        settings.Address = "14 Gloucester Road, Bristol";
        settings.Phone = "0117 496 0321";
        settings.Email = "hello@northfieldvets.example";
        settings.VatNumber = "GB123456789";
        settings.BackupFolder = Path.Combine(Path.GetTempPath(), "VetDesk", "Backups");

        // People: the practice's own team, by first name. Everyone signs in with DemoPin; administrators open
        // users and settings with DemoPassword.
        var pinHash = _hasher.Hash(DemoPin);
        var passwordHash = _hasher.Hash(DemoPassword);
        StaffMember Staff(string first, StaffRole role, bool admin = false) =>
            new() { FirstName = first, Role = role, IsAdministrator = admin, PinHash = pinHash, PasswordHash = admin || role == StaffRole.Admin ? passwordHash : string.Empty };
        var douglas = Staff("Douglas", StaffRole.Vet, admin: true);
        var heather = Staff("Heather", StaffRole.Vet, admin: true);
        var jen = Staff("Jen", StaffRole.Vet, admin: true);
        var janet = Staff("Janet", StaffRole.Nurse);
        var lesley = Staff("Lesley", StaffRole.Nurse);
        var lorna = Staff("Lorna", StaffRole.Nurse);
        var liam = Staff("Liam", StaffRole.Admin);
        _db.StaffMembers.AddRange(douglas, heather, jen, janet, lesley, lorna, liam);

        // Price list
        Product P(string code, string name, ProductCategory cat, decimal price, decimal cost = 0, string unit = "item", string? form = null,
            bool stocked = false, decimal reorder = 0, string? location = null, bool cd = false, string? drugClass = null, string? ingredients = null,
            int? booster = null, VatRate vat = VatRate.Standard, string? stockCategory = null) =>
            new()
            {
                Code = code, Name = name, Category = cat, UnitPrice = price, CostPrice = cost, Unit = unit, Form = form, IsStocked = stocked,
                ReorderLevel = reorder, Location = location, IsControlledDrug = cd, DrugClass = drugClass, Ingredients = ingredients,
                BoosterMonths = booster, VatRate = vat, StockCategory = stockCategory ?? (cat == ProductCategory.Vaccine ? "Vaccine" : null),
            };
        var consult = P("CONS", "Consultation", ProductCategory.Service, 52m);
        var recheck = P("RECHK", "Recheck consultation", ProductCategory.Service, 28m);
        var nailClip = P("NAIL", "Nail clip", ProductCategory.Service, 14m);
        var microchip = P("CHIP", "Microchip implant", ProductCategory.Service, 22m);
        var dental = P("DENT", "Dental scale and polish (under GA)", ProductCategory.Service, 285m);
        var neuter = P("NEUT", "Cat neuter", ProductCategory.Service, 95m);
        var meloxicam = P("MELOX", "Meloxicam 1.5 mg/ml oral suspension", ProductCategory.Medicine, 0.85m, 0.31m, "ml", "Oral suspension · 100 ml", true, 100, "Shelf B2", drugClass: "NSAID", ingredients: "meloxicam", stockCategory: "Analgesic");
        var amoxiclav = P("AMOXC", "Amoxicillin/clavulanate 250 mg tablets", ProductCategory.Medicine, 0.95m, 0.28m, "tablet", "Tablet · 250 mg", true, 60, "Shelf A1", drugClass: "Penicillin", ingredients: "amoxicillin, clavulanic acid", stockCategory: "Antibiotic");
        var maropitant = P("MARO", "Maropitant 10 mg/ml injection", ProductCategory.Medicine, 3.20m, 1.10m, "ml", "Injection · 20 ml", true, 20, "Fridge 1", ingredients: "maropitant", stockCategory: "Antiemetic");
        var methadone = P("METH", "Methadone 10 mg/ml injection", ProductCategory.Medicine, 4.50m, 1.60m, "ml", "Injection · 10 ml", true, 10, "CD cabinet", cd: true, drugClass: "Opioid", ingredients: "methadone", stockCategory: "Analgesic");
        var ketamine = P("KETA", "Ketamine 100 mg/ml injection", ProductCategory.Medicine, 3.80m, 1.20m, "ml", "Injection · 10 ml", true, 10, "CD cabinet", cd: true, ingredients: "ketamine", stockCategory: "Anaesthetic");
        var buprenorphine = P("BUPR", "Buprenorphine 0.3 mg/ml injection", ProductCategory.Medicine, 5.10m, 1.75m, "ml", "Injection · 10 ml", true, 10, "CD cabinet", cd: true, drugClass: "Opioid", ingredients: "buprenorphine", stockCategory: "Analgesic");
        var kennelCough = P("KCV", "Kennel cough vaccine (intranasal)", ProductCategory.Vaccine, 38m, 9.50m, "dose", "Intranasal · 1 dose", true, 15, "Fridge 1", ingredients: "Bordetella bronchiseptica, parainfluenza", booster: 12);
        var lepto = P("L4", "Lepto 4 booster", ProductCategory.Vaccine, 45m, 8.20m, "dose", "Injection · 1 dose", true, 20, "Fridge 1", booster: 12);
        var catFlu = P("FCV", "Feline flu and enteritis vaccine", ProductCategory.Vaccine, 46m, 8.90m, "dose", "Injection · 1 dose", true, 15, "Fridge 1", booster: 12);
        var chlorhex = P("CHLX", "Chlorhexidine gel 2%", ProductCategory.Retail, 7.50m, 2.40m, "tube", "Gel · 30 g", true, 8, "Retail shelf", ingredients: "chlorhexidine", stockCategory: "Topical");
        var flea = P("FLEA", "Spot-on flea treatment (dog 10–20 kg)", ProductCategory.Retail, 12.50m, 5.10m, "pipette", "Pipette · 1.34 ml", true, 12, "Retail shelf", stockCategory: "Parasiticide");
        var food = P("DIET", "Renal support diet 2 kg", ProductCategory.Retail, 24m, 13m, "bag", "Dry food · 2 kg", true, 4, "Store room", vat: VatRate.Zero, stockCategory: "Diet");
        var products = new[] { consult, recheck, nailClip, microchip, dental, neuter, meloxicam, amoxiclav, maropitant, methadone, ketamine, buprenorphine, kennelCough, lepto, catFlu, chlorhex, flea, food };
        _db.Products.AddRange(products);

        void Batch(Product p, string number, int expiryMonths, decimal quantity) =>
            p.Batches.Add(new StockBatch { Product = p, BatchNumber = number, Expiry = today.AddMonths(expiryMonths), Quantity = quantity, ReceivedUtc = now.AddDays(-30) });
        Batch(meloxicam, "MX2291", 14, 420);
        Batch(amoxiclav, "AC1187", 9, 48);       // below reorder level
        Batch(maropitant, "MP0442", 1, 26);      // expiring soon
        Batch(methadone, "ME7781", 18, 32);
        Batch(ketamine, "KT3310", 20, 18);
        Batch(buprenorphine, "BU5520", 11, 6);   // below reorder level
        Batch(kennelCough, "KC9014", 5, 22);
        Batch(lepto, "L40871", 2, 14);           // below reorder level
        Batch(catFlu, "FC2210", 7, 30);
        Batch(chlorhex, "CH0012", 24, 15);
        Batch(flea, "FL6631", 30, 9);            // below reorder level
        Batch(food, "RD1102", 10, 7);

        foreach (var cd in new[] { methadone, ketamine, buprenorphine })
            _db.ControlledDrugEntries.Add(new ControlledDrugEntry
            {
                Product = cd, Utc = now.AddDays(-30), Kind = ControlledDrugEntryKind.Received, Change = cd.OnHand, Balance = cd.OnHand,
                Reason = $"Opening balance, batch {cd.Batches[0].BatchNumber}", By = janet.ShortName, Witness = douglas.ShortName,
            });

        // Clients and pets
        Client C(string first, string last, string phone, string? email, string? address, string? postcode) =>
            new() { FirstName = first, LastName = last, Phone = phone, Email = email, Address = address, Postcode = postcode };
        Patient Pet(Client owner, string name, string species, string? breed, Sex sex, bool neutered, int ageMonths, string? chip = null)
        {
            var pet = new Patient { Name = name, Client = owner, Species = species, Breed = breed, Sex = sex, IsNeutered = neutered, DateOfBirth = today.AddMonths(-ageMonths).AddDays(-9), Microchip = chip };
            owner.Patients.Add(pet);
            return pet;
        }

        var clarke = C("Hannah", "Clarke", "07700 900412", "hannah.clarke@example.com", "22 Cotham Hill, Bristol", "BS6 5QT");
        var osei = C("Rachel", "Osei", "07700 900188", "r.osei@example.com", "5 Elton Road, Bristol", "BS7 8DA");
        var tanaka = C("Leo", "Tanaka", "07700 900233", "leo.tanaka@example.com", "71 St Michael's Hill, Bristol", "BS2 8BH");
        var novak = C("Sam", "Novak", "07700 900571", null, "3 Ashley Down Road, Bristol", "BS7 9JN");
        var hughes = C("Mark", "Hughes", "07700 900649", "mark.hughes@example.com", "48 Gloucester Road, Bristol", "BS7 8BH");
        var byrne = C("Aoife", "Byrne", "07700 900725", "aoife.b@example.com", "9 Redland Grove, Bristol", "BS6 6PR");
        var ferreira = C("Jorge", "Ferreira", "07700 900318", "jorge.f@example.com", "17 Zetland Road, Bristol", "BS6 7AH");
        var adeyemi = C("Kemi", "Adeyemi", "07700 900864", "kemi.a@example.com", "30 Chandos Road, Bristol", "BS6 6PF");
        var moore = C("Dan", "Moore", "07700 900127", null, "12 Sommerville Road, Bristol", "BS7 9AD");
        var evans = C("Tom", "Evans", "07700 900990", "tom.evans@example.com", "6 Belvoir Road, Bristol", "BS6 5DQ");
        var singh = C("Priya", "Singh", "07700 900455", "priya.singh@example.com", "88 Kings Drive, Bristol", "BS7 8JW");
        var wright = C("Grace", "Wright", "07700 900302", "grace.w@example.com", "2 Abbotsford Road, Bristol", "BS6 6HB");
        wright.RemindersOptIn = false;
        _db.Clients.AddRange(clarke, osei, tanaka, novak, hughes, byrne, ferreira, adeyemi, moore, evans, singh, wright);

        var biscuit = Pet(clarke, "Biscuit", "Dog", "Labrador Retriever", Sex.Male, true, 74, "826098100345219");
        var mabel = Pet(clarke, "Mabel", "Cat", "Domestic shorthair", Sex.Female, true, 38, "826098100771034");
        var juniper = Pet(osei, "Juniper", "Cat", "British Shorthair", Sex.Female, true, 49);
        var miso = Pet(tanaka, "Miso", "Cat", "Siamese", Sex.Male, false, 7);
        var pepper = Pet(novak, "Pepper", "Dog", "Border Collie", Sex.Female, true, 63, "826098100559812");
        var rufus = Pet(hughes, "Rufus", "Dog", "Cocker Spaniel", Sex.Male, true, 98, "826098100120044");
        var clover = Pet(byrne, "Clover", "Rabbit", "Netherland Dwarf", Sex.Female, true, 26);
        var atlas = Pet(ferreira, "Atlas", "Dog", "German Shepherd", Sex.Male, false, 31, "826098100902215");
        var luna = Pet(adeyemi, "Luna", "Cat", "Maine Coon", Sex.Female, true, 57);
        var bramble = Pet(moore, "Bramble", "Dog", "Jack Russell Terrier", Sex.Female, true, 120, "826098100004417");
        var ziggy = Pet(evans, "Ziggy", "Dog", "French Bulldog", Sex.Male, true, 44, "826098100662390");
        var nala = Pet(singh, "Nala", "Cat", "Bengal", Sex.Female, false, 14);
        var max = Pet(wright, "Max", "Dog", "Golden Retriever", Sex.Male, true, 86, "826098100233718");

        biscuit.Allergies.Add(new PatientAllergy { Patient = biscuit, Substance = "amoxicillin", DrugClass = "Penicillin" });
        biscuit.Alerts.Add(new PatientAlert { Patient = biscuit, Text = "Nervous — muzzle for nail clips", Severity = AlertSeverity.Warning, PresetKey = "5" });
        biscuit.Alerts.Add(new PatientAlert { Patient = biscuit, Text = "Insured", Severity = AlertSeverity.Note, PresetKey = "9" });
        ziggy.Alerts.Add(new PatientAlert { Patient = ziggy, Text = "Brachycephalic — airway risk", Severity = AlertSeverity.Warning, PresetKey = "6" });
        atlas.Alerts.Add(new PatientAlert { Patient = atlas, Text = "Bites when restrained", Severity = AlertSeverity.Alert, PresetKey = "1" });
        rufus.Alerts.Add(new PatientAlert { Patient = rufus, Text = "Heart murmur — anaesthetic risk", Severity = AlertSeverity.Alert, PresetKey = "4" });
        luna.Alerts.Add(new PatientAlert { Patient = luna, Text = "Diabetic — on insulin", Severity = AlertSeverity.Alert, PresetKey = "2" });

        // Appointment types come from the initializer.
        var types = await _db.AppointmentTypes.ToDictionaryAsync(t => t.Name, cancellationToken);

        Appointment Book(Patient pet, string type, int day, int hour, int minute, string reason, StaffMember? vet, int? minutes = null)
        {
            var appointment = new Appointment { Patient = pet, Type = types[type], StartUtc = At(day, hour, minute), Minutes = minutes ?? types[type].Minutes, Reason = reason, Clinician = vet };
            _db.Appointments.Add(appointment);
            return appointment;
        }

        // History: completed consultations with invoices, some paid.
        var invoiceNumber = 1040;
        Invoice History(Patient pet, StaffMember vet, int daysAgo, string reason, string s, string o, string a, string plan, decimal? weight,
            IEnumerable<(Product Product, decimal Qty)> items, PaymentMethod? paidBy, string? type = "Consultation")
        {
            var started = At(-daysAgo, 10, 30);
            var appointment = type is null ? null : new Appointment { Patient = pet, Type = types[type], StartUtc = started, Minutes = types[type].Minutes, Reason = reason, Clinician = vet };
            if (appointment is not null)
            {
                appointment.ChangeStatus(AppointmentStatus.InConsult, started);
                appointment.ChangeStatus(AppointmentStatus.Done, started.AddMinutes(15));
                _db.Appointments.Add(appointment);
            }

            var consultation = new Consultation
            {
                Patient = pet, Clinician = vet, Appointment = appointment, StartedUtc = started, Reason = reason,
                Subjective = s, Objective = o, Assessment = a, Plan = plan, WeightKg = weight, TemperatureC = 38.4m, HeartRate = 96, RespiratoryRate = 24,
                MucousMembranes = "Pink, moist", CapillaryRefill = "< 2 s",
            };
            foreach (var (product, qty) in items)
                consultation.Charges.Add(new ConsultationCharge { Consultation = consultation, Product = product, Description = product.Name, Quantity = qty, UnitPrice = product.UnitPrice, VatRate = product.VatRate });

            var invoice = new Invoice
            {
                Number = Invoice.FormatNumber(invoiceNumber++), Client = pet.Client, Patient = pet, IssuedUtc = started.AddMinutes(20),
                DueDate = today.AddDays(-daysAgo + settings.PaymentTermsDays),
            };
            invoice.Lines.AddRange(consultation.Charges.Select(ch => new InvoiceLine { Invoice = invoice, Product = ch.Product, Description = ch.Description, Quantity = ch.Quantity, UnitPrice = ch.UnitPrice, VatRate = ch.VatRate }));
            invoice.SetTotals(VatCalculator.For(invoice.Lines));
            if (paidBy is { } method)
                invoice.Payments.Add(new Payment { Invoice = invoice, Amount = invoice.Total, Method = method, TakenBy = liam.ShortName, TakenUtc = started.AddMinutes(25), Reference = method == PaymentMethod.Card ? "Terminal 1" : null });
            consultation.MarkCompleted(started.AddMinutes(20), invoice);
            _db.Consultations.Add(consultation);
            _db.Invoices.Add(invoice);

            foreach (var vaccine in consultation.Charges.Where(ch => ch.Product.Category == ProductCategory.Vaccine))
                _db.Vaccinations.Add(new Vaccination
                {
                    Patient = pet, VaccineName = vaccine.Product.Name, GivenUtc = started, GivenBy = vet.ShortName, Batch = vaccine.Product.Batches.FirstOrDefault()?.BatchNumber,
                    DueDate = today.AddDays(-daysAgo).AddMonths(vaccine.Product.BoosterMonths ?? 12),
                });
            if (weight is { } kg)
                _db.WeightRecords.Add(new WeightRecord { Patient = pet, Kg = kg, RecordedUtc = started, Context = reason, RecordedBy = vet.ShortName });
            return invoice;
        }

        History(biscuit, douglas, 352, "Annual booster", "Owner reports well. Eating and drinking normally.", "BCS 6/9. Heart and lungs normal. Teeth grade 1 tartar.",
            "Healthy. Slightly overweight.", "Lepto 4 given. Diet advice. Recheck weight in 3 months.", 32.4m, [(consult, 1), (lepto, 1)], PaymentMethod.Card);
        History(biscuit, heather, 180, "Ear infection", "Shaking head for 3 days, scratching left ear.", "Left ear canal erythematous, brown discharge. Right ear normal.",
            "Otitis externa, left.", "Ear cleaner and drops. Avoid amoxicillin (allergy). Recheck in 10 days.", 31.8m, [(consult, 1), (chlorhex, 1)], PaymentMethod.Cash);
        History(biscuit, douglas, 21, "Lameness, right fore", "Limping after a long walk, worse after rest.", "Mild pain on extension of right elbow. No swelling.",
            "Suspect early elbow osteoarthritis.", "Meloxicam 21 days. Restricted exercise. Recheck in 3 weeks.", 31.2m, [(consult, 1), (meloxicam, 30)], PaymentMethod.Card);
        History(mabel, jen, 330, "Vaccination", "Indoor/outdoor cat, well.", "Healthy exam.", "Healthy.", "Feline flu and enteritis vaccine given.", 4.1m, [(consult, 1), (catFlu, 1)], PaymentMethod.Card);
        History(rufus, heather, 45, "Cough", "Soft cough at night for 2 weeks.", "Grade 3/6 left apical systolic murmur. Lungs clear.",
            "Murmur, likely degenerative mitral valve disease.", "Refer for echo. Recheck in 4 weeks.", 14.6m, [(consult, 1)], null);
        History(pepper, douglas, 12, "Vomiting", "Vomited 4 times yesterday, now bright.", "Mild abdominal discomfort. Temp 38.9.",
            "Acute gastritis, likely dietary.", "Maropitant injection. Bland diet 3 days.", 19.2m, [(consult, 1), (maropitant, 2)], null);
        History(luna, jen, 9, "Diabetic check", "Drinking less since insulin dose change.", "BCS 5/9. Hydrated.", "Diabetes well controlled.",
            "Continue insulin. Fructosamine next visit.", 5.8m, [(consult, 1)], PaymentMethod.Transfer);
        var overdue = History(max, heather, 40, "Skin itch", "Itchy paws and belly.", "Erythema on ventral abdomen, no parasites seen.",
            "Suspect atopic dermatitis.", "Flea treatment. Diet trial discussed.", 36.0m, [(consult, 1), (flea, 1)], null);
        var claimed = History(atlas, douglas, 5, "Wound on paw", "Cut pad on glass yesterday.", "2 cm laceration right hind pad, clean.", "Pad laceration.",
            "Cleaned and dressed. Recheck in 3 days.", 38.5m, [(consult, 1), (chlorhex, 1)], null);
        claimed.Claim = new InsuranceClaim { Invoice = claimed, Insurer = "Petplan", PolicyNumber = "PP-4471902", Excess = 99m, Condition = "Pad laceration", CreatedUtc = now.AddDays(-4) };
        var partPaid = History(bramble, jen, 3, "Dental check", "Bad breath.", "Grade 2 tartar, gingivitis.", "Periodontal disease.", "Book dental under GA.", 7.4m,
            [(consult, 1), (chlorhex, 1)], null);
        partPaid.Payments.Add(new Payment { Invoice = partPaid, Amount = 30m, Method = PaymentMethod.Cash, TakenBy = liam.ShortName, TakenUtc = At(-3, 11) });
        _ = overdue;

        // Vaccinations due soon, for the reminders list.
        _db.Vaccinations.Add(new Vaccination { Patient = juniper, VaccineName = catFlu.Name, GivenUtc = At(-350, 9), GivenBy = jen.ShortName, DueDate = today.AddDays(15) });
        _db.Vaccinations.Add(new Vaccination { Patient = ziggy, VaccineName = lepto.Name, GivenUtc = At(-372, 9), GivenBy = heather.ShortName, DueDate = today.AddDays(-7) });
        _db.Vaccinations.Add(new Vaccination { Patient = pepper, VaccineName = kennelCough.Name, GivenUtc = At(-360, 9), GivenBy = douglas.ShortName, DueDate = today.AddDays(5) });

        // Today, as on the design's Today screen (only on days the practice is open).
        if (settings.HoursOn(today.DayOfWeek) is not null)
        {
            var done = Book(juniper, "Vaccination", 0, 8, 30, "Annual booster", jen);
            done.ChangeStatus(AppointmentStatus.InConsult, At(0, 8, 30));
            done.ChangeStatus(AppointmentStatus.Done, At(0, 8, 45));
            var inConsult = Book(biscuit, "Recheck", 0, 9, 0, "Lameness recheck", douglas);
            inConsult.ChangeStatus(AppointmentStatus.CheckedIn, At(0, 8, 52));
            inConsult.ChangeStatus(AppointmentStatus.InConsult, At(0, 9, 2));
            _db.Consultations.Add(new Consultation
            {
                Patient = biscuit, Appointment = inConsult, Clinician = douglas, StartedUtc = At(0, 9, 2), Reason = "Lameness recheck",
                Subjective = "Much improved on meloxicam. Still a little stiff in the mornings.",
                Charges = { new ConsultationCharge { Product = recheck, Description = recheck.Name, Quantity = 1, UnitPrice = recheck.UnitPrice, VatRate = recheck.VatRate } },
            });
            var waiting = Book(miso, "Consultation", 0, 9, 15, "Vomiting since yesterday", heather);
            waiting.ChangeStatus(AppointmentStatus.CheckedIn, At(0, 9, 10));
            Book(atlas, "Surgery", 0, 9, 30, "Castration", jen, 120);
            Book(pepper, "Consultation", 0, 10, 15, "Itchy skin", douglas);
            Book(clover, "Nurse clinic", 0, 10, 45, "Nail clip and weight check", null);
            Book(ziggy, "Consultation", 0, 11, 30, "Breathing noisily", heather);
            // Saturday closes at lunchtime.
            if (today.DayOfWeek != DayOfWeek.Saturday)
            {
                Book(nala, "Vaccination", 0, 14, 0, "Second kitten vaccination", jen);
                Book(rufus, "Recheck", 0, 14, 30, "Murmur recheck", heather);
                Book(bramble, "Dental", 0, 15, 0, "Dental scale and polish", douglas);
                Book(max, "Consultation", 0, 16, 15, "Skin recheck", douglas);
                Book(luna, "Nurse clinic", 0, 17, 0, "Insulin technique check", null);
            }
        }

        // The rest of the week.
        for (var day = 1; day <= 6; day++)
        {
            if (settings.HoursOn(today.AddDays(day).DayOfWeek) is null)
                continue;
            Book(mabel, "Consultation", day, 9, 0, "Weight loss", jen);
            Book(pepper, "Recheck", day, 10, 30, "Gastritis recheck", douglas);
            if (today.AddDays(day).DayOfWeek != DayOfWeek.Saturday)
            {
                Book(rufus, "Surgery", day, 14, 0, "Mass removal", heather, 90);
                Book(juniper, "Nurse clinic", day, 16, 0, "Weight clinic", null);
            }
        }

        // Pharmacy queue: one from the open consultation path, one controlled drug, one repeat needing a vet.
        _db.DispenseItems.Add(new DispenseItem { Patient = pepper, Product = meloxicam, Quantity = 30, Directions = "1 ml once daily with food for 21 days", PrescribedBy = douglas.ShortName });
        _db.DispenseItems.Add(new DispenseItem { Patient = atlas, Product = methadone, Quantity = 0.8m, Directions = "Pre-medication, IV", PrescribedBy = jen.ShortName });
        _db.DispenseItems.Add(new DispenseItem { Patient = luna, Product = food, Quantity = 1, Directions = "Repeat request by phone", PrescribedBy = liam.ShortName, RequiresAuthorisation = true });

        // A delivery on its way.
        var order = new PurchaseOrder { Number = PurchaseOrder.FormatNumber(1), Supplier = "Covetrus", DeliverBy = today.AddDays(1), Status = PurchaseOrderStatus.Sent, SentUtc = now.AddDays(-1) };
        order.Lines.Add(new PurchaseOrderLine { Order = order, Product = amoxiclav, Quantity = 100 });
        order.Lines.Add(new PurchaseOrderLine { Order = order, Product = lepto, Quantity = 20 });
        _db.PurchaseOrders.Add(order);

        _db.NumberSequences.Add(new NumberSequence { Name = NumberSequence.Invoice, Next = invoiceNumber });
        _db.NumberSequences.Add(new NumberSequence { Name = NumberSequence.PurchaseOrder, Next = 2 });

        _db.FollowUps.AddRange(
            new FollowUp { Text = "Call Mr Hughes with Rufus's referral appointment", Patient = rufus, DueDate = today },
            new FollowUp { Text = "Chase Petplan on Atlas's claim", Patient = atlas, DueDate = today },
            new FollowUp { Text = "Send Max's owner a reminder about the unpaid invoice", Patient = max, DueDate = today.AddDays(-2) });

        _db.Notifications.AddRange(
            new Notification { CreatedUtc = At(0, 9, 10), Text = "Miso checked in for vomiting since yesterday", Kind = NotificationKind.Success, Link = $"/patients" },
            new Notification { CreatedUtc = At(0, 8, 0), Text = "Amoxicillin/clavulanate 250 mg tablets is below its reorder level", Kind = NotificationKind.Warning, Link = "/pharmacy" },
            new Notification { CreatedUtc = At(-1, 17, 30), Text = "Delivery PO-0001 from Covetrus is due tomorrow", Kind = NotificationKind.Info, Link = "/pharmacy" });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
