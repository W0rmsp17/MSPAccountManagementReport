using System.Text.Json;

namespace MSPAccountManagementReport.Tests;

public sealed class AccountManagementReportRunnerTests
{
    [Fact]
    public async Task RunAsync_ReturnsSucceededOutputWithExpectedLicenseSummary()
    {
        var input = new ModuleJobInput
        {
            JobId = "job-test",
            ModuleId = "msp-account-management-report",
            ModuleVersion = "0.1.0",
            ClientConnectionId = "client-contoso",
            TenantContext = new TenantContext
            {
                ClientId = "client-contoso",
                TenantName = "Contoso"
            },
            TargetScope = new TargetScope
            {
                Type = "Users",
                Targets =
                [
                    new TargetScopeTarget
                    {
                        Id = "alex.example@contoso.com",
                        UserPrincipalName = "alex.example@contoso.com"
                    }
                ]
            },
            Parameters = JsonDocument.Parse("""{"includeInactiveUsers":true}""").RootElement
        };

        var result = await AccountManagementReportRunner.RunAsync(input);

        Assert.Equal("Succeeded", result.Status);
        Assert.Equal("job-test", result.JobId);
        Assert.Contains("Contoso", result.Summary);
        Assert.Equal(1, result.Metrics["targetCount"]);
        Assert.Equal(2, result.Metrics["licenseSkuCount"]);
        Assert.Equal(30, result.Metrics["totalLicenses"]);
        Assert.Equal(20, result.Metrics["assignedLicenses"]);
        Assert.Equal(10, result.Metrics["availableLicenses"]);
        Assert.Equal(3, result.Metrics["usersChecked"]);
        Assert.Equal(2, result.Metrics["licensedUsers"]);
        Assert.Equal(1, result.Metrics["unlicensedUsers"]);
        Assert.Equal(1, result.Metrics["disabledLicensedUsers"]);
        Assert.Equal(3, result.Metrics["recommendationCount"]);
        Assert.NotNull(result.Report);
        Assert.Contains(result.Report.LicenseSummary.Items, item => item.SkuPartNumber == "SPB" && item.DisplayName == "Microsoft 365 Business Premium");
        Assert.Contains(result.Report.UserLicenses.Items, item => item.UserPrincipalName == "alex.standard@example.com" && item.IsLicensed);
        Assert.Contains(result.Report.Recommendations, recommendation => recommendation.Code == "DISABLED_USERS_WITH_LICENSES");
        Assert.Contains(result.Report.Recommendations, recommendation => recommendation.Code == "UNLICENSED_USERS_PRESENT");
        Assert.Contains(result.Report.Recommendations, recommendation => recommendation.Code == "AVAILABLE_LICENSE_CAPACITY");
        Assert.NotNull(result.Report.RenderedReport);
        Assert.Equal("markdown", result.Report.RenderedReport.Format);
        Assert.Contains("MSP Account Management Report - Contoso", result.Report.RenderedReport.Content);
        Assert.Contains("Microsoft 365 Business Premium", result.Report.RenderedReport.Content);
        Assert.Contains(result.Findings, finding => finding.Code == "INACTIVE_USER_SECTION_REQUESTED");
        Assert.Contains(result.Findings, finding => finding.Code == "UNKNOWN_SKU_MAPPING");
        Assert.Contains(result.Findings, finding => finding.Code == "DISABLED_USERS_WITH_LICENSES");
    }
}
