using MedAssistAgent.Domain;

namespace MedAssistAgent.Services;

public sealed class InMemoryMedicationRepository : IMedicationRepository
{
    private static readonly MedicationRequest[] Requests =
    [
        new("MR-201", "P1001", "Amoxicillin 500 mg", "active"),
        new("MR-202", "P1001", "Paracetamol 500 mg", "draft"),
        new("MR-203", "P1001", "Vitamin D 1000 IU", "completed"),
        new("MR-301", "P1002", "Ibuprofen 200 mg", "active")
    ];

    public IReadOnlyList<MedicationRequest> GetByPatientId(string patientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientId);
        return Array.AsReadOnly(Requests.Where(r => r.PatientId == patientId.Trim()).ToArray());
    }

    public MedicationRequest? GetById(string requestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        return Requests.SingleOrDefault(r => r.RequestId == requestId.Trim());
    }
}
