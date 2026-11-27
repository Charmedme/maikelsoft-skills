namespace Kiosk.Api.Admin;

/// <summary>A recurring export of the order data for the finance team.</summary>
public sealed class ExportSchedule
{
    private string _archivePath = string.Empty;

    public string Name { get; set; } = "orders";

    public string Cron { get; set; } = "0 3 * * *";

    /// <summary>The folder that receives the export files. Setting it makes sure that the folder exists.</summary>
    public string ArchivePath
    {
        get => _archivePath;
        set
        {
            _archivePath = value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                Directory.CreateDirectory(value);
            }
        }
    }
}
