"use client";

import { FormEvent, useState } from "react";

interface IntakeState {
  practiceName: string;
  contactName: string;
  email: string;
  monthlyAuthorizations: string;
}

interface IntakeErrors {
  practiceName?: string;
  contactName?: string;
  email?: string;
  monthlyAuthorizations?: string;
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function validate(state: IntakeState): IntakeErrors {
  const errors: IntakeErrors = {};
  if (!state.practiceName.trim()) errors.practiceName = "Practice name is required.";
  if (!state.contactName.trim()) errors.contactName = "Contact name is required.";
  if (!state.email.trim()) errors.email = "Email is required.";
  else if (!EMAIL_PATTERN.test(state.email)) errors.email = "Enter a valid email.";
  const n = Number(state.monthlyAuthorizations);
  if (!state.monthlyAuthorizations.trim()) {
    errors.monthlyAuthorizations = "Estimate is required.";
  } else if (!Number.isFinite(n) || n < 0) {
    errors.monthlyAuthorizations = "Enter a non-negative number.";
  }
  return errors;
}

export function IntakeForm() {
  const [state, setState] = useState<IntakeState>({
    practiceName: "",
    contactName: "",
    email: "",
    monthlyAuthorizations: "",
  });
  const [errors, setErrors] = useState<IntakeErrors>({});
  const [submitted, setSubmitted] = useState(false);

  function update<K extends keyof IntakeState>(key: K, value: IntakeState[K]) {
    setState((s) => ({ ...s, [key]: value }));
  }

  function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const result = validate(state);
    setErrors(result);
    if (Object.keys(result).length > 0) return;

    // Mock submission. In production this would call a server action.
    // eslint-disable-next-line no-console
    console.log("[SerenAuth] demo intake submitted", state);
    setSubmitted(true);
  }

  if (submitted) {
    return (
      <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-6 text-sm text-emerald-900">
        <p className="font-semibold">Thanks — we&apos;ll be in touch.</p>
        <p className="mt-2">
          A member of the SerenAuth team will email <strong>{state.email}</strong>{" "}
          within one business day to schedule a walkthrough.
        </p>
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-5">
      <Field
        label="Practice name"
        error={errors.practiceName}
        input={
          <input
            type="text"
            required
            value={state.practiceName}
            onChange={(e) => update("practiceName", e.target.value)}
            className={inputClass}
            placeholder="Riverbend Dialysis"
          />
        }
      />
      <Field
        label="Contact name"
        error={errors.contactName}
        input={
          <input
            type="text"
            required
            value={state.contactName}
            onChange={(e) => update("contactName", e.target.value)}
            className={inputClass}
            placeholder="Avery Carter"
          />
        }
      />
      <Field
        label="Work email"
        error={errors.email}
        input={
          <input
            type="email"
            required
            value={state.email}
            onChange={(e) => update("email", e.target.value)}
            className={inputClass}
            placeholder="avery@riverbend.example"
          />
        }
      />
      <Field
        label="Estimated monthly authorizations"
        error={errors.monthlyAuthorizations}
        input={
          <input
            type="number"
            min={0}
            required
            value={state.monthlyAuthorizations}
            onChange={(e) => update("monthlyAuthorizations", e.target.value)}
            className={inputClass}
            placeholder="120"
          />
        }
      />
      <button
        type="submit"
        className="inline-flex items-center justify-center rounded-lg bg-brand-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-brand-700"
      >
        Request walkthrough
      </button>
    </form>
  );
}

const inputClass =
  "w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 placeholder:text-slate-400 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600";

function Field({
  label,
  input,
  error,
}: {
  label: string;
  input: React.ReactNode;
  error?: string;
}) {
  return (
    <label className="block space-y-1.5">
      <span className="text-sm font-medium text-slate-800">{label}</span>
      {input}
      {error ? <p className="text-xs text-rose-600">{error}</p> : null}
    </label>
  );
}
