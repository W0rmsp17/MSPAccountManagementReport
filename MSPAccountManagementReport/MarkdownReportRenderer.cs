using System.Text;

namespace MSPAccountManagementReport;

public sealed class MarkdownReportRenderer
{
    public RenderedReport Render(
        string tenantName,
        IReadOnlyDictionary<string, object?> metrics,
        IReadOnlyList<LicenseSummaryItem> licenseSummary,
        IReadOnlyList<UserLicenseItem> userLicenses,
        IReadOnlyList<ReportRecommendation> recommendations)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# MSP Account Management Report - {tenantName}");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- License SKUs: {GetMetric(metrics, "licenseSkuCount")}");
        builder.AppendLine($"- Total licenses: {GetMetric(metrics, "totalLicenses")}");
        builder.AppendLine($"- Assigned licenses: {GetMetric(metrics, "assignedLicenses")}");
        builder.AppendLine($"- Available licenses: {GetMetric(metrics, "availableLicenses")}");
        builder.AppendLine($"- Users checked: {GetMetric(metrics, "usersChecked")}");
        builder.AppendLine($"- Licensed users: {GetMetric(metrics, "licensedUsers")}");
        builder.AppendLine($"- Unlicensed users: {GetMetric(metrics, "unlicensedUsers")}");
        builder.AppendLine($"- Disabled licensed users: {GetMetric(metrics, "disabledLicensedUsers")}");
        builder.AppendLine();

        builder.AppendLine("## Recommendations");
        builder.AppendLine();
        if (recommendations.Count == 0)
        {
            builder.AppendLine("No actionable recommendations were detected.");
        }
        else
        {
            foreach (var recommendation in recommendations)
            {
                builder.AppendLine($"### {recommendation.Title}");
                builder.AppendLine();
                builder.AppendLine($"Severity: {recommendation.Severity}");
                builder.AppendLine();
                builder.AppendLine(recommendation.Detail);
                builder.AppendLine();
                builder.AppendLine($"Recommended action: {recommendation.RecommendedAction}");
                builder.AppendLine();
            }
        }

        builder.AppendLine("## License Summary");
        builder.AppendLine();
        builder.AppendLine("| Product | SKU | Total | Assigned | Available |");
        builder.AppendLine("| --- | --- | ---: | ---: | ---: |");
        foreach (var license in licenseSummary)
        {
            builder.AppendLine($"| {Escape(license.DisplayName)} | `{Escape(license.SkuPartNumber)}` | {license.TotalLicenses} | {license.AssignedLicenses} | {license.AvailableLicenses} |");
        }

        builder.AppendLine();
        builder.AppendLine("## User License Signals");
        builder.AppendLine();
        builder.AppendLine("| User | UPN | Enabled | Licensed | Products |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var user in userLicenses)
        {
            var products = user.Licenses.Count == 0
                ? "None"
                : string.Join(", ", user.Licenses.Select(license => Escape(license.DisplayName)));
            builder.AppendLine($"| {Escape(user.DisplayName ?? "Unknown")} | `{Escape(user.UserPrincipalName ?? "Unknown")}` | {FormatBoolean(user.AccountEnabled)} | {FormatBoolean(user.IsLicensed)} | {products} |");
        }

        return new RenderedReport("markdown", builder.ToString().TrimEnd());
    }

    private static object? GetMetric(IReadOnlyDictionary<string, object?> metrics, string name)
    {
        return metrics.TryGetValue(name, out var value) ? value : null;
    }

    private static string FormatBoolean(bool? value)
    {
        return value switch
        {
            true => "Yes",
            false => "No",
            null => "Unknown"
        };
    }

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
