"use client";

import { FormEvent, useState } from "react";
import { useMutation } from "@apollo/client";
import { useRouter } from "next/navigation";
import { LOGIN_MUTATION } from "@/lib/graphql/operations";
import type { Role } from "@serenauth/shared-types";

interface LoginResult {
  token: string;
  email: string;
  displayName: string;
  role: Role;
  organizationId: string;
  issuedAt: string;
}

const DEMO_USERS = [
  { label: "Admin", email: "admin@riverbend.example" },
  { label: "Clinician", email: "clin@riverbend.example" },
  { label: "Intake", email: "intake@riverbend.example" },
];

export function LoginForm() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitError, setSubmitError] = useState<string | null>(null);

  const [login, { loading }] = useMutation<{ login: LoginResult }>(
    LOGIN_MUTATION,
  );

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setSubmitError(null);

    try {
      const result = await login({
        variables: { input: { email, password } },
      });
      const data = result.data?.login;
      if (!data) {
        setSubmitError("Login failed. Please try again.");
        return;
      }
      window.localStorage.setItem("serenauth.token", data.token);
      window.localStorage.setItem(
        "serenauth.user",
        JSON.stringify({
          email: data.email,
          displayName: data.displayName,
          role: data.role,
          organizationId: data.organizationId,
        }),
      );
      router.push("/dashboard");
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Sign in failed. Try again.";
      setSubmitError(message);
    }
  }

  return (
    <div className="mx-auto max-w-md px-6 py-16">
      <h1 className="text-2xl font-semibold tracking-tight text-slate-900">
        Sign in to SerenAuth
      </h1>
      <p className="mt-1 text-sm text-slate-600">
        Use one of the seeded demo accounts below.
      </p>

      <form onSubmit={onSubmit} noValidate className="mt-8 space-y-5">
        <label className="block space-y-1.5">
          <span className="text-sm font-medium text-slate-800">Email</span>
          <input
            type="email"
            required
            autoComplete="username"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className={inputClass}
            placeholder="clin@riverbend.example"
          />
        </label>
        <label className="block space-y-1.5">
          <span className="text-sm font-medium text-slate-800">Password</span>
          <input
            type="password"
            required
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className={inputClass}
            placeholder="ChangeMe!123"
          />
        </label>
        {submitError ? (
          <p className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-rose-700">
            {submitError}
          </p>
        ) : null}
        <button
          type="submit"
          disabled={loading}
          className="inline-flex w-full items-center justify-center rounded-lg bg-brand-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-brand-700 disabled:opacity-60"
        >
          {loading ? "Signing in…" : "Sign in"}
        </button>
      </form>

      <div className="mt-10 space-y-2 rounded-xl border border-slate-200 bg-slate-50 px-4 py-4 text-xs text-slate-600">
        <p className="font-semibold text-slate-800">
          Demo accounts (password: <code className="font-mono">ChangeMe!123</code>)
        </p>
        <ul className="space-y-1">
          {DEMO_USERS.map((u) => (
            <li key={u.email}>
              <button
                type="button"
                onClick={() => {
                  setEmail(u.email);
                  setPassword("ChangeMe!123");
                }}
                className="font-mono text-slate-700 hover:text-brand-700"
              >
                {u.label}: {u.email}
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

const inputClass =
  "w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 placeholder:text-slate-400 focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600";
