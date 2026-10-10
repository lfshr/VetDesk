namespace VetDesk.Domain.Common;

/// <summary>
/// Rows that two desks may edit at the same time. The database context gives <see cref="Version"/> a new
/// value on every save and uses it as a concurrency token, so the second save fails instead of silently
/// overwriting the first. A GUID is used rather than a SQL Server rowversion so the same model works on SQLite.
/// </summary>
public interface IHasVersion
{
    Guid Version { get; set; }
}
