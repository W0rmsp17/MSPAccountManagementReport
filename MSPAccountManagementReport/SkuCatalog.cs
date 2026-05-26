using System.Text.Json;
using System.Text.Json.Serialization;

namespace MSPAccountManagementReport;

public sealed record SkuCatalog(
    string SchemaVersion,
    string Source,
    string SourceUrl,
    DateTimeOffset GeneratedUtc,
    int SkuCount,
    Dictionary<string, SkuCatalogEntry> Skus);

public sealed record SkuCatalogEntry(
    string DisplayName,
    string SkuPartNumber,
    string SkuId);

public sealed class SkuCatalogLookup
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SkuCatalog _catalog;

    private SkuCatalogLookup(SkuCatalog catalog)
    {
        _catalog = catalog;
    }

    public int Count => _catalog.Skus.Count;

    public static SkuCatalogLookup LoadDefault()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "m365-sku-map.json");
        return Load(path);
    }

    public static SkuCatalogLookup Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("SKU catalog file was not found.", path);
        }

        var json = File.ReadAllText(path);
        var catalog = JsonSerializer.Deserialize<SkuCatalog>(json, JsonOptions);
        if (catalog is null)
        {
            throw new InvalidOperationException("SKU catalog could not be parsed.");
        }

        return new SkuCatalogLookup(catalog);
    }

    public SkuCatalogEntry? FindBySkuPartNumber(string? skuPartNumber)
    {
        if (string.IsNullOrWhiteSpace(skuPartNumber))
        {
            return null;
        }

        return _catalog.Skus.TryGetValue(skuPartNumber, out var entry) ? entry : null;
    }

    public string GetDisplayNameOrFallback(string? skuPartNumber)
    {
        return FindBySkuPartNumber(skuPartNumber)?.DisplayName
            ?? skuPartNumber
            ?? "Unknown SKU";
    }
}
