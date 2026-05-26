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
            var report = AccountManagementReportRunner.Run(input);
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
    public static ModuleJobOutput Run(ModuleJobInput input)
    {
        var tenantName = input.TenantContext?.TenantName ?? input.ClientConnectionId;
        var includeInactiveUsers = input.Parameters.TryGetProperty("includeInactiveUsers", out var includeInactiveUsersProperty) &&
                                   includeInactiveUsersProperty.ValueKind == JsonValueKind.True;
        var skuCatalog = SkuCatalogLookup.LoadDefault();

        var findings = new List<ReportFinding>
        {
            new(
                Severity: "Info",
                Code: "MODULE_SCAFFOLD_READY",
                Title: "Module contract validated",
                Detail: $"Generated account-management report scaffold for '{tenantName}'."),
            new(
                Severity: "Info",
                Code: "GRAPH_COLLECTORS_PENDING",
                Title: "Graph collection not enabled yet",
                Detail: "This version returns deterministic scaffold metrics. Graph-backed license and usage collectors will be added in a later version."),
            new(
                Severity: "Info",
                Code: "SKU_CATALOG_LOADED",
                Title: "License SKU catalog loaded",
                Detail: $"Loaded {skuCatalog.Count} Microsoft 365 SKU friendly-name mappings.")
        };

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
                ["licensedUsers"] = 0,
                ["unlicensedUsers"] = 0,
                ["unusedLicenses"] = 0,
                ["estimatedMonthlyWaste"] = 0,
                ["skuMappingsLoaded"] = skuCatalog.Count,
                ["targetCount"] = input.TargetScope?.Targets.Count ?? 0,
                ["checkedAtUtc"] = DateTimeOffset.UtcNow
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
