// Usage: dotnet run -- <cases.json> <out.json>
// Renders each case with the real Razor component from VetDesk.UI/Shared (bUnit, so a case can type, pick and click
// first) and writes {id: html}, so compare.mjs can check the React mirrors in .design-sync/react against it.
// Case props use the React mirror's names; Parameters() maps them onto the Razor component's parameters.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bunit;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleResult;
using VetDesk.Application.Patients;
using VetDesk.Application.Patients.Queries;
using VetDesk.Application.Products;
using VetDesk.Application.Products.Queries;
using VetDesk.Application.Staff;
using VetDesk.Application.Staff.Queries;
using VetDesk.Application.Pharmacy;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;
using VetDesk.UI.Services;
using VetDesk.UI.Shared;

// The app runs in en-GB; Blazor formats bound numbers and dates with the current culture.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

var cases = JsonNode.Parse(File.ReadAllText(args[0]))!.AsArray();
var output = new Dictionary<string, string>();
foreach (var c in cases)
{
    var id = (string)c!["id"]!;
    try
    {
        output[id] = await RenderCase(c.AsObject());
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"case {id} failed: {ex}");
        output[id] = $"<pre>RefRender failed: {System.Net.WebUtility.HtmlEncode(ex.Message)}</pre>";
    }
}
File.WriteAllText(args[1], JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"rendered {output.Count} cases");

async Task<string> RenderCase(JsonObject c)
{
    var component = (string)c["component"]!;
    var p = c["props"]?.AsObject() ?? new JsonObject();
    await using var ctx = new BunitContext();
    ctx.JSInterop.Mode = JSRuntimeMode.Loose;

    var session = new UserSession();
    // ChargeForm asks whether the signed-in person may prescribe.
    if (p["canOverride"] is not JsonValue v || v.GetValue<bool>())
        session.SignIn(new SignedInStaff(99, "Test Vet", "Test", "TV", "Veterinary surgeon", StaffRole.Vet, false, new HashSet<Permission> { Permission.Prescribe }));
    ctx.Services.AddSingleton(session);
    ctx.Services.AddSingleton<DeviceAddress>();
    ctx.Services.AddSingleton<CurrentUser>();
    ctx.Services.AddSingleton<ISender>(new FakeSender(p));
    ctx.Services.AddSingleton<ILogger<Backend>>(NullLogger<Backend>.Instance);
    ctx.Services.AddSingleton<Backend>();
    ctx.Services.AddSingleton<DialogService>();
    ctx.Services.AddSingleton<ToastService>();
    ctx.Services.AddSingleton<Crumbs>();

    var (type, parameters) = Parameters(component, p, ctx.Services);
    var cut = ctx.Render(b =>
    {
        b.OpenComponent(0, type);
        foreach (var (name, value) in parameters)
            b.AddAttribute(1, name, value);
        b.CloseComponent();
        // ConfirmHost renders whatever DialogService is asking (AlertEditor's delete asks it).
        if (component != "ConfirmDialog")
        {
            b.OpenComponent<ConfirmHost>(2);
            b.CloseComponent();
        }
    });

    foreach (var a in (c["blazorActions"] ?? c["actions"])?.AsArray() ?? new JsonArray())
    {
        var act = a!.AsObject();
        if (act["wait"] is { } wait) { await Task.Delay((int)wait); continue; }
        var sel = (string)act["selector"]!;
        await cut.InvokeAsync(() => { });
        if (act["fill"] is { } fill) cut.Find(sel).Input((string)fill!);
        else if (act["select"] is { } select) cut.Find(sel).Change((string)select!);
        else if (act["click"] is not null) cut.Find(sel).Click();
        else if (act["key"] is { } key) cut.Find(sel).KeyDown((string)key!);
        // Debounced searches (180 ms) finish on their own after an input.
        await Task.Delay(act["fill"] is not null ? 400 : 50);
    }
    await Task.Delay(50);
    return cut.Markup;
}

