"use client";

import { Suspense, useState } from "react";
import type { SubmitEvent } from "react";
import { useRouter } from "next/navigation";
import { useSearchParams } from "next/navigation";
import { login } from "@/lib/auth-api";
import { AuthField } from "@/components/auth/AuthField";
import { AuthShell } from "@/components/auth/AuthShell";
import { AuthSubmitButton } from "@/components/auth/AuthSubmitButton";

export default function AuthPage() {
  return (
    <Suspense fallback={<main className="min-h-screen bg-[#f5f7f2]" />}>
      <AuthForm />
    </Suspense>
  );
}

function AuthForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    const form = event.currentTarget;
    const formData = new FormData(form);

    try {
      const response = await login({
        email: String(formData.get("email")),
        password: String(formData.get("password")),
      });

      localStorage.setItem("subtasker_token", response.token);
      form.reset();
      router.replace("/");
    } catch (submissionError) {
      setError(submissionError instanceof Error ? submissionError.message : "Unable to sign in.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
      <AuthShell
        headingId="login-heading"
        title="Sign in to SubTasker"
        description="Enter your account details to continue."
        footer={
          <div className="mt-8 flex flex-wrap justify-center gap-x-1 gap-y-0.5 text-center text-sm text-[#687776]">
            <span>New to SubTasker?</span>
            <a className="cursor-pointer font-bold text-[#166b67] hover:underline" href="/auth/register">Create new user</a>
          </div>
        }
      >
        {searchParams.get("registered") === "true" && (
          <p className="mt-5 rounded border border-[#b7d8c8] bg-[#eef9f2] px-4 py-3 text-sm text-[#166b67]" role="status">
            Your user was created successfully. You can now log in.
          </p>
        )}
        <form className="mt-8 grid gap-6 sm:mt-12" onSubmit={handleSubmit}>
          <AuthField id="email" name="email" label="Email address" type="email" autoComplete="email" placeholder="you@example.com" required />
          <AuthField
            id="password"
            name="password"
            label="Password"
            type="password"
            autoComplete="current-password"
            placeholder="Enter your password"
            minLength={6}
            required
            trailingContent={<a className="text-xs font-bold text-[#166b67] hover:underline" href="#forgot-password">Forgot password?</a>}
          />
          <AuthSubmitButton isSubmitting={isSubmitting} label="Log in" submittingLabel="Signing in..." />
        </form>
        {error && <p className="mt-4 text-sm text-red-700" role="alert">{error}</p>}
      </AuthShell>
  );
}
