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
        var castrate = P("CAST", "Dog castration", ProductCategory.Service, 240m);
        var spay = P("SPAY", "Bitch spay", ProductCategory.Service, 320m);
        var bloods = P("BLOOD", "In-house blood panel", ProductCategory.Service, 89m);
        var xray = P("XRAY", "Radiography (per study)", ProductCategory.Service, 145m);
        var urinalysis = P("URIN", "Urinalysis", ProductCategory.Service, 32m);
        var euthanasia = P("EUTH", "Euthanasia", ProductCategory.Service, 180m);
        var cremation = P("CREM", "Individual cremation, ashes returned", ProductCategory.Service, 165m);
        var pimobendan = P("PIMO", "Pimobendan 5 mg chewable tablets", ProductCategory.Medicine, 1.10m, 0.42m, "tablet", "Tablet · 5 mg", true, 50, "Shelf A2", ingredients: "pimobendan", stockCategory: "Cardiac");
        var prednisolone = P("PRED", "Prednisolone 5 mg tablets", ProductCategory.Medicine, 0.30m, 0.06m, "tablet", "Tablet · 5 mg", true, 100, "Shelf A1", drugClass: "Corticosteroid", ingredients: "prednisolone", stockCategory: "Anti-inflammatory");
        var cefalexin = P("CEFA", "Cefalexin 250 mg tablets", ProductCategory.Medicine, 0.75m, 0.22m, "tablet", "Tablet · 250 mg", true, 60, "Shelf A1", drugClass: "Cephalosporin", ingredients: "cefalexin", stockCategory: "Antibiotic");
        var insulin = P("INSU", "Porcine insulin 40 IU/ml, 10 ml vial", ProductCategory.Medicine, 42.50m, 21m, "vial", "Injection · 10 ml", true, 2, "Fridge 1", ingredients: "insulin (porcine)", stockCategory: "Endocrine");
        var rabbitVaccine = P("RHD", "Rabbit myxomatosis and RHD vaccine", ProductCategory.Vaccine, 52m, 12.40m, "dose", "Injection · 1 dose", true, 10, "Fridge 1", booster: 12);
        var wormer = P("WORM", "Worming tablet (dog 5–25 kg)", ProductCategory.Retail, 9.80m, 3.60m, "tablet", "Tablet · 1", true, 20, "Retail shelf", stockCategory: "Parasiticide");
        var catFlea = P("FLEAC", "Spot-on flea treatment (cat)", ProductCategory.Retail, 10.50m, 4.20m, "pipette", "Pipette · 0.5 ml", true, 12, "Retail shelf", stockCategory: "Parasiticide");
        var products = new[]
        {
            consult, recheck, nailClip, microchip, dental, neuter, meloxicam, amoxiclav, maropitant, methadone, ketamine, buprenorphine, kennelCough, lepto, catFlu, chlorhex, flea, food,
            castrate, spay, bloods, xray, urinalysis, euthanasia, cremation, pimobendan, prednisolone, cefalexin, insulin, rabbitVaccine, wormer, catFlea,
        };
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
        Batch(pimobendan, "PB4410", 16, 180);
        Batch(prednisolone, "PR2093", 22, 250);
        Batch(cefalexin, "CF7724", 1, 40);       // older batch, expiring soon
        Batch(cefalexin, "CF8102", 13, 100);
        Batch(insulin, "IN0339", 6, 1);          // below reorder level
        Batch(rabbitVaccine, "RH1156", 3, 12);
        Batch(wormer, "WM5518", 28, 64);
        Batch(catFlea, "FC0907", 26, 3);         // below reorder level

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

        // A wider list of clients across north and central Bristol, so search, filters and sorting have something to
        // work through: households with several pets, rabbits and guinea pigs, clients with no email or a landline.
        var patel = C("Anil", "Patel", "07700 900014", "anil.patel@example.com", "41 Filton Avenue, Bristol", "BS7 0AQ");
        var oconnor = C("Siobhan", "O'Connor", "07700 900026", "siobhan.oc@example.com", "15 Effingham Road, Bristol", "BS6 5AY");
        var walker = C("Ben", "Walker", "07700 900041", "ben.walker@example.com", "7 Cranbrook Road, Bristol", "BS6 7BJ");
        var nowak = C("Magda", "Nowak", "07700 900059", null, "120 Stapleton Road, Bristol", "BS5 0PU");
        var thompson = C("Eileen", "Thompson", "0117 496 0587", null, "4 Henleaze Gardens, Bristol", "BS9 4HJ");
        var ahmed = C("Yusuf", "Ahmed", "07700 900077", "yusuf.ahmed@example.com", "62 Ashley Road, Bristol", "BS6 5NR");
        var robinson = C("Chloe", "Robinson", "07700 900085", "chloe.robinson@example.com", "23 North Road, Bristol", "BS6 5AF");
        var mensah = C("Kwame", "Mensah", "07700 900096", "kwame.m@example.com", "9 Brynland Avenue, Bristol", "BS7 9DR");
        var davies = C("Rhys", "Davies", "07700 900103", "rhys.davies@example.com", "31 Nevil Road, Bristol", "BS7 9EG");
        var murphy = C("Ciara", "Murphy", "07700 900119", "ciara.murphy@example.com", "18 Overton Road, Bristol", "BS6 6PU");
        var lewis = C("Owen", "Lewis", "07700 900142", "owen.lewis@example.com", "5 Southmead Road, Bristol", "BS10 5DP");
        var kowalski = C("Piotr", "Kowalski", "07700 900156", null, "77 Fishponds Road, Bristol", "BS5 6SF");
        var green = C("Sophie", "Green", "07700 900167", "sophie.green@example.com", "2 Clifton Park Road, Bristol", "BS8 3HL");
        var hassan = C("Amira", "Hassan", "07700 900174", "amira.h@example.com", "44 Picton Street, Bristol", "BS6 5QA");
        var barker = C("Neil", "Barker", "07700 900199", "neil.barker@example.com", "13 Coldharbour Road, Bristol", "BS6 7JU");
        var campbell = C("Isla", "Campbell", "07700 900205", "isla.campbell@example.com", "56 Wells Road, Bristol", "BS4 2AF");
        var chen = C("Wei", "Chen", "07700 900216", "wei.chen@example.com", "8 Wedmore Vale, Bristol", "BS3 5HL");
        var foster = C("Lucy", "Foster", "07700 900248", "lucy.foster@example.com", "27 North Street, Bristol", "BS3 1EN");
        var oyelaran = C("Tunde", "Oyelaran", "07700 900261", "tunde.o@example.com", "3 Station Road, Bristol", "BS16 3SG");
        var baker = C("Margaret", "Baker", "07700 900277", null, "11 Westbury Hill, Bristol", "BS9 3AG");
        var kaur = C("Jaspreet", "Kaur", "07700 900289", "jaspreet.kaur@example.com", "90 Church Road, Bristol", "BS5 9JZ");
        var price = C("Ellie", "Price", "07700 900334", "ellie.price@example.com", "19 Raleigh Road, Bristol", "BS3 1QR");
        price.RemindersOptIn = false;
        var hall = C("James", "Hall", "07700 900347", "james.hall@example.com", "6 Downs Park East, Bristol", "BS6 7QD");
        var silva = C("Beatriz", "Silva", "07700 900362", "bia.silva@example.com", "35 Cheltenham Road, Bristol", "BS6 5RW");
        _db.Clients.AddRange(patel, oconnor, walker, nowak, thompson, ahmed, robinson, mensah, davies, murphy, lewis, kowalski,
            green, hassan, barker, campbell, chen, foster, oyelaran, baker, kaur, price, hall, silva);

        var bruno = Pet(patel, "Bruno", "Dog", "Staffordshire Bull Terrier", Sex.Male, true, 52, "826098100418823");
        var poppy = Pet(oconnor, "Poppy", "Dog", "Cavalier King Charles Spaniel", Sex.Female, true, 91, "826098100537106");
        var hazel = Pet(walker, "Hazel", "Dog", "Cockapoo", Sex.Female, false, 5);
        var kot = Pet(nowak, "Kot", "Cat", "Domestic shorthair", Sex.Male, true, 132);
        var smudge = Pet(thompson, "Smudge", "Cat", "Domestic longhair", Sex.Female, true, 214, "826098100011592");
        var dottie = Pet(thompson, "Dottie", "Cat", "Domestic shorthair", Sex.Female, true, 101, "826098100011607");
        var simba = Pet(ahmed, "Simba", "Cat", "Ragdoll", Sex.Male, true, 39, "826098100683340");
        var willow = Pet(robinson, "Willow", "Dog", "Whippet", Sex.Female, true, 28, "826098100775201");
        var kofi = Pet(mensah, "Kofi", "Dog", "Labrador Retriever", Sex.Male, false, 11, "826098100860419");
        var dai = Pet(davies, "Dai", "Dog", "Welsh Springer Spaniel", Sex.Male, true, 69, "826098100294476");
        var tilly = Pet(murphy, "Tilly", "Cat", "Burmese", Sex.Female, true, 77, "941000024681357");
        var biggles = Pet(lewis, "Biggles", "Guinea pig", "Abyssinian", Sex.Male, false, 22);
        var pip = Pet(lewis, "Pip", "Guinea pig", "American", Sex.Male, false, 22);
        var reksio = Pet(kowalski, "Reksio", "Dog", "Crossbreed", Sex.Male, true, 104, "826098100340958");
        var olive = Pet(green, "Olive", "Dog", "Dachshund (miniature)", Sex.Female, true, 47, "826098100509137");
        var noor = Pet(hassan, "Noor", "Cat", "Persian", Sex.Female, true, 60);
        var duke = Pet(barker, "Duke", "Dog", "Boxer", Sex.Male, true, 83, "826098100122680");
        var thistle = Pet(campbell, "Thistle", "Rabbit", "Lionhead", Sex.Male, true, 33);
        var bramley = Pet(campbell, "Bramley", "Rabbit", "Mini Lop", Sex.Female, true, 33);
        var mochi = Pet(chen, "Mochi", "Dog", "Shiba Inu", Sex.Female, true, 40, "826098100951762");
        var bear = Pet(foster, "Bear", "Dog", "Newfoundland", Sex.Male, false, 26, "826098100664025");
        var ginger = Pet(foster, "Ginger", "Cat", "Domestic shorthair", Sex.Male, true, 66);
        var zuri = Pet(oyelaran, "Zuri", "Dog", "Rhodesian Ridgeback", Sex.Female, true, 55, "826098100387714");
        var sherlock = Pet(baker, "Sherlock", "Dog", "Basset Hound", Sex.Male, true, 118, "826098100207359");
        var rani = Pet(kaur, "Rani", "Cat", "Abyssinian", Sex.Female, false, 4);
        var otis = Pet(price, "Otis", "Dog", "Greyhound", Sex.Male, true, 72, "826098100448801");
        var fudge = Pet(hall, "Fudge", "Other", "Ferret", Sex.Male, true, 30);
        var bolo = Pet(silva, "Bolo", "Dog", "Pug", Sex.Male, true, 61, "826098100573218");
        smudge.IsDeceased = true;

        tilly.Allergies.Add(new PatientAllergy { Patient = tilly, Substance = "meloxicam", DrugClass = "NSAID" });
        duke.Allergies.Add(new PatientAllergy { Patient = duke, Substance = "cefalexin", DrugClass = "Cephalosporin" });
        poppy.Alerts.Add(new PatientAlert { Patient = poppy, Text = "Heart murmur — anaesthetic risk", Severity = AlertSeverity.Alert, PresetKey = "4" });
        poppy.Alerts.Add(new PatientAlert { Patient = poppy, Text = "Insured", Severity = AlertSeverity.Note, PresetKey = "9" });
        dai.Alerts.Add(new PatientAlert { Patient = dai, Text = "Epileptic", Severity = AlertSeverity.Alert, PresetKey = "3" });
        bolo.Alerts.Add(new PatientAlert { Patient = bolo, Text = "Brachycephalic — airway risk", Severity = AlertSeverity.Warning, PresetKey = "6" });
        noor.Alerts.Add(new PatientAlert { Patient = noor, Text = "Aggressive — handle with care", Severity = AlertSeverity.Alert, PresetKey = "0" });
        zuri.Alerts.Add(new PatientAlert { Patient = zuri, Text = "Insured", Severity = AlertSeverity.Note, PresetKey = "9" });
        fudge.Alerts.Add(new PatientAlert { Patient = fudge, Text = "Escape risk — keep doors closed", Severity = AlertSeverity.Warning, PresetKey = "7" });
        sherlock.Alerts.Add(new PatientAlert { Patient = sherlock, Text = "Owner hard of hearing", Severity = AlertSeverity.Note, PresetKey = "11" });
        reksio.Alerts.Add(new PatientAlert { Patient = reksio, Text = "Payment required upfront", Severity = AlertSeverity.Warning, PresetKey = "8" });
        olive.Alerts.Add(new PatientAlert { Patient = olive, Text = "Microchip details need updating", Severity = AlertSeverity.Note, PresetKey = "12" });

        // Appointment types come from the initializer.
        var types = await _db.AppointmentTypes.ToDictionaryAsync(t => t.Name, cancellationToken);

        Appointment Book(Patient pet, string type, int day, int hour, int minute, string reason, StaffMember? vet, int? minutes = null)
        {
            var appointment = new Appointment { Patient = pet, Type = types[type], StartUtc = At(day, hour, minute), Minutes = minutes ?? types[type].Minutes, Reason = reason, Clinician = vet };
            _db.Appointments.Add(appointment);
            return appointment;
        }

        // History: completed consultations with invoices, some paid. A visit that would fall on a day the practice is
        // closed moves back to the day before it was open.
        int Open(int daysAgo)
        {
            while (settings.HoursOn(today.AddDays(-daysAgo).DayOfWeek) is null)
                daysAgo++;
            return daysAgo;
        }
        var invoiceNumber = 1040;
        Invoice History(Patient pet, StaffMember vet, int daysAgo, string reason, string s, string o, string a, string plan, decimal? weight,
            IEnumerable<(Product Product, decimal Qty)> items, PaymentMethod? paidBy, string? type = "Consultation", int hour = 10, int minute = 30)
        {
            daysAgo = Open(daysAgo);
            var started = At(-daysAgo, hour, minute);
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
        partPaid.Payments.Add(new Payment { Invoice = partPaid, Amount = 30m, Method = PaymentMethod.Cash, TakenBy = liam.ShortName, TakenUtc = partPaid.IssuedUtc.AddMinutes(5) });
        _ = overdue;

        // A year of visits for the wider client list, so reports, last-visit sorting and the invoice list have a
        // realistic spread: paid by card, cash and transfer, unpaid, overdue and claimed.
        History(bruno, douglas, Open(340), "Annual booster", "Well. Lives with a young family, walks twice daily.", "BCS 5/9. Heart and lungs normal. Mild tartar.",
            "Healthy.", "Lepto 4 given. Dental chews suggested.", 24.6m, [(consult, 1), (lepto, 1)], PaymentMethod.Card, "Vaccination", 9, 15);
        History(bruno, heather, Open(26), "Itchy ears", "Scratching both ears for a week, worse after swimming.", "Both ear canals red with waxy discharge. Tympanic membranes intact.",
            "Bilateral otitis externa.", "Clean ears twice weekly. Prednisolone 5 days. Recheck if not settled.", 25.1m, [(consult, 1), (prednisolone, 10)], PaymentMethod.Card, hour: 11);
        var poppyClaim = History(poppy, heather, Open(120), "Heart check", "Breathing faster at rest, tires on walks.", "Grade 4/6 left apical murmur. Resting respiratory rate 36.",
            "Mitral valve disease, stage B2.", "Chest radiographs taken. Start pimobendan twice daily. Recheck in 3 months.", 9.2m, [(consult, 1), (xray, 1), (pimobendan, 60)], null);
        poppyClaim.Claim = new InsuranceClaim { Invoice = poppyClaim, Insurer = "Animal Friends", PolicyNumber = "AF-2290614", Excess = 120m, Condition = "Mitral valve disease", CreatedUtc = At(-Open(120), 12), Status = ClaimStatus.Settled };
        poppyClaim.Payments.Add(new Payment { Invoice = poppyClaim, Amount = 120m, Method = PaymentMethod.Card, TakenBy = liam.ShortName, TakenUtc = At(-Open(120), 11), Reference = "Excess" });
        poppyClaim.Payments.Add(new Payment { Invoice = poppyClaim, Amount = poppyClaim.Total - 120m, Method = PaymentMethod.Transfer, TakenBy = liam.ShortName, TakenUtc = At(-Open(92), 9), Reference = "Animal Friends claim settlement" });
        History(poppy, heather, Open(14), "Pimobendan recheck", "Brighter on the tablets, sleeping respiratory rate under 30.", "Murmur unchanged. Lungs clear.",
            "Stable on pimobendan.", "Continue. Repeat prescription supplied. Recheck in 3 months.", 9.4m, [(recheck, 1), (pimobendan, 60)], PaymentMethod.Card, "Recheck", 11, 45);
        History(hazel, jen, Open(33), "First puppy vaccination", "New puppy from a breeder in Somerset. Eating well.", "Bright, healthy puppy. Umbilical hernia, small, reducible.",
            "Healthy puppy.", "Lepto 4 first dose, second in 4 weeks. Wormed today.", 4.3m, [(consult, 1), (lepto, 1), (wormer, 1)], PaymentMethod.Card, "Vaccination", 9, 45);
        History(kot, jen, Open(62), "Weight loss and thirst", "Eating more but losing weight, drinking a lot.", "BCS 3/9. Palpable thyroid nodule, heart rate 220.",
            "Suspect hyperthyroidism. Rule out kidney disease.", "Bloods and urine today. Call owner with results.", 3.6m, [(consult, 1), (bloods, 1), (urinalysis, 1)], PaymentMethod.Card, hour: 9);
        History(dottie, jen, Open(345), "Annual booster", "Well. Lives with Smudge.", "Healthy exam. Mild dental disease.", "Healthy.", "Feline flu and enteritis vaccine given.",
            4.4m, [(consult, 1), (catFlu, 1)], PaymentMethod.Cash, "Vaccination", 11);
        History(smudge, douglas, Open(200), "Kidney check", "Drinking more, fussy with food.", "BCS 4/9. Small, firm kidneys.", "Chronic kidney disease, IRIS stage 2.",
            "Renal diet. Recheck bloods in 3 months.", 3.2m, [(consult, 1), (bloods, 1), (food, 1)], PaymentMethod.Cash, hour: 11, minute: 15);
        History(smudge, douglas, Open(58), "Quality of life", "Not eating for 3 days, hiding. Owner has decided it is time.", "Dehydrated, BCS 2/9, uraemic breath.",
            "End-stage kidney disease.", "Euthanasia with owner present. Individual cremation, ashes to be collected.", 2.9m, [(euthanasia, 1), (cremation, 1)], PaymentMethod.Card, "Euthanasia", 12);
        History(simba, douglas, Open(7), "Straining in litter tray", "Going to the tray often, passing small amounts.", "Small, non-painful bladder. Not blocked.",
            "Idiopathic cystitis.", "Urinalysis today. Meloxicam 5 days, wet food, more water stations.", 5.9m, [(consult, 1), (urinalysis, 1), (meloxicam, 3)], PaymentMethod.Card, hour: 9);
        History(willow, heather, Open(95), "Cut on flank", "Caught on barbed wire on a walk this morning.", "4 cm skin tear right flank, superficial, clean edges.",
            "Skin laceration.", "Clipped, cleaned, left to heal. Cefalexin 7 days.", 14.8m, [(consult, 1), (cefalexin, 14), (chlorhex, 1)], PaymentMethod.Card, hour: 11, minute: 30);
        History(kofi, douglas, Open(2), "Ate a sock", "Seen eating a sock last night, vomited once this morning.", "Bright. Mild cranial abdominal discomfort.",
            "Possible foreign body.", "Abdominal radiographs: no obstruction seen. Bland diet, watch for vomiting, call if worse.", 29.5m, [(consult, 1), (xray, 1)], null, hour: 11, minute: 15);
        History(dai, jen, Open(150), "Seizure", "Two-minute fit last night, wobbly for an hour after.", "Normal neurological exam today.",
            "Suspect idiopathic epilepsy.", "Bloods to rule out other causes. Seizure diary.", 21.0m, [(consult, 1), (bloods, 1)], PaymentMethod.Card, hour: 9, minute: 30);
        History(dai, jen, Open(16), "Epilepsy review", "One seizure in the last month, short.", "Normal exam.", "Epilepsy, well controlled.",
            "Continue medication. Review in 3 months.", 21.4m, [(recheck, 1)], PaymentMethod.Transfer, "Recheck", 12);
        History(tilly, heather, Open(210), "Limping", "Holding up left fore since jumping off a wardrobe.", "Pain on flexing the left carpus, no fracture felt.",
            "Soft tissue injury, left carpus.", "Meloxicam avoided (allergy). Prednisolone 5 days, cage rest.", 4.0m, [(consult, 1), (prednisolone, 5)], PaymentMethod.Card, hour: 11);
        History(biggles, lesley, Open(40), "Nail clip and weight check", "Eating well.", "Nails overgrown. Teeth fine.", "Healthy.", "Nails clipped. Weigh again in a month.",
            1.1m, [(nailClip, 1)], PaymentMethod.Cash, "Nurse clinic", 10);
        History(reksio, douglas, Open(75), "Dental", "Bad breath, dropping food.", "Grade 3 tartar, two loose premolars extracted under GA.",
            "Periodontal disease.", "Amoxicillin/clavulanate 7 days. Soft food 3 days. Check-up in 2 weeks.", 18.2m, [(dental, 1), (amoxiclav, 14)], null, "Dental", 9);
        var olivesClaim = History(olive, heather, Open(23), "Back pain", "Crying when picked up, reluctant to use stairs.", "Pain on palpating the thoracolumbar spine. Walking, reflexes normal.",
            "Suspect intervertebral disc disease, grade 1.", "Radiographs. Strict crate rest 4 weeks. Meloxicam. Refer if any weakness.", 6.1m, [(consult, 1), (xray, 1), (meloxicam, 10)], null, hour: 9, minute: 45);
        olivesClaim.Claim = new InsuranceClaim { Invoice = olivesClaim, Insurer = "Petplan", PolicyNumber = "PP-5583021", Excess = 99m, Condition = "Intervertebral disc disease", CreatedUtc = At(-Open(22), 10) };
        History(duke, douglas, Open(300), "Annual booster", "Well. Going into kennels next month.", "Healthy exam.", "Healthy.", "Lepto 4 and kennel cough vaccine given.",
            33.0m, [(consult, 1), (lepto, 1), (kennelCough, 1)], PaymentMethod.Card, "Vaccination", 11, 30);
        History(duke, heather, Open(4), "Lump on chest", "Owner found a soft lump while stroking him.", "3 cm soft, mobile subcutaneous mass, left chest wall.",
            "Lipoma on fine needle aspirate.", "Monitor. Measure at next visit.", 33.8m, [(consult, 1)], PaymentMethod.Cash, hour: 9, minute: 15);
        History(thistle, jen, Open(355), "Annual vaccination", "Lives outdoors with Bramley.", "Healthy. Teeth normal.", "Healthy.", "Myxomatosis and RHD vaccine given.",
            1.9m, [(consult, 1), (rabbitVaccine, 1)], PaymentMethod.Card, "Vaccination", 10);
        History(bramley, jen, Open(355), "Annual vaccination", "Lives outdoors with Thistle.", "Healthy. Slight overgrowth of incisors.", "Healthy.",
            "Myxomatosis and RHD vaccine given. More hay in the diet.", 1.7m, [(rabbitVaccine, 1)], PaymentMethod.Card, "Vaccination", 10, 15);
        History(mochi, jen, Open(110), "Vomiting", "Vomited 3 times after eating a chicken bone on a walk.", "Mild discomfort, otherwise bright.",
            "Gastritis.", "Maropitant injection. Small bland meals.", 9.6m, [(consult, 1), (maropitant, 1)], PaymentMethod.Transfer, hour: 12);
        History(bear, douglas, Open(50), "Lameness, left hind", "Lame after playing in the park.", "Mild pain on stifle extension. No drawer movement.",
            "Soft tissue strain, rule out cruciate disease.", "Radiographs normal. Meloxicam 10 days, lead walks. Recheck in 2 weeks.", 58.0m, [(consult, 1), (xray, 1), (meloxicam, 40)], PaymentMethod.Card, hour: 9);
        History(zuri, heather, Open(200), "Spay", "Pre-op check fine, starved from 8 pm.", "Routine ovariohysterectomy, no complications.", "Routine spay.",
            "Meloxicam 3 days. Buster collar. Post-op check in 3 days.", 31.5m, [(spay, 1), (meloxicam, 6)], PaymentMethod.Transfer, "Surgery", 9);
        History(sherlock, douglas, Open(18), "Ear infection", "Smell from both ears, head shaking.", "Both ears thickened, purulent discharge.",
            "Chronic bilateral otitis externa.", "Ear clean. Cefalexin 10 days. Discuss long-term ear care at recheck.", 27.4m, [(consult, 1), (cefalexin, 20)], null, hour: 12, minute: 15);
        History(rani, jen, Open(3), "First kitten vaccination", "New kitten, settling in well.", "Healthy kitten. No fleas or ear mites.", "Healthy.",
            "Feline flu and enteritis first dose, second in 3–4 weeks.", 1.6m, [(consult, 1), (catFlu, 1)], PaymentMethod.Card, "Vaccination", 11);
        History(otis, heather, Open(130), "Dental", "Bad breath.", "Grade 2 tartar, one fractured carnassial extracted under GA.", "Periodontal disease.",
            "Amoxicillin/clavulanate 5 days. Soft food 3 days.", 31.0m, [(dental, 1), (amoxiclav, 10)], PaymentMethod.Card, "Dental", 9, 30);
        History(fudge, douglas, Open(68), "Hair loss", "Thinning fur on tail and flanks for a month.", "Symmetrical alopecia, otherwise well.",
            "Suspect adrenal disease.", "Discuss implant with owner. Recheck in 4 weeks.", 1.3m, [(consult, 1)], PaymentMethod.Cash, hour: 10);
        History(bolo, heather, Open(35), "Squinting right eye", "Rubbing right eye since yesterday.", "Fluorescein positive, 2 mm superficial corneal ulcer.",
            "Superficial corneal ulcer.", "Eye drops, buster collar. Recheck in 5 days.", 8.6m, [(consult, 1)], PaymentMethod.Card, hour: 12, minute: 30);
        History(ginger, jen, Open(85), "Annual booster", "Well, hunts a lot.", "Healthy exam. Fleas seen.", "Healthy. Flea infestation.",
            "Feline flu and enteritis vaccine given. Flea treatment for both pets.", 5.2m, [(consult, 1), (catFlu, 1), (catFlea, 1)], PaymentMethod.Card, "Vaccination", 9);
        History(noor, jen, Open(260), "Matted coat", "Owner unable to groom her.", "Extensive matting, skin underneath healthy.", "Matted coat.",
            "Clipped under sedation. Grooming advice given.", 4.7m, [(consult, 1)], PaymentMethod.Card, hour: 11, minute: 45);

        // Counter sales: invoices raised at the desk with no consultation, some with free-text lines.
        Invoice CounterSale(Client client, Patient? pet, int daysAgo, int hour, IEnumerable<(Product? Product, string Description, decimal Qty, decimal Price, VatRate Vat)> lines, PaymentMethod? paidBy)
        {
            var issued = At(-daysAgo, hour);
            var invoice = new Invoice { Number = Invoice.FormatNumber(invoiceNumber++), Client = client, Patient = pet, IssuedUtc = issued, DueDate = today.AddDays(-daysAgo + settings.PaymentTermsDays) };
            invoice.Lines.AddRange(lines.Select(l => new InvoiceLine { Invoice = invoice, Product = l.Product, Description = l.Description, Quantity = l.Qty, UnitPrice = l.Price, VatRate = l.Vat }));
            invoice.SetTotals(VatCalculator.For(invoice.Lines));
            if (paidBy is { } method)
                invoice.Payments.Add(new Payment { Invoice = invoice, Amount = invoice.Total, Method = method, TakenBy = liam.ShortName, TakenUtc = issued.AddMinutes(2), Reference = method == PaymentMethod.Card ? "Terminal 1" : null });
            _db.Invoices.Add(invoice);
            return invoice;
        }

        CounterSale(ahmed, simba, Open(6), 15, [(catFlea, catFlea.Name, 3, catFlea.UnitPrice, catFlea.VatRate), (null, "Engraved ID tag", 1, 6.50m, VatRate.Standard)], PaymentMethod.Card);
        CounterSale(thompson, smudge, Open(50), 11, [(null, "Clay paw print keepsake", 1, 25m, VatRate.Standard)], PaymentMethod.Cash);
        CounterSale(barker, duke, Open(10), 16, [(null, "Insurance claim form", 1, 25m, VatRate.Standard)], null);
        CounterSale(lewis, biggles, Open(40), 10, [(null, "Vitamin C drops for guinea pigs, 50 ml", 1, 5.99m, VatRate.Standard)], PaymentMethod.Cash);
        var refunded = CounterSale(patel, bruno, Open(9), 17, [(flea, flea.Name, 2, flea.UnitPrice, flea.VatRate)], PaymentMethod.Card);
        refunded.Payments.Add(new Payment { Invoice = refunded, Amount = -refunded.Total, Method = PaymentMethod.Card, TakenBy = liam.ShortName, TakenUtc = At(-Open(9), 17, 30), Reference = "Returned unopened, wrong weight band" });

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

        // The next two weeks for the wider client list, spread thinly across times that don't clash with the bookings
        // above, so the schedule has gaps to book into.
        var vetVisits = new (Patient Pet, string Type, string Reason)[]
        {
            (hazel, "Vaccination", "Second puppy vaccination"), (kot, "Recheck", "Thyroid recheck"), (bruno, "Recheck", "Ear recheck"),
            (olive, "Recheck", "Back pain recheck"), (sherlock, "Recheck", "Ear recheck"), (bolo, "Consultation", "Snoring and breathing noisily"),
            (dai, "Consultation", "Seizure last night"), (dottie, "Vaccination", "Annual booster"), (willow, "Consultation", "Limping after running"),
            (tilly, "Consultation", "Senior health check"), (noor, "Consultation", "Weeping eyes"), (zuri, "Consultation", "Itchy paws"),
            (thistle, "Vaccination", "Annual vaccination"), (bramley, "Vaccination", "Annual vaccination"), (mochi, "Consultation", "Scooting"),
            (simba, "Recheck", "Cystitis recheck"), (fudge, "Recheck", "Adrenal recheck"), (otis, "Consultation", "Corn on paw pad"),
            (ginger, "Consultation", "Abscess on tail"), (kofi, "Consultation", "Pre-op check"), (bear, "Recheck", "Lameness recheck"),
            (rani, "Vaccination", "Second kitten vaccination"),
        };
        var nurseVisits = new (Patient Pet, string Reason)[]
        {
            (biggles, "Nail clip and weight check"), (pip, "Nail clip"), (olive, "Weight check"), (hazel, "Puppy weigh-in and worming"),
            (kot, "Blood pressure check"), (bolo, "Weight clinic"), (otis, "Nail clip"), (rani, "Flea and worming advice"),
        };
        (int Hour, int Minute, StaffMember? Vet)[] weekdaySlots = [(9, 30, douglas), (11, 0, heather), (11, 15, jen), (12, 0, douglas), (14, 15, jen), (15, 45, heather), (16, 30, null), (17, 15, douglas)];
        (int Hour, int Minute, StaffMember? Vet)[] saturdaySlots = [(9, 30, douglas), (11, 0, heather), (11, 15, jen), (12, 0, null)];
        int nextVet = 0, nextNurse = 0;
        for (var day = 1; day <= 14; day++)
        {
            var dayOfWeek = today.AddDays(day).DayOfWeek;
            if (settings.HoursOn(dayOfWeek) is null)
                continue;
            var slots = dayOfWeek == DayOfWeek.Saturday ? saturdaySlots : weekdaySlots;
            for (var i = 0; i < slots.Length; i++)
            {
                if ((day + i) % 3 != 0)
                    continue;
                var (hour, minute, vet) = slots[i];
                if (vet is null && nextNurse < nurseVisits.Length)
                {
                    var (pet, reason) = nurseVisits[nextNurse++];
                    Book(pet, "Nurse clinic", day, hour, minute, reason, null).SmsReminder = nextNurse % 2 == 0;
                }
                else if (vet is not null && nextVet < vetVisits.Length)
                {
                    var (pet, type, reason) = vetVisits[nextVet++];
                    Book(pet, type, day, hour, minute, reason, vet).SmsReminder = nextVet % 2 == 0;
                }
            }
        }

        // Morning operations the week after next, before Jen's consultations start.
        var surgeryDays = Enumerable.Range(7, 8).Where(d => settings.HoursOn(today.AddDays(d).DayOfWeek) is not null && today.AddDays(d).DayOfWeek != DayOfWeek.Saturday).ToList();
        Book(kofi, "Surgery", surgeryDays[0], 8, 30, "Castration", jen);
        Book(sherlock, "Dental", surgeryDays[1], 8, 30, "Dental scale and polish", jen);
        Book(mochi, "Dental", surgeryDays[2], 8, 30, "Dental scale and polish", jen);

        // Recent appointments that didn't happen.
        Book(bolo, "Nurse clinic", -Open(3), 16, 30, "Weight clinic", null).ChangeStatus(AppointmentStatus.NoShow, At(-Open(3), 16, 45));
        Book(willow, "Vaccination", -Open(6), 11, 0, "Annual booster", heather).ChangeStatus(AppointmentStatus.Cancelled, At(-Open(6), 8, 15));
        Book(otis, "Consultation", -Open(8), 15, 45, "Lump check", heather).ChangeStatus(AppointmentStatus.NoShow, At(-Open(8), 16));

        // More boosters due or overdue for the reminders list, for pets without a booster in the history above.
        void Due(Patient pet, Product vaccine, int dueInDays, StaffMember vet) =>
            _db.Vaccinations.Add(new Vaccination
            {
                Patient = pet, VaccineName = vaccine.Name, GivenUtc = At(dueInDays - 365, 10), GivenBy = vet.ShortName, DueDate = today.AddDays(dueInDays),
            });
        Due(simba, catFlu, -20, jen);
        Due(willow, lepto, 9, heather);
        Due(mochi, lepto, 22, jen);
        Due(zuri, lepto, 40, heather);
        Due(otis, lepto, -35, douglas);
        Due(noor, catFlu, 3, jen);
        Due(olive, kennelCough, 12, heather);

        _db.ClinicalNotes.AddRange(
            new ClinicalNote
            {
                Patient = kot, WrittenUtc = At(-Open(62), 16, 30), Author = jen.ShortName, Title = "Phone call: blood results",
                Body = "Total T4 raised. Kidney values normal. Discussed treatment options with Ms Nowak; she would like to start tablets and recheck in a month.",
            },
            new ClinicalNote
            {
                Patient = smudge, WrittenUtc = At(-Open(58), 8, 40), Author = douglas.ShortName, Title = "Phone call: quality of life",
                Body = "Mrs Thompson says Smudge has stopped eating. Talked through what to expect. Booked in for 12:00 today, longer appointment, side room.",
            },
            new ClinicalNote
            {
                Patient = olive, WrittenUtc = At(-Open(22), 10), Author = liam.ShortName, Title = "Insurance claim sent",
                Body = "Petplan claim form and clinical history emailed. Claim reference to follow.",
            });

        // Pharmacy queue: one from the open consultation path, one controlled drug, one repeat needing a vet.
        _db.DispenseItems.Add(new DispenseItem { Patient = pepper, Product = meloxicam, Quantity = 30, Directions = "1 ml once daily with food for 21 days", PrescribedBy = douglas.ShortName });
        _db.DispenseItems.Add(new DispenseItem { Patient = atlas, Product = methadone, Quantity = 0.8m, Directions = "Pre-medication, IV", PrescribedBy = jen.ShortName });
        _db.DispenseItems.Add(new DispenseItem { Patient = luna, Product = food, Quantity = 1, Directions = "Repeat request by phone", PrescribedBy = liam.ShortName, RequiresAuthorisation = true });
        _db.DispenseItems.Add(new DispenseItem { Patient = poppy, Product = pimobendan, Quantity = 60, Directions = "1 tablet twice daily, an hour before food", PrescribedBy = heather.ShortName });
        _db.DispenseItems.Add(new DispenseItem { Patient = luna, Product = insulin, Quantity = 1, Directions = "Repeat request by email", PrescribedBy = liam.ShortName, RequiresAuthorisation = true });

        // A delivery on its way.
        var order = new PurchaseOrder { Number = PurchaseOrder.FormatNumber(1), Supplier = "Covetrus", DeliverBy = today.AddDays(1), Status = PurchaseOrderStatus.Sent, SentUtc = now.AddDays(-1) };
        order.Lines.Add(new PurchaseOrderLine { Order = order, Product = amoxiclav, Quantity = 100 });
        order.Lines.Add(new PurchaseOrderLine { Order = order, Product = lepto, Quantity = 20 });
        _db.PurchaseOrders.Add(order);

        // And a reorder drafted but not yet sent.
        var draft = new PurchaseOrder { Number = PurchaseOrder.FormatNumber(2), Supplier = "NVS", Note = "Insulin to come on the chilled van" };
        draft.Lines.Add(new PurchaseOrderLine { Order = draft, Product = insulin, Quantity = 4 });
        draft.Lines.Add(new PurchaseOrderLine { Order = draft, Product = catFlea, Quantity = 24 });
        draft.Lines.Add(new PurchaseOrderLine { Order = draft, Product = flea, Quantity = 24 });
        _db.PurchaseOrders.Add(draft);

        _db.NumberSequences.Add(new NumberSequence { Name = NumberSequence.Invoice, Next = invoiceNumber });
        _db.NumberSequences.Add(new NumberSequence { Name = NumberSequence.PurchaseOrder, Next = 3 });

        _db.FollowUps.AddRange(
            new FollowUp { Text = "Call Mr Hughes with Rufus's referral appointment", Patient = rufus, DueDate = today },
            new FollowUp { Text = "Chase Petplan on Atlas's claim", Patient = atlas, DueDate = today },
            new FollowUp { Text = "Send Max's owner a reminder about the unpaid invoice", Patient = max, DueDate = today.AddDays(-2) },
            new FollowUp { Text = "Chase Mr Kowalski for Reksio's dental invoice", Patient = reksio, DueDate = today },
            new FollowUp { Text = "Check Petplan has Olive's claim", Patient = olive, DueDate = today.AddDays(3) },
            new FollowUp
            {
                Text = "Call Ms Nowak with Kot's blood results", Patient = kot, DueDate = today.AddDays(-Open(62)), IsDone = true,
                DoneUtc = At(-Open(62), 16, 30), DoneBy = jen.ShortName,
            });

        _db.Notifications.AddRange(
            new Notification { CreatedUtc = At(0, 9, 10), Text = "Miso checked in for vomiting since yesterday", Kind = NotificationKind.Success, Link = $"/patients" },
            new Notification { CreatedUtc = At(0, 8, 0), Text = "Amoxicillin/clavulanate 250 mg tablets is below its reorder level", Kind = NotificationKind.Warning, Link = "/pharmacy" },
            new Notification { CreatedUtc = At(0, 8, 0), Text = "Porcine insulin 40 IU/ml, 10 ml vial is below its reorder level", Kind = NotificationKind.Warning, Link = "/pharmacy" },
            new Notification { CreatedUtc = At(-1, 17, 30), Text = "Delivery PO-0001 from Covetrus is due tomorrow", Kind = NotificationKind.Info, Link = "/pharmacy" });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
