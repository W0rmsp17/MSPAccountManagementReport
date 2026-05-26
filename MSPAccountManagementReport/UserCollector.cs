using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MSPAccountManagementReport;

public sealed class UserCollector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string GraphUrl = "https://graph.microsoft.com/v1.0/users?$select=id,displayName,userPrincipalName,accountEnabled,assignedLicenses";

    private readonly HttpClient _httpClient;
    private readonly string? _graphAccessToken;
    private readonly string? _samplePath;

    public UserCollector(HttpClient httpClient, string? graphAccessToken, string? samplePath = null)
    {
        _httpClient = httpClient;
        _graphAccessToken = graphAccessToken;
        _samplePath = samplePath;
    }

    public async Task<UserCollectionResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_graphAccessToken))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, GraphUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _graphAccessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return new UserCollectionResult(Parse(json), "Graph");
        }

        var samplePath = ResolveSamplePath();
        if (samplePath is not null)
        {
            var json = await File.ReadAllTextAsync(samplePath, cancellationToken);
            return new UserCollectionResult(Parse(json), "Sample");
        }

        return new UserCollectionResult([], "Unavailable");
    }

    private string? ResolveSamplePath()
    {
        if (!string.IsNullOrWhiteSpace(_samplePath) && File.Exists(_samplePath))
        {
            return _samplePath;
        }

        var configuredPath = Environment.GetEnvironmentVariable("GRAPH_USERS_SAMPLE_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var localSamplePath = Path.Combine(AppContext.BaseDirectory, "samples", "users.sample.json");
        return File.Exists(localSamplePath) ? localSamplePath : null;
    }

    private static IReadOnlyList<GraphUser> Parse(string json)
    {
        var response = JsonSerializer.Deserialize<GraphCollectionResponse<GraphUser>>(json, JsonOptions);
        return response?.Value ?? [];
    }
}

public sealed record UserCollectionResult(
    IReadOnlyList<GraphUser> Users,
    string Source);

public sealed class UserLicenseSummaryBuilder
{
    private readonly IReadOnlyDictionary<string, LicenseSummaryItem> _licensesBySkuId;

    public UserLicenseSummaryBuilder(IReadOnlyList<LicenseSummaryItem> licenseSummary)
    {
        _licensesBySkuId = licenseSummary
            .Where(item => !string.IsNullOrWhiteSpace(item.SkuId))
            .GroupBy(item => item.SkuId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<UserLicenseItem> Build(IReadOnlyList<GraphUser> users)
    {
        return users
            .Where(user => !string.IsNullOrWhiteSpace(user.UserPrincipalName))
            .Select(ToUserLicenseItem)
            .OrderBy(item => item.DisplayName ?? item.UserPrincipalName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private UserLicenseItem ToUserLicenseItem(GraphUser user)
    {
        var licenses = user.AssignedLicenses
            .Where(license => !string.IsNullOrWhiteSpace(license.SkuId))
            .Select(ToAssignedLicenseItem)
            .OrderBy(license => license.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new UserLicenseItem
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            UserPrincipalName = user.UserPrincipalName,
            AccountEnabled = user.AccountEnabled,
            IsLicensed = licenses.Count > 0,
            Licenses = licenses
        };
    }

    private UserAssignedLicenseItem ToAssignedLicenseItem(GraphAssignedLicense assignedLicense)
    {
        if (_licensesBySkuId.TryGetValue(assignedLicense.SkuId!, out var sku))
        {
            return new UserAssignedLicenseItem
            {
                SkuId = assignedLicense.SkuId,
                SkuPartNumber = sku.SkuPartNumber,
                DisplayName = sku.DisplayName,
                FriendlyNameKnown = sku.FriendlyNameKnown
            };
        }

        return new UserAssignedLicenseItem
        {
            SkuId = assignedLicense.SkuId,
            SkuPartNumber = null,
            DisplayName = assignedLicense.SkuId!,
            FriendlyNameKnown = false
        };
    }
}
