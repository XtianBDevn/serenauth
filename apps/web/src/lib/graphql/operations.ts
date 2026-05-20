import { gql } from "@apollo/client";

export const PRIOR_AUTHORIZATIONS_QUERY = gql`
  query PriorAuthorizations($status: PaStatus, $payer: String, $limit: Int) {
    priorAuthorizations(status: $status, payer: $payer, limit: $limit) {
      id
      patientId
      providerId
      procedureCpt
      diagnosisIcd10
      payer
      status
      aiConfidence
      createdAt
      updatedAt
    }
  }
`;
