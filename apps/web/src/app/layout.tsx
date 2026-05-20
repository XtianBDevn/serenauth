import type { Metadata } from "next";
import Link from "next/link";
import "./globals.css";

export const metadata: Metadata = {
  title: "SerenAuth — Calm authorization. Faster care.",
  description:
    "Prior authorization software built specifically for dialysis clinics.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body className="font-sans antialiased">
        <header className="border-b border-slate-200 bg-white">
          <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
            <Link href="/" className="flex items-center gap-2">
              <span className="inline-flex h-8 w-8 items-center justify-center rounded-lg bg-brand-600 text-sm font-semibold text-white">
                SA
              </span>
              <span className="text-sm font-semibold tracking-tight text-slate-900">
                SerenAuth
              </span>
            </Link>
            <nav className="flex items-center gap-6 text-sm text-slate-700">
              <Link className="hover:text-brand-700" href="/dialysis">
                Dialysis
              </Link>
              <Link className="hover:text-brand-700" href="/demo">
                Request demo
              </Link>
              <Link
                className="rounded-lg border border-slate-200 px-3 py-1.5 text-slate-800 hover:border-brand-600 hover:text-brand-700"
                href="/dashboard"
              >
                Sign in
              </Link>
            </nav>
          </div>
        </header>
        <main>{children}</main>
        <footer className="mt-24 border-t border-slate-200 bg-white">
          <div className="mx-auto max-w-6xl px-6 py-8 text-xs text-slate-500">
            © {new Date().getFullYear()} SerenAuth. HIPAA-conscious by design.
          </div>
        </footer>
      </body>
    </html>
  );
}
