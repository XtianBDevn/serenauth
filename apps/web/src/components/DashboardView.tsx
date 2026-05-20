"use client";

import { useMemo, useState } from "react";
import { useQuery } from "@apollo/client";
import { PRIOR_AUTHORIZATIONS_QUERY } from "@/lib/graphql/operations";
import type { PriorAuthorizationDto, PaStatus } from "@serenauth/shared-types";

const STATUSES: Array<PaStatus | "ALL"> = ["ALL", "DRAFT", "PENDING", "APPROVED", "DENIED"];

export function DashboardView() {
  const [payer, setPayer] = useState<string>("");
  const [status, setStatus] = useState<PaStatus | "ALL">("ALL");

  const { data, loading, error } = useQuery<{
    priorAuthorizations: PriorAuthorizationDto[];
  }>(PRIOR_AUTHORIZATIONS_QUERY, {
    variables: {
      status: status === "ALL" ? null : status,
      payer: payer || null,
      limit: 100,
    },
    errorPolicy: "all",
  });

  const rows = data?.priorAuthorizations ?? [];
  const payers = useMemo(() => {
    const set = new Set<string>();
    for (const r of rows) set.add(r.payer);
    return Array.from(set).sort();
  }, [rows]);

  return (
    <section className="space-y-6">
      <div className="flex flex-wrap items-center gap-3">
        <div className="flex flex-wrap items-center gap-1">
          {STATUSES.map((s) => (
            <button
              key={s}
              type="button"
              onClick={() => setStatus(s)}
              className={
                "rounded-full px-3 py-1 text-xs " +
                (status === s
                  ? "bg-brand-600 text-white"
                  : "border border-slate-200 text-slate-700 hover:border-brand-600 hover:text-brand-700")
              }
            >
              {s}
            </button>
          ))}
        </div>
        <select
          aria-label="Filter by payer"
          value={payer}
          onChange={(e) => setPayer(e.target.value)}
          className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-800 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600"
        >
          <option value="">All payers</option>
          {payers.map((p) => (
            <option key={p} value={p}>
              {p}
            </option>
          ))}
        </select>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-xs uppercase tracking-[0.12em] text-slate-500">
            <tr>
              <Th>PA</Th>
              <Th>Payer</Th>
              <Th>Procedure</Th>
              <Th>Diagnosis</Th>
              <Th>Status</Th>
              <Th>AI confidence</Th>
              <Th>Updated</Th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white">
            {loading ? (
              <tr>
                <td colSpan={7} className="px-5 py-6 text-center text-slate-500">
                  Loading…
                </td>
              </tr>
            ) : error ? (
              <tr>
                <td colSpan={7} className="px-5 py-6 text-center text-rose-600">
                  Failed to load authorizations.
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-5 py-6 text-center text-slate-500">
                  No authorizations match these filters yet.
                </td>
              </tr>
            ) : (
              rows.map((r) => (
                <tr key={r.id} className="hover:bg-slate-50/60">
                  <Td mono>{r.id.slice(0, 8).toUpperCase()}</Td>
                  <Td>{r.payer}</Td>
                  <Td>{r.procedureCpt}</Td>
                  <Td>{r.diagnosisIcd10}</Td>
                  <Td>
                    <StatusPill status={r.status} />
                  </Td>
                  <Td>{Math.round(r.aiConfidence * 100)}%</Td>
                  <Td>{new Date(r.updatedAt).toLocaleDateString()}</Td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function StatusPill({ status }: { status: PaStatus }) {
  const tone =
    status === "APPROVED"
      ? "bg-emerald-50 text-emerald-700 ring-emerald-200"
      : status === "DENIED"
      ? "bg-rose-50 text-rose-700 ring-rose-200"
      : status === "PENDING"
      ? "bg-amber-50 text-amber-700 ring-amber-200"
      : "bg-slate-50 text-slate-700 ring-slate-200";
  return (
    <span
      className={
        "inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset " +
        tone
      }
    >
      {status}
    </span>
  );
}

function Th({ children }: { children: React.ReactNode }) {
  return <th className="px-5 py-3 text-left font-medium">{children}</th>;
}

function Td({
  children,
  mono = false,
}: {
  children: React.ReactNode;
  mono?: boolean;
}) {
  return (
    <td
      className={
        "px-5 py-3 text-slate-700 " + (mono ? "font-mono text-xs" : "")
      }
    >
      {children}
    </td>
  );
}
