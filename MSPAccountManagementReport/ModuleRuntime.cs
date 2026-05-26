using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;

namespace MSPAccountManagementReport;

public static class ModuleProgram
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var input = await ModuleInputLoader.LoadAsync(args);
            var report = await AccountManagementReportRunner.RunAsync(input);
            await ModuleOutputWriter.WriteAsync(report);

            Console.WriteLine($"Account management report completed for job '{input.JobId}'.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    internal static string ToJson(object value) => JsonSerializer.Serialize(value, JsonOptions);

    internal static T? FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonOptions);
}

public static class ModuleInputLoader
{
    public static async Task<ModuleJobInput> LoadAsync(string[] args)
    {
        if (args.Length > 0 && File.Exists(args[0]))
        {
            return await LoadFromJsonAsync(await File.ReadAllTextAsync(args[0]));
        }

        var encoded = Environment.GetEnvironmentVariable("CONTROL_PLANE_JOB_INPUT_BASE64");
        if (!string.IsNullOrWhiteSpace(encoded))
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            return await LoadFromJsonAsync(json);
        }

        var localSamplePath = Path.Combine(AppContext.BaseDirectory, "samples", "job-input.json");
        if (File.Exists(localSamplePath))
        {
            return await LoadFromJsonAsync(await File.ReadAllTextAsync(localSamplePath));
        }

        throw new InvalidOperationException("No job input supplied. Pass a JSON file path or set CONTROL_PLANE_JOB_INPUT_BASE64.");
    }

    private static Task<ModuleJobInput> LoadFromJsonAsync(string json)
    {
        var input = ModuleProgram.FromJson<ModuleJobInput>(json);
        if (input is null)
        {
            throw new InvalidOperationException("Job input could not be parsed.");
        }

        if (string.IsNullOrWhiteSpace(input.JobId))
        {
            throw new InvalidOperationException("Job input is missing jobId.");
        }

        if (string.IsNullOrWhiteSpace(input.ClientConnectionId))
        {
            throw new InvalidOperationException("Job input is missing clientConnectionId.");
        }

        return Task.FromResult(input);
    }
}

public static class ModuleOutputWriter
{
    public static async Task WriteAsync(ModuleJobOutput output)
    {
        var json = ModuleProgram.ToJson(output);
        var outputBlobUri = Environment.GetEnvironmentVariable("CONTROL_PLANE_OUTPUT_BLOB_URI");
        if (!string.IsNullOrWhiteSpace(outputBlobUri))
        {
            var blobClient = new BlobClient(new Uri(outputBlobUri));
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await blobClient.UploadAsync(stream, overwrite: true);
            Console.WriteLine("Report output uploaded to control plane artifact storage.");
            return;
        }

        var outputPath = Environment.GetEnvironmentVariable("CONTROL_PLANE_OUTPUT_PATH");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(".out", "result.json");
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, json);
        Console.WriteLine($"Report output written to {outputPath}.");
    }
}

