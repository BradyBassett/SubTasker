import type { ReactNode } from "react";

type AuthShellProps = {
  headingId: string;
  title: string;
  description: string;
  children: ReactNode;
  footer: ReactNode;
};

export function AuthShell({ headingId, title, description, children, footer }: AuthShellProps) {
  return (
    <main className="flex min-h-screen items-center justify-center bg-[#f5f7f2] px-4 py-8 text-[#1e2a2a] sm:px-6 sm:py-12">
      <section className="w-full max-w-md rounded-lg bg-white p-6 shadow-sm sm:p-10" aria-labelledby={headingId}>
        <h1 id={headingId} className="text-2xl font-semibold tracking-tight sm:text-3xl">
          {title}
        </h1>
        <p className="mt-3 text-[#687776]">{description}</p>
        {children}
        {footer}
      </section>
    </main>
  );
}