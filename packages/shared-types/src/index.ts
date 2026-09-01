// Types shared between web (Apollo) and any future TS consumers.
// Source of truth is the .NET API; these mirror DTO/enum shape.

export type PaStatus = "DRAFT" | "PENDING" | "APPROVED" | "DENIED";

export type Role = "Viewer" | "Intake" | "Clinician" | "Admin";

export interface PriorAuthorizationDto {
  id: string;
  patientId: string;
  providerId: string;
  procedureCpt: string;
  diagnosisIcd10: string;
  payer: string;
  status: PaStatus;
  aiConfidence: number;
  createdAt: string;
  updatedAt: string;
}

export interface PatientDto {
  id: string;
  externalMrn: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
}

export interface ProviderDto {
  id: string;
  firstName: string;
  lastName: string;
  npi: string;
  specialty: string;
}
