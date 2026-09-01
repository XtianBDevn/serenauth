import { ApolloProviderClient } from "@/components/ApolloProviderClient";
import { DashboardView } from "@/components/DashboardView";

export default function DashboardPage() {
  return (
    <div className="mx-auto max-w-6xl px-6 py-12">
      <header className="mb-8 flex flex-col gap-3 border-b border-slate-200 pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-slate-900">
            Prior authorizations
          </h1>
          <p className="mt-1 text-sm text-slate-600">
            All PAs across every payer for your clinic.
          </p>
        </div>
        <button
          type="button"
          className="inline-flex items-center justify-center rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-brand-700"
        >
          + Create authorization
        </button>
      </header>

      <ApolloProviderClient>
        <DashboardView />
      </ApolloProviderClient>
    </div>
  );
}
