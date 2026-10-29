using MedAssistAgent.Domain;

namespace MedAssistAgent.Services;

public interface IMedicationRepository
{
    IReadOnlyList<MedicationRequest> GetByPatientId(string patientId);
    MedicationRequest? GetById(string requestId);
}
