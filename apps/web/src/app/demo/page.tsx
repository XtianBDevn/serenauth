import { IntakeForm } from "@/components/IntakeForm";

export default function DemoPage() {
  return (
    <div className="mx-auto max-w-2xl px-6 py-20">
      <h1 className="text-3xl font-semibold tracking-tight text-slate-900">
        Request a demo
      </h1>
      <p className="mt-3 text-slate-600">
        Tell us about your clinic and our team will reach out within one
        business day. Nothing on this page is stored as PHI — we ask only
        what we need to schedule a walkthrough.
      </p>
      <div className="mt-10">
        <IntakeForm />
      </div>
    </div>
  );
}
