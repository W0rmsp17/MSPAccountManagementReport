using System.Text.Json.Serialization;

namespace MSPAccountManagementReport;

public sealed record GraphCollectionResponse<T>
{
    [JsonPropertyName("value")]
    public IReadOnlyList<T> Value { get; init; } = [];
}

public sealed record GraphSubscribedSku
{
    [JsonPropertyName("skuId")]
    public string? SkuId { get; init; }

    [JsonPropertyName("skuPartNumber")]
    public string? SkuPartNumber { get; init; }

    [JsonPropertyName("consumedUnits")]
    public int ConsumedUnits { get; init; }

    [JsonPropertyName("prepaidUnits")]
    public GraphPrepaidUnits? PrepaidUnits { get; init; }
}

public sealed record GraphPrepaidUnits
{
    [JsonPropertyName("enabled")]
    public int Enabled { get; init; }

    [JsonPropertyName("suspended")]
    public int Suspended { get; init; }

    [JsonPropertyName("warning")]
    public int Warning { get; init; }
}

public sealed record GraphUser
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("userPrincipalName")]
    public string? UserPrincipalName { get; init; }

    [JsonPropertyName("accountEnabled")]
    public bool? AccountEnabled { get; init; }

    [JsonPropertyName("assignedLicenses")]
    public IReadOnlyList<GraphAssignedLicense> AssignedLicenses { get; init; } = [];
}

public sealed record GraphAssignedLicense
{
    [JsonPropertyName("skuId")]
    public string? SkuId { get; init; }
}
