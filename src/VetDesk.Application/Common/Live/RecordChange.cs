namespace VetDesk.Application.Common.Live;

/// <summary>
/// One save that changed records. <see cref="Topics"/> names what changed so a screen can tell whether it shows any of
/// it: the record type ("Appointment"), the record ("Consultation:40") and the records it belongs to ("Patient:12" for
/// a new clinical note, alert or appointment of patient 12).
/// </summary>
/// <param name="ByName">Who saved it, as records store them ("Heather"), or "System".</param>
/// <param name="DeskId">The desk it was saved from, or null for background work such as the nightly backup.</param>
public sealed record RecordChange(IReadOnlySet<string> Topics, string ByName, Guid? DeskId)
{
    public static string Topic(string type, int id) => $"{type}:{id}";

    public bool Touches(IEnumerable<string> topics) => topics.Any(Topics.Contains);
}
