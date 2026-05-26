using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MSPAccountManagementReport;

public sealed class SubscribedSkuCollector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly string? _graphAccessToken;
    private readonly string? _samplePath;

    public SubscribedSkuCollector(HttpClient httpClient, string? graphAccessToken, string? samplePath = null)
    {
        _httpClient = httpClient;
        _graphAccessToken = graphAccessToken;
        _samplePath = samplePath;
    }

    public async Task<SubscribedSkuCollectionResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_graphAccessToken))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/subscribedSkus");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _graphAccessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return new SubscribedSkuCollectionResult(await ParseAsync(json), "Graph");
        }

        var samplePath = ResolveSamplePath();
        if (samplePath is not null)
        {
            var json = await File.ReadAllTextAsync(samplePath, cancellationToken);
            return new SubscribedSkuCollectionResult(await ParseAsync(json), "Sample");
        }

        return new SubscribedSkuCollectionResult([], "Unavailable");
    }

    private string? ResolveSamplePath()
    {
        if (!string.IsNullOrWhiteSpace(_samplePath) && File.Exists(_samplePath))
        {
            return _samplePath;
        }

        var configuredPath = Environment.GetEnvironmentVariable("GRAPH_SUBSCRIBED_SKUS_SAMPLE_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var localSamplePath = Path.Combine(AppContext.BaseDirectory, "samples", "subscribed-skus.sample.json");
        return File.Exists(localSamplePath) ? localSamplePath : null;
    }

    private static Task<IReadOnlyList<GraphSubscribedSku>> ParseAsync(string json)
    {
        var response = JsonSerializer.Deserialize<GraphCollectionResponse<GraphSubscribedSku>>(json, JsonOptions);
        return Task.FromResult(response?.Value ?? []);
    }
}

public sealed record SubscribedSkuCollectionResult(
    IReadOnlyList<GraphSubscribedSku> Skus,
    string Source);

public sealed class LicenseSummaryBuilder
{
    private readonly SkuCatalogLookup _skuCatalog;

    public LicenseSummaryBuilder(SkuCatalogLookup skuCatalog)
    {
        _skuCatalog = skuCatalog;
    }

    public IReadOnlyList<LicenseSummaryItem> Build(IReadOnlyList<GraphSubscribedSku> skus)
    {
        return skus
            .Where(sku => !string.IsNullOrWhiteSpace(sku.SkuPartNumber))
            .Select(ToSummaryItem)
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private LicenseSummaryItem ToSummaryItem(GraphSubscribedSku sku)
    {
        var skuPartNumber = sku.SkuPartNumber!;
        var catalogEntry = _skuCatalog.FindBySkuPartNumber(skuPartNumber);
        var total = sku.PrepaidUnits?.Enabled ?? 0;
        var assigned = sku.ConsumedUnits;

        return new LicenseSummaryItem
        {
            SkuPartNumber = skuPartNumber,
            SkuId = sku.SkuId,
            DisplayName = catalogEntry?.DisplayName ?? skuPartNumber,
            FriendlyNameKnown = catalogEntry is not null,
            TotalLicenses = total,
            AssignedLicenses = assigned,
            AvailableLicenses = Math.Max(total - assigned, 0),
            SuspendedLicenses = sku.PrepaidUnits?.Suspended ?? 0,
            WarningLicenses = sku.PrepaidUnits?.Warning ?? 0
        };
    }
}
