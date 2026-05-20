import { render, screen, waitFor } from "@testing-library/react";
import { MockedProvider } from "@apollo/client/testing";
import { DashboardView } from "../components/DashboardView";
import { PRIOR_AUTHORIZATIONS_QUERY } from "../lib/graphql/operations";

const mocks = [
  {
    request: {
      query: PRIOR_AUTHORIZATIONS_QUERY,
      variables: { status: null, payer: null, limit: 100 },
    },
    result: {
      data: {
        priorAuthorizations: [
          {
            id: "abcd1234-aaaa-bbbb-cccc-ddddeeeeffff",
            patientId: "p1",
            providerId: "pr1",
            procedureCpt: "90935",
            diagnosisIcd10: "N18.6",
            payer: "BCBS",
            status: "PENDING",
            aiConfidence: 0.82,
            createdAt: "2026-04-01T00:00:00Z",
            updatedAt: "2026-04-02T00:00:00Z",
          },
        ],
      },
    },
  },
];

describe("DashboardView", () => {
  it("renders a PA row from a mocked GraphQL response", async () => {
    render(
      <MockedProvider mocks={mocks} addTypename={false}>
        <DashboardView />
      </MockedProvider>,
    );

    await waitFor(() => {
      expect(screen.getByText("BCBS")).toBeInTheDocument();
    });
    expect(screen.getByText("90935")).toBeInTheDocument();
    expect(screen.getByText("N18.6")).toBeInTheDocument();
    expect(screen.getByText("PENDING")).toBeInTheDocument();
    expect(screen.getByText("82%")).toBeInTheDocument();
  });
});