public static class AccountManagementReportRunner
{
    public static async Task<ModuleJobOutput> RunAsync(ModuleJobInput input, HttpClient? httpClient = null)
    {
        var tenantName = input.TenantContext?.TenantName ?? input.ClientConnectionId;
        var includeInactiveUsers = input.Parameters.TryGetProperty("includeInactiveUsers", out var includeInactiveUsersProperty) &&
                                   includeInactiveUsersProperty.ValueKind == JsonValueKind.True;
        var skuCatalog = SkuCatalogLookup.LoadDefault();
        using var ownedHttpClient = httpClient is null ? new HttpClient() : null;
        var collector = new SubscribedSkuCollector(
            httpClient ?? ownedHttpClient!,
            Environment.GetEnvironmentVariable("GRAPH_ACCESS_TOKEN"));
        var subscribedSkus = await collector.CollectAsync();
        var licenseSummary = new LicenseSummaryBuilder(skuCatalog).Build(subscribedSkus.Skus);
        var userCollector = new UserCollector(
            httpClient ?? ownedHttpClient!,
            Environment.GetEnvironmentVariable("GRAPH_ACCESS_TOKEN"));
        var users = await userCollector.CollectAsync();
        var userLicenses = new UserLicenseSummaryBuilder(licenseSummary).Build(users.Users);
        var recommendations = new RecommendationBuilder().Build(licenseSummary, userLicenses);

        var findings = new List<ReportFinding>
        {
            new(
                Severity: "Info",
                Code: "MODULE_SCAFFOLD_READY",
                Title: "Module contract validated",
                Detail: $"Generated account-management report scaffold for '{tenantName}'."),
            new(
                Severity: "Info",
                Code: subscribedSkus.Source == "Graph" ? "GRAPH_SUBSCRIBED_SKUS_COLLECTED" : "GRAPH_SUBSCRIBED_SKUS_SAMPLE_USED",
                Title: subscribedSkus.Source == "Graph" ? "Subscribed SKU collection completed" : "Sample subscribed SKU data used",
                Detail: subscribedSkus.Source == "Graph"
                    ? "Collected subscribed SKU data from Microsoft Graph."
                    : "No GRAPH_ACCESS_TOKEN was supplied, so the module used local sample subscribed SKU data."),
            new(
                Severity: "Info",
                Code: users.Source == "Graph" ? "GRAPH_USERS_COLLECTED" : "GRAPH_USERS_SAMPLE_USED",
                Title: users.Source == "Graph" ? "User collection completed" : "Sample user data used",
                Detail: users.Source == "Graph"
                    ? "Collected user license assignment data from Microsoft Graph."
                    : "No GRAPH_ACCESS_TOKEN was supplied, so the module used local sample user data."),
            new(
                Severity: "Info",
                Code: "SKU_CATALOG_LOADED",
                Title: "License SKU catalog loaded",
                Detail: $"Loaded {skuCatalog.Count} Microsoft 365 SKU friendly-name mappings.")
        };

        foreach (var unknownSku in licenseSummary.Where(item => !item.FriendlyNameKnown))
        {
            findings.Add(new ReportFinding(
                Severity: "Warning",
                Code: "UNKNOWN_SKU_MAPPING",
                Title: "Unknown license SKU mapping",
                Detail: $"No friendly-name mapping was found for SKU '{unknownSku.SkuPartNumber}'. The technical SKU value was used as the display name."));
        }

        foreach (var recommendation in recommendations)
        {
            findings.Add(new ReportFinding(
                Severity: recommendation.Severity,
                Code: recommendation.Code,
                Title: recommendation.Title,
                Detail: recommendation.Detail));
        }

        if (includeInactiveUsers)
        {
            findings.Add(new ReportFinding(
                Severity: "Info",
                Code: "INACTIVE_USER_SECTION_REQUESTED",
                Title: "Inactive user section requested",
                Detail: "The request asked for inactive user analysis. This will require Graph reporting data in a later implementation."));
        }

        return new ModuleJobOutput
        {
            JobId = input.JobId,
            Status = "Succeeded",
            Summary = $"Account management report scaffold generated for {tenantName}.",
            Findings = findings,
            Metrics = new Dictionary<string, object?>
            {
                ["licenseSkuCount"] = licenseSummary.Count,
                ["totalLicenses"] = licenseSummary.Sum(item => item.TotalLicenses),
                ["assignedLicenses"] = licenseSummary.Sum(item => item.AssignedLicenses),
                ["availableLicenses"] = licenseSummary.Sum(item => item.AvailableLicenses),
                ["usersChecked"] = userLicenses.Count,
                ["licensedUsers"] = userLicenses.Count(item => item.IsLicensed),
                ["unlicensedUsers"] = userLicenses.Count(item => !item.IsLicensed),
                ["disabledLicensedUsers"] = userLicenses.Count(item => item.IsLicensed && item.AccountEnabled == false),
                ["recommendationCount"] = recommendations.Count,
                ["unknownSkuMappings"] = licenseSummary.Count(item => !item.FriendlyNameKnown),
                ["estimatedMonthlyWaste"] = 0,
                ["skuMappingsLoaded"] = skuCatalog.Count,
                ["subscribedSkuSource"] = subscribedSkus.Source,
                ["userSource"] = users.Source,
                ["targetCount"] = input.TargetScope?.Targets.Count ?? 0,
                ["checkedAtUtc"] = DateTimeOffset.UtcNow
            },
            Report = new AccountManagementReportData
            {
                LicenseSummary = new LicenseReportSection
                {
                    Items = licenseSummary
                },
                UserLicenses = new UserLicenseReportSection
                {
                    Items = userLicenses
                },
                Recommendations = recommendations
            },
            Artifacts =
            [
                new ReportArtifact(
                    Type: "json-report",
                    Name: "Account management report",
                    Uri: null)
            ]
        };
    }
}
