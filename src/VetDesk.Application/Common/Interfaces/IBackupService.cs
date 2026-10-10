namespace VetDesk.Application.Common.Interfaces;

public interface IBackupService
{
    /// <summary>Writes a full copy of the database into <paramref name="folder"/> and returns the file written.</summary>
    Task<string> BackUpAsync(string folder, CancellationToken cancellationToken);
}
