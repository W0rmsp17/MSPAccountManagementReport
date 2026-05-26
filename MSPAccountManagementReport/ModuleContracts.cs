using System.Text.Json;

namespace MSPAccountManagementReport;

public sealed record ModuleJobInput
{
    public required string JobId { get; init; }

    public required string ModuleId { get; init; }

    public required string ModuleVersion { get; init; }

    public required string ClientConnectionId { get; init; }

    public TenantContext? TenantContext { get; init; }

    public TargetScope? TargetScope { get; init; }

    public OperatorContext? Operator { get; init; }

    public JsonElement Parameters { get; init; }
}

public sealed record TenantContext
{
    public string? ClientId { get; init; }

    public string? TenantId { get; init; }

    public string? TenantName { get; init; }
}

public sealed record TargetScope
{
    public required string Type { get; init; }

    public string Mode { get; init; } = "Selected";

    public IReadOnlyList<TargetScopeTarget> Targets { get; init; } = [];
}

public sealed record TargetScopeTarget
{
    public required string Id { get; init; }

    public string? DisplayName { get; init; }

    public string? UserPrincipalName { get; init; }
}

public sealed record OperatorContext
{
    public string? Id { get; init; }

    public string? Upn { get; init; }
}

public sealed record ModuleJobOutput
{
    public string SchemaVersion { get; init; } = "1.0";

    public required string JobId { get; init; }

    public required string Status { get; init; }

    public required string Summary { get; init; }

    public IReadOnlyList<ReportFinding> Findings { get; init; } = [];

    public IReadOnlyDictionary<string, object?> Metrics { get; init; } = new Dictionary<string, object?>();

    public AccountManagementReportData? Report { get; init; }

    public IReadOnlyList<ReportArtifact> Artifacts { get; init; } = [];
}

public sealed record ReportFinding(
    string Severity,
    string Code,
    string Title,
    string Detail);

public sealed record ReportArtifact(
    string Type,
    string Name,
    string? Uri);

public sealed record AccountManagementReportData
{
    public LicenseReportSection LicenseSummary { get; init; } = new();
}

public sealed record LicenseReportSection
{
    public IReadOnlyList<LicenseSummaryItem> Items { get; init; } = [];
}

public sealed record LicenseSummaryItem
{
    public required string SkuPartNumber { get; init; }

    public string? SkuId { get; init; }

    public required string DisplayName { get; init; }

    public bool FriendlyNameKnown { get; init; }

    public int TotalLicenses { get; init; }

    public int AssignedLicenses { get; init; }

    public int AvailableLicenses { get; init; }

    public int SuspendedLicenses { get; init; }

    public int WarningLicenses { get; init; }
}
