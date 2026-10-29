namespace MedAssistAgent.Domain;

// Simplified internal DTO, not a complete FHIR resource. Status uses FHIR R4 codes.
public sealed record MedicationRequest(
    string RequestId, string PatientId, string Medication, string Status);
