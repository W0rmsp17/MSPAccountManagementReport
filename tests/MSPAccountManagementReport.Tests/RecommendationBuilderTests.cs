namespace MSPAccountManagementReport.Tests;

public sealed class RecommendationBuilderTests
{
    [Fact]
    public void Build_ReturnsExpectedAccountManagementRecommendations()
    {
        var builder = new RecommendationBuilder();

        var recommendations = builder.Build(
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
                DisplayName = "Disabled User",
                UserPrincipalName = "disabled.user@example.com",
                AccountEnabled = false,
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
            },
            new UserLicenseItem
            {
                DisplayName = "Unlicensed User",
                UserPrincipalName = "unlicensed.user@example.com",
                AccountEnabled = true,
                IsLicensed = false
            }
        ]);

        Assert.Contains(recommendations, recommendation => recommendation.Code == "DISABLED_USERS_WITH_LICENSES");
        Assert.Contains(recommendations, recommendation => recommendation.Code == "UNLICENSED_USERS_PRESENT");
        Assert.Contains(recommendations, recommendation => recommendation.Code == "AVAILABLE_LICENSE_CAPACITY");
    }

    [Fact]
    public void Build_ReturnsNoRecommendationsWhenNoActionableSignalsExist()
    {
        var builder = new RecommendationBuilder();

        var recommendations = builder.Build(
        [
            new LicenseSummaryItem
            {
                SkuPartNumber = "SPB",
                SkuId = "sku-1",
                DisplayName = "Microsoft 365 Business Premium",
                FriendlyNameKnown = true,
                TotalLicenses = 1,
                AssignedLicenses = 1,
                AvailableLicenses = 0
            }
        ],
        [
            new UserLicenseItem
            {
                DisplayName = "Licensed User",
                UserPrincipalName = "licensed.user@example.com",
                AccountEnabled = true,
                IsLicensed = true
            }
        ]);

        Assert.Empty(recommendations);
    }
}
