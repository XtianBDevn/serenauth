import Link from "next/link";

export default function HomePage() {
  return (
    <div className="bg-white">
      {/* Hero */}
      <section className="border-b border-slate-200">
        <div className="mx-auto max-w-6xl px-6 py-20">
          <p className="text-sm font-medium uppercase tracking-[0.16em] text-brand-700">
            For dialysis clinics
          </p>
          <h1 className="mt-3 max-w-3xl text-4xl font-semibold tracking-tight text-slate-900 sm:text-5xl">
            Calm authorization. Faster care.
          </h1>
          <p className="mt-5 max-w-2xl text-lg text-slate-600">
            Prior authorization software built specifically for dialysis
            clinics. Reduce delays, prevent denials, and keep treatment
            schedules predictable — without adding another tab to your
            staff&apos;s day.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Link
              href="/demo"
              className="inline-flex items-center justify-center rounded-lg bg-brand-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-brand-700"
            >
              Request a demo
            </Link>
            <Link
              href="/dialysis"
              className="inline-flex items-center justify-center rounded-lg border border-slate-200 px-5 py-2.5 text-sm font-medium text-slate-800 hover:border-brand-600 hover:text-brand-700"
            >
              Why dialysis is different
            </Link>
          </div>
        </div>
      </section>

      {/* Challenge */}
      <section className="border-b border-slate-200">
        <div className="mx-auto grid max-w-6xl gap-12 px-6 py-16 md:grid-cols-2">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-900">
              Why prior authorization is hard in dialysis
            </h2>
            <p className="mt-4 text-slate-600">
              ESRD patients depend on uninterrupted dialysis. A late or denied
              prior authorization isn&apos;t a billing nuisance — it&apos;s a
              missed treatment. The workflow today is fragmented across
              spreadsheets, payer portals, and fax queues.
            </p>
          </div>
          <ul className="space-y-3 text-sm text-slate-700">
            {[
              "Three to five payer portals per patient panel",
              "Manual re-entry of CPT 90935 / 90937 and ICD-10 N18.6",
              "Status updates that live in shared inboxes",
              "No durable audit trail when payers dispute medical necessity",
            ].map((item) => (
              <li key={item} className="flex items-start gap-3">
                <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-brand-600" />
                <span>{item}</span>
              </li>
            ))}
          </ul>
        </div>
      </section>

      {/* Solution */}
      <section className="border-b border-slate-200 bg-slate-50">
        <div className="mx-auto grid max-w-6xl gap-12 px-6 py-16 md:grid-cols-2">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-900">
              How SerenAuth helps
            </h2>
            <p className="mt-4 text-slate-600">
              SerenAuth gives intake coordinators, nephrologists, and
              administrators a single, calm surface for moving a prior
              authorization from draft to approved.
            </p>
          </div>
          <ul className="space-y-3 text-sm text-slate-700">
            {[
              "One workspace for every PA across every payer",
              "Built-in dialysis CPT / ICD-10 catalog and validation",
              "Role-aware actions for intake, clinicians, and admins",
              "Immutable audit log every PA can be reviewed against",
            ].map((item) => (
              <li key={item} className="flex items-start gap-3">
                <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-brand-600" />
                <span>{item}</span>
              </li>
            ))}
          </ul>
        </div>
      </section>

      {/* Patient-centered */}
      <section className="border-b border-slate-200">
        <div className="mx-auto max-w-3xl px-6 py-16">
          <h2 className="text-2xl font-semibold tracking-tight text-slate-900">
            Patient-centered, not paperwork-centered
          </h2>
          <p className="mt-4 text-slate-600">
            Every workflow inside SerenAuth begins with the patient&apos;s
            treatment plan, not with the form. Estimators and care
            coordinators see the same fields a nephrologist would: weight,
            access type, modality, prior authorization status. Nothing more,
            and nothing less than the minimum necessary.
          </p>
        </div>
      </section>

      {/* Compliance */}
      <section className="border-b border-slate-200 bg-slate-50">
        <div className="mx-auto grid max-w-6xl gap-12 px-6 py-16 md:grid-cols-2">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-900">
              Compliance &amp; trust
            </h2>
            <p className="mt-4 text-slate-600">
              We treat clinic IT teams as the first reviewer of our product.
              SerenAuth ships with HIPAA-conscious defaults so your security
              review can focus on policy, not plumbing.
            </p>
          </div>
          <ul className="space-y-3 text-sm text-slate-700">
            {[
              "Least-privilege role model (Viewer / Intake / Clinician / Admin)",
              "Immutable audit log for every sensitive action",
              "Per-tenant isolation enforced server-side",
              "PHI minimized to fields the workflow actually needs",
            ].map((item) => (
              <li key={item} className="flex items-start gap-3">
                <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-brand-600" />
                <span>{item}</span>
              </li>
            ))}
          </ul>
        </div>
      </section>

      {/* CTA */}
      <section className="border-b border-slate-200">
        <div className="mx-auto flex max-w-6xl flex-col items-start justify-between gap-6 px-6 py-16 md:flex-row md:items-center">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-900">
              See SerenAuth on your panel
            </h2>
            <p className="mt-2 text-slate-600">
              Tell us a little about your clinic and we&apos;ll set up a
              walkthrough with a member of our team.
            </p>
          </div>
          <Link
            href="/demo"
            className="inline-flex items-center justify-center rounded-lg bg-brand-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-brand-700"
          >
            Request a demo
          </Link>
        </div>
      </section>
    </div>
  );
}
