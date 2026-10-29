using System.ComponentModel;
using System.Text.Json;
using MedAssistAgent.Domain;
using MedAssistAgent.Services;
using Microsoft.Extensions.AI;

namespace MedAssistAgent.Tools;

public sealed class MedicationTools(IMedicationRepository repository, TextWriter invocationLog)
{
    public IReadOnlyList<AITool> CreateFunctions() =>
    [
        AIFunctionFactory.Create(GetMedicationRequestsByPatientId),
        AIFunctionFactory.Create(GetMedicationRequestById)
    ];

    [Description("Retrieve all synthetic medication requests for one patient ID. Returns an empty list if none exist. Use for patient medication lists; do not guess missing IDs.")]
    public IReadOnlyList<MedicationRequest> GetMedicationRequestsByPatientId(
        [Description("The patient identifier supplied by the user, for example P1001.")] string patientId)
    {
        Log(nameof(GetMedicationRequestsByPatientId), new { patientId });
        return repository.GetByPatientId(patientId);
    }

    [Description("Retrieve one synthetic medication request by its request ID, including medication and FHIR status. Returns null if not found. Use for a specific request, not a patient list.")]
    public MedicationRequest? GetMedicationRequestById(
        [Description("The medication request identifier supplied by the user, for example MR-202.")] string requestId)
    {
        Log(nameof(GetMedicationRequestById), new { requestId });
        return repository.GetById(requestId);
    }

    private void Log(string tool, object arguments) =>
        invocationLog.WriteLine($"[TOOL] {tool} {JsonSerializer.Serialize(arguments)}");
}
