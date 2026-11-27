using System.Globalization;
using System.Text;
using Kiosk.Api.Files;
using Microsoft.Extensions.Options;

namespace Kiosk.Api.Wishlists;

/// <summary>Writes a wish list as text, with a header from a template (for example "birthday" or "wedding").</summary>
public sealed class WishlistExporter(IOptions<PriceAlertOptions> options, IOptions<FileStorageOptions> storage, IWebHostEnvironment environment)
{
    public async Task<string?> ExportAsync(WishlistDto list, string template, CancellationToken ct)
    {
        var root = storage.Value.Resolve(environment.ContentRootPath, options.Value.TemplateRoot);
        var path = Path.Combine(root, template + ".txt");
        if (!File.Exists(path))
        {
            return null;
        }

        var text = new StringBuilder(await File.ReadAllTextAsync(path, ct));
        text.AppendLine().AppendLine(list.Name);
        foreach (var item in list.Items)
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"- {item.Quantity} x {item.ProductName} ({item.Price:0.00} EUR){(item.Note is null ? string.Empty : " - " + item.Note)}");
        }

        return text.ToString();
    }
}
