namespace MSPAccountManagementReport.Tests;

public sealed class SkuCatalogLookupTests
{
    [Fact]
    public void LoadDefault_ResolvesCommonMicrosoft365Sku()
    {
        var lookup = SkuCatalogLookup.LoadDefault();

        var entry = lookup.FindBySkuPartNumber("SPB");

        Assert.NotNull(entry);
        Assert.Equal("Microsoft 365 Business Premium", entry.DisplayName);
        Assert.Equal("SPB", entry.SkuPartNumber);
        Assert.NotEmpty(entry.SkuId);
    }

    [Fact]
    public void GetDisplayNameOrFallback_ReturnsSkuPartNumberForUnknownSku()
    {
        var lookup = SkuCatalogLookup.LoadDefault();

        var displayName = lookup.GetDisplayNameOrFallback("UNKNOWN_TEST_SKU");

        Assert.Equal("UNKNOWN_TEST_SKU", displayName);
    }
}
