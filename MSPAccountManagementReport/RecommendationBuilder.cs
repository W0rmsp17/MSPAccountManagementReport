namespace MSPAccountManagementReport;

public sealed class RecommendationBuilder
{
    public IReadOnlyList<ReportRecommendation> Build(
        IReadOnlyList<LicenseSummaryItem> licenseSummary,
        IReadOnlyList<UserLicenseItem> userLicenses)
    {
        var recommendations = new List<ReportRecommendation>();
        var disabledLicensedUsers = userLicenses
            .Where(user => user.IsLicensed && user.AccountEnabled == false)
            .ToList();
        if (disabledLicensedUsers.Count > 0)
        {
            recommendations.Add(new ReportRecommendation(
                Severity: "Warning",
                Code: "DISABLED_USERS_WITH_LICENSES",
                Title: "Disabled users still have licenses assigned",
                Detail: $"{disabledLicensedUsers.Count} disabled user account(s) still have at least one Microsoft 365 license assigned.",
                RecommendedAction: "Review disabled licensed users and reclaim licenses where access is no longer required."));
        }

        var unlicensedUsers = userLicenses
            .Where(user => !user.IsLicensed)
            .ToList();
        if (unlicensedUsers.Count > 0)
        {
            recommendations.Add(new ReportRecommendation(
                Severity: "Info",
                Code: "UNLICENSED_USERS_PRESENT",
                Title: "Unlicensed users present",
                Detail: $"{unlicensedUsers.Count} user account(s) do not have Microsoft 365 licenses assigned.",
                RecommendedAction: "Confirm these accounts are service, admin, guest, or intentionally unlicensed users."));
        }

        var skusWithAvailableCapacity = licenseSummary
            .Where(sku => sku.AvailableLicenses > 0)
            .ToList();
        if (skusWithAvailableCapacity.Count > 0)
        {
            var availableCount = skusWithAvailableCapacity.Sum(sku => sku.AvailableLicenses);
            recommendations.Add(new ReportRecommendation(
                Severity: "Info",
                Code: "AVAILABLE_LICENSE_CAPACITY",
                Title: "Available license capacity detected",
                Detail: $"{availableCount} paid license seat(s) appear available across {skusWithAvailableCapacity.Count} SKU(s).",
                RecommendedAction: "Review whether available seats are required for upcoming onboarding or can be reduced at renewal."));
        }

        return recommendations;
    }
}
