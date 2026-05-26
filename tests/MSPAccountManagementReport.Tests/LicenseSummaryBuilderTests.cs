namespace MSPAccountManagementReport.Tests;

public sealed class LicenseSummaryBuilderTests
{
    [Fact]
    public void Build_MapsSkuFriendlyNameAndAvailability()
    {
        var builder = new LicenseSummaryBuilder(SkuCatalogLookup.LoadDefault());

        var result = builder.Build(
        [
            new GraphSubscribedSku
            {
                SkuId = "cbdc14ab-d96c-4c30-b9f4-6ada7cdc1d46",
                SkuPartNumber = "SPB",
                ConsumedUnits = 18,
                PrepaidUnits = new GraphPrepaidUnits
                {
                    Enabled = 25
                }
            }
        ]);

        var item = Assert.Single(result);
        Assert.Equal("Microsoft 365 Business Premium", item.DisplayName);
        Assert.Equal(25, item.TotalLicenses);
        Assert.Equal(18, item.AssignedLicenses);
        Assert.Equal(7, item.AvailableLicenses);
        Assert.True(item.FriendlyNameKnown);
    }

    [Fact]
    public void Build_UsesSkuPartNumberWhenMappingIsUnknown()
    {
        var builder = new LicenseSummaryBuilder(SkuCatalogLookup.LoadDefault());

        var result = builder.Build(
        [
            new GraphSubscribedSku
            {
                SkuId = "00000000-0000-0000-0000-000000000000",
                SkuPartNumber = "UNKNOWN_TEST_SKU",
                ConsumedUnits = 1,
                PrepaidUnits = new GraphPrepaidUnits
                {
                    Enabled = 2
                }
            }
        ]);

        var item = Assert.Single(result);
        Assert.Equal("UNKNOWN_TEST_SKU", item.DisplayName);
        Assert.False(item.FriendlyNameKnown);
    }
}
