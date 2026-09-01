export default function DialysisPage() {
  return (
    <div className="mx-auto max-w-3xl px-6 py-20">
      <h1 className="text-3xl font-semibold tracking-tight text-slate-900">
        Built for dialysis, not for everyone
      </h1>
      <p className="mt-5 text-slate-600">
        Most prior authorization platforms target multi-specialty groups. The
        result is a generic intake form bolted to a generic queue. Dialysis
        workflows look different, and SerenAuth is shaped accordingly.
      </p>

      <section className="mt-10 space-y-6">
        <Block
          title="Coded for ESRD and in-center hemodialysis"
          body="The MVP only accepts the dialysis CPT codes (90935 and 90937) and ESRD diagnosis (ICD-10 N18.6). Out-of-scope codes are rejected at the API boundary so an off-domain submission never lands silently in the queue."
        />
        <Block
          title="Recurring authorizations, not one-off claims"
          body="Dialysis is recurring care. Our data model treats each PA as an ongoing relationship with the payer — re-auth dates, expiring approvals, and patient continuity are first-class concepts."
        />
        <Block
          title="Intake and clinician roles are different"
          body="Intake coordinators move PAs from draft to ready. Clinicians submit. Admins read the audit trail. Roles aren't a permissions config — they're how the product is shaped."
        />
        <Block
          title="HIPAA-conscious from the schema up"
          body="No SSN. No demographic data beyond what payers require for medical-necessity review. Audit events are append-only and never overwritten."
        />
      </section>
    </div>
  );
}

function Block({ title, body }: { title: string; body: string }) {
  return (
    <article className="rounded-xl border border-slate-200 p-6">
      <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
      <p className="mt-2 text-sm text-slate-600">{body}</p>
    </article>
  );
}