(Type, List<(string, object?)>) Parameters(string component, JsonObject p, IServiceProvider services)
{
    var list = new List<(string, object?)>();
    void Add(string name, object? value) => list.Add((name, value));
    string? S(string key) => (string?)p[key];
    bool B(string key) => p[key] is JsonValue v && v.GetValue<bool>();

    switch (component)
    {
        case "Icon":
            Add("Name", S("name"));
            if (p["size"] is { } size) Add("Size", (int)size);
            return (typeof(Icon), list);
        case "AlertChip":
            Add("Text", S("text"));
            Add("Severity", Enum.Parse<AlertSeverity>(S("severity") ?? "Alert"));
            Add("IsAllergy", B("isAllergy"));
            Add("Large", B("large"));
            return (typeof(AlertChip), list);
        case "AppointmentStatusChip":
            Add("Status", Enum.Parse<AppointmentStatus>(S("status")!));
            Add("Kind", Enum.Parse<AppointmentKind>(S("kind") ?? "Consultation"));
            return (typeof(AppointmentStatusChip), list);
        case "InvoiceStatusChip":
            Add("Status", Enum.Parse<InvoiceStatus>(S("status")!));
            return (typeof(InvoiceStatusChip), list);
        case "StockStatusChip":
            Add("Status", Enum.Parse<StockStatus>(S("status") ?? "InStock"));
            return (typeof(StockStatusChip), list);
        case "VaccineDueChip":
            Add("Vaccination", new VaccinationDto(1, "DHP", DateTime.UtcNow, "Test", null,
                S("dueDate") is { } due ? DateOnly.Parse(due, CultureInfo.InvariantCulture) : null, B("isOverdue"), B("isDueSoon")));
            return (typeof(VaccineDueChip), list);
        case "Kpi":
            Add("Label", S("label"));
            Add("Value", S("value"));
            Add("Change", p["change"] is { } ch ? (decimal?)(decimal)ch : null);
            return (typeof(Kpi), list);
        case "Check":
            Add("Ok", B("ok"));
            Add("Text", S("text"));
            return (typeof(Check), list);
        case "FormError":
            Add("Text", S("text"));
            return (typeof(FormError), list);
        case "SoapLetter":
            Add("L", S("letter"));
            return (typeof(SoapLetter), list);
        case "Vital":
            Add("Label", S("label"));
            Add("Unit", S("unit") ?? "");
            Add("Id", S("id"));
            Add("Value", S("value"));
            Add("Disabled", B("disabled"));
            Add("Text", B("text"));
            return (typeof(Vital), list);
        case "WeightChart":
            Add("Weights", p["weights"]!.AsArray().Select(w => new WeightDto(
                DateTime.Parse((string)w!["recordedUtc"]!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                (decimal)w["kg"]!, (string)w["context"]!)).ToList());
            return (typeof(WeightChart), list);
        case "Modal":
            Add("Title", S("title"));
            if (S("subtitle") is { } sub) Add("Subtitle", sub);
            if (p["children"]?["$html"] is { } body) Add("ChildContent", Data.Markup((string)body!));
            if (p["footer"]?["$html"] is { } foot) Add("Footer", Data.Markup((string)foot!));
            if (S("size") is { } sz) Add("Size", sz);
            Add("Flush", B("flush"));
            Add("OnTop", B("onTop"));
            return (typeof(Modal), list);
        case "ConfirmDialog":
            var dialogs = services.GetRequiredService<DialogService>();
            if (p.ContainsKey("cancelLabel") && p["cancelLabel"] is null)
                _ = dialogs.AlertAsync(S("title")!, S("message"));
            else
                _ = dialogs.ConfirmAsync(S("title")!, S("message"), S("confirmLabel") ?? "OK", S("cancelLabel") ?? "Cancel", B("danger"));
            return (typeof(ConfirmHost), list);
        case "Toasts":
            var toasts = services.GetRequiredService<ToastService>();
            foreach (var t in p["toasts"]!.AsArray())
            {
                var text = (string)t!["text"]!;
                switch ((string?)t["kind"] ?? "Success")
                {
                    case "Error": toasts.Error(text); break;
                    case "Warning": toasts.Warning(text); break;
                    default: toasts.Success(text); break;
                }
            }
            return (typeof(ToastHost), list);
        case "PinPad":
            if (S("id") is { } pinId) Add("Id", pinId);
            if (S("label") is { } label) Add("Label", label);
            Add("Busy", B("busy"));
            return (typeof(PinPad), list);
        case "ChargeForm":
            Add("Pending", new ChargeForm.PendingCharge(Data.Product(p["product"]!.AsObject()), B("isPrescription"),
                p["conflicts"]?.AsArray().Select(x => (string)x!).ToList() ?? []));
            return (typeof(ChargeForm), list);
        case "AlertEditor":
            var al = p["alert"]!.AsObject();
            Add("Alert", new AlertEditor.Draft((int?)al["id"], (string?)al["presetKey"], (string)al["text"]!, Enum.Parse<AlertSeverity>((string)al["severity"]!)));
            return (typeof(AlertEditor), list);
        case "WitnessPicker":
            if (S("id") is { } wid) Add("Id", wid);
            Add("WitnessId", (int?)p["witnessId"]);
            Add("Pin", S("pin"));
            return (typeof(WitnessPicker), list);
        case "PatientSearch":
            return (typeof(PatientSearch), list);
        case "ProductSearch":
            if (p["categories"] is JsonArray cats) Add("Categories", cats.Select(x => Enum.Parse<ProductCategory>((string)x!)).ToList());
            Add("StockedOnly", B("stockedOnly"));
            if (S("placeholder") is { } ph) Add("Placeholder", ph);
            if (S("inputId") is { } pid) Add("InputId", pid);
            return (typeof(ProductSearch), list);
        case "PatientPicker":
            if (p["selected"] is JsonObject sel) Add("Selected", Data.Patient(sel));
            Add("Locked", B("locked"));
            if (S("inputId") is { } iid) Add("InputId", iid);
            return (typeof(PatientPicker), list);
        default:
            throw new InvalidOperationException($"Unknown component {component}");
    }
}

/// <summary>Answers the queries the search components send with the case's own data, in the order given.</summary>
sealed class FakeSender(JsonObject props) : ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        object result = request switch
        {
            SearchPatientsQuery q => Result.Success<IReadOnlyList<PatientRow>>(
                (q.Text is null ? props["recent"] : props["patients"])?.AsArray().Select(x => Data.Patient(x!.AsObject())).Take(q.Take).ToList() ?? []),
            SearchProductsQuery q => Result.Success<IReadOnlyList<ProductDto>>(
                props["products"]?.AsArray().Select(x => Data.Product(x!.AsObject()))
                    .Where(x => q.Categories is null || q.Categories.Contains(x.Category)).Take(q.Take).ToList() ?? []),
            GetStaffQuery => Result.Success<IReadOnlyList<StaffDto>>(
                props["witnesses"]?.AsArray().Select(x => new StaffDto((int)x!["id"]!, "", "", "", (string)x["shortName"]!, "", null, StaffRole.Nurse,
                    false, "", (string)x["jobTitle"]!, null, true, true, false, null)).ToList() ?? []),
            _ => throw new NotSupportedException(request.GetType().Name)
        };
        return Task.FromResult((TResponse)result);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotSupportedException();
    public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

/// <summary>Builds the app's DTOs from a case's React-style props.</summary>
static class Data
{
    public static RenderFragment Markup(string html) => b => b.AddMarkupContent(0, html);

    public static PatientRow Patient(JsonObject j) => new(
        (int)j["id"]!, (string)j["name"]!, (string?)j["patientNumber"] ?? "", (string)j["species"]!, (string)j["speciesAndBreed"]!, "",
        0, (string)j["ownerName"]!, (string?)j["ownerPhone"] ?? "", (string?)j["microchip"], null, 0, false,
        j["allergyLabels"]?.AsArray().Select(x => (string)x!).ToList() ?? []);

    public static ProductDto Product(JsonObject j) => new(
        (int?)j["id"] ?? 1, (string?)j["code"] ?? "", (string)j["name"]!, Enum.Parse<ProductCategory>((string?)j["category"] ?? "Medicine"),
        (decimal)j["grossPrice"]!, (decimal)j["grossPrice"]!, 0, VatRate.Standard, null, (string)j["unit"]!, null,
        (bool?)j["isControlledDrug"] ?? false, null, null, (bool?)j["isStocked"] ?? false, 0, null, null, true,
        (decimal?)j["onHand"] ?? 0, j["allergyConflicts"]?.AsArray().Select(x => (string)x!).ToList() ?? []);
}
