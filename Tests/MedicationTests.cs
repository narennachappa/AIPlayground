using System.Text.Json;
using MedAssistAgent.Configuration;
using MedAssistAgent.Services;
using MedAssistAgent.Tools;
using Microsoft.Extensions.AI;
using Xunit;

namespace MedAssistAgent.Tests;

public sealed class MedicationTests
{
    private readonly InMemoryMedicationRepository _repository = new();

    [Fact]
    public void PatientLookupReturnsOnlyThatPatientsThreeRecords()
    {
        var records = _repository.GetByPatientId("P1001");
        Assert.Equal(3, records.Count);
        Assert.All(records, r => Assert.Equal("P1001", r.PatientId));
        Assert.Equal(new[] { "active", "draft", "completed" }, records.Select(r => r.Status));
    }

    [Fact]
    public void RequestLookupReturnsDraftAndMissingReturnsNull()
    {
        Assert.Equal("draft", _repository.GetById("MR-202")?.Status);
        Assert.Null(_repository.GetById("MR-999"));
        Assert.Empty(_repository.GetByPatientId("P9999"));
        Assert.Single(_repository.GetByPatientId("P1002"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void MissingIdentifiersAreRejected(string? id)
    {
        Assert.ThrowsAny<ArgumentException>(() => _repository.GetByPatientId(id!));
        Assert.ThrowsAny<ArgumentException>(() => _repository.GetById(id!));
    }

    [Fact]
    public async Task RegisteredFunctionsExposeSchemasAndInvokeRealTools()
    {
        using var log = new StringWriter();
        var functions = new MedicationTools(_repository, log).CreateFunctions().Cast<AIFunction>().ToArray();
        Assert.Equal(2, functions.Length);
        foreach (var function in functions) Assert.False(string.IsNullOrWhiteSpace(function.Description));
        var patient = Assert.Single(functions, f => f.Name == "GetMedicationRequestsByPatientId");
        Assert.Contains("patientId", patient.JsonSchema.ToString());
        var requests = await patient.InvokeAsync(new AIFunctionArguments { ["patientId"] = "P1001" });
        using var patientJson = JsonDocument.Parse(JsonSerializer.Serialize(requests));
        Assert.Equal(3, patientJson.RootElement.GetArrayLength());
        var request = Assert.Single(functions, f => f.Name == "GetMedicationRequestById");
        Assert.Contains("requestId", request.JsonSchema.ToString());
        var result = await request.InvokeAsync(new AIFunctionArguments { ["requestId"] = "MR-202" });
        Assert.Contains("draft", JsonSerializer.Serialize(result));
        var missing = await request.InvokeAsync(new AIFunctionArguments { ["requestId"] = "MR-999" });
        Assert.True(missing is null || JsonSerializer.Serialize(missing) == "null");
        Assert.Contains("[TOOL] GetMedicationRequestsByPatientId {\"patientId\":\"P1001\"}", log.ToString());
        Assert.Contains("[TOOL] GetMedicationRequestById {\"requestId\":\"MR-202\"}", log.ToString());
        Assert.Equal(3, log.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void RegisteringToolsDoesNotExecuteThem()
    {
        using var log = new StringWriter();
        _ = new MedicationTools(_repository, log).CreateFunctions();
        Assert.Equal("", log.ToString());
    }

    [Theory]
    [InlineData(null, "model")]
    [InlineData("http://example.com/api/projects/test", "model")]
    [InlineData("https://example.com/", "model")]
    [InlineData("https://example.com/api/projects/test", " ")]
    public void InvalidConfigurationFailsBeforeNetworkAccess(string? endpoint, string? model) =>
        Assert.Throws<InvalidOperationException>(() => FoundryConfiguration.Parse(endpoint, model));

    [Fact]
    public void ValidConfigurationPreservesProjectEndpointAndDeploymentName()
    {
        var config = FoundryConfiguration.Parse("https://example.services.ai.azure.com/api/projects/demo", " my-deployment ");
        Assert.Equal("my-deployment", config.Model);
        Assert.Equal("/api/projects/demo", config.ProjectEndpoint.AbsolutePath);
    }
}
