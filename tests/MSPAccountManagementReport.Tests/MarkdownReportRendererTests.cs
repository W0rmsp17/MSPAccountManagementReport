namespace MSPAccountManagementReport.Tests;

public sealed class MarkdownReportRendererTests
{
    [Fact]
    public void Render_IncludesSummaryRecommendationsLicensesAndUsers()
    {
        var renderer = new MarkdownReportRenderer();

        var report = renderer.Render(
            "Contoso",
            new Dictionary<string, object?>
            {
                ["licenseSkuCount"] = 1,
                ["totalLicenses"] = 10,
                ["assignedLicenses"] = 7,
                ["availableLicenses"] = 3,
                ["usersChecked"] = 1,
                ["licensedUsers"] = 1,
                ["unlicensedUsers"] = 0,
                ["disabledLicensedUsers"] = 0
            },
            [
                new LicenseSummaryItem
                {
                    SkuPartNumber = "SPB",
                    SkuId = "sku-1",
                    DisplayName = "Microsoft 365 Business Premium",
                    FriendlyNameKnown = true,
                    TotalLicenses = 10,
                    AssignedLicenses = 7,
                    AvailableLicenses = 3
                }
            ],
            [
                new UserLicenseItem
                {
                    DisplayName = "Alex Standard",
                    UserPrincipalName = "alex.standard@example.com",
                    AccountEnabled = true,
                    IsLicensed = true,
                    Licenses =
                    [
                        new UserAssignedLicenseItem
                        {
                            SkuId = "sku-1",
                            SkuPartNumber = "SPB",
                            DisplayName = "Microsoft 365 Business Premium",
                            FriendlyNameKnown = true
                        }
                    ]
                }
            ],
            [
                new ReportRecommendation(
                    Severity: "Info",
                    Code: "AVAILABLE_LICENSE_CAPACITY",
                    Title: "Available license capacity detected",
                    Detail: "3 paid license seat(s) appear available.",
                    RecommendedAction: "Review license needs.")
            ]);

        Assert.Equal("markdown", report.Format);
        Assert.Contains("# MSP Account Management Report - Contoso", report.Content);
        Assert.Contains("## Recommendations", report.Content);
        Assert.Contains("Available license capacity detected", report.Content);
        Assert.Contains("Microsoft 365 Business Premium", report.Content);
        Assert.Contains("alex.standard@example.com", report.Content);
    }
}
