using System.Text.Json;

namespace MSPAccountManagementReport.Tests;

public sealed class AccountManagementReportRunnerTests
{
    [Fact]
    public void Run_ReturnsSucceededOutputWithExpectedMetrics()
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

        var result = AccountManagementReportRunner.Run(input);

        Assert.Equal("Succeeded", result.Status);
        Assert.Equal("job-test", result.JobId);
        Assert.Contains("Contoso", result.Summary);
        Assert.Equal(1, result.Metrics["targetCount"]);
        Assert.Contains(result.Findings, finding => finding.Code == "INACTIVE_USER_SECTION_REQUESTED");
    }
}
