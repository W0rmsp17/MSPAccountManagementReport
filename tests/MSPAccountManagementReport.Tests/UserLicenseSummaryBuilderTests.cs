namespace MSPAccountManagementReport.Tests;

public sealed class UserLicenseSummaryBuilderTests
{
    [Fact]
    public void Build_MapsAssignedLicenseToFriendlyProduct()
    {
        var licenseSummary = new LicenseSummaryBuilder(SkuCatalogLookup.LoadDefault()).Build(
        [
            new GraphSubscribedSku
            {
                SkuId = "cbdc14ab-d96c-4c30-b9f4-6ada7cdc1d46",
                SkuPartNumber = "SPB",
                ConsumedUnits = 1,
                PrepaidUnits = new GraphPrepaidUnits
                {
                    Enabled = 5
                }
            }
        ]);
        var builder = new UserLicenseSummaryBuilder(licenseSummary);

        var result = builder.Build(
        [
            new GraphUser
            {
                Id = "user-1",
                DisplayName = "Alex Standard",
                UserPrincipalName = "alex.standard@example.com",
                AccountEnabled = true,
                AssignedLicenses =
                [
                    new GraphAssignedLicense
                    {
                        SkuId = "cbdc14ab-d96c-4c30-b9f4-6ada7cdc1d46"
                    }
                ]
            }
        ]);

        var user = Assert.Single(result);
        Assert.True(user.IsLicensed);
        var license = Assert.Single(user.Licenses);
        Assert.Equal("SPB", license.SkuPartNumber);
        Assert.Equal("Microsoft 365 Business Premium", license.DisplayName);
    }

    [Fact]
    public void Build_MarksUserWithoutAssignedLicensesAsUnlicensed()
    {
        var builder = new UserLicenseSummaryBuilder([]);

        var result = builder.Build(
        [
            new GraphUser
            {
                Id = "user-2",
                DisplayName = "Casey Unlicensed",
                UserPrincipalName = "casey.unlicensed@example.com",
                AccountEnabled = true,
                AssignedLicenses = []
            }
        ]);

        var user = Assert.Single(result);
        Assert.False(user.IsLicensed);
        Assert.Empty(user.Licenses);
    }
}
