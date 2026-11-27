namespace Kiosk.Api.Files;

public sealed class FileStorageOptions
{
    public const string Section = "Files";

    public string InvoiceRoot { get; set; } = "App_Data/invoices";

    public string UploadRoot { get; set; } = "wwwroot/uploads";

    public string ProfileImageRoot { get; set; } = "wwwroot/profile-images";

    public string PublicFilesRoot { get; set; } = "wwwroot/files";

    public string AccessLogRoot { get; set; } = "logs/access";

    public string Resolve(string contentRoot, string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(contentRoot, path);
}
