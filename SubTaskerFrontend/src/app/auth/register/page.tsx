"use client";

import { useState } from "react";
import type { SubmitEvent } from "react";
import { useRouter } from "next/navigation";
import { register } from "@/lib/auth-api";
import { AuthField } from "@/components/auth/AuthField";
import { AuthShell } from "@/components/auth/AuthShell";
import { AuthSubmitButton } from "@/components/auth/AuthSubmitButton";

export default function RegisterPage() {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    const form = event.currentTarget;
    const formData = new FormData(form);

    try {
      await register({
        username: String(formData.get("username")),
        email: String(formData.get("email")),
        password: String(formData.get("password")),
        confirmPassword: String(formData.get("confirmPassword")),
      });

      form.reset();
      router.replace("/auth?registered=true");
    } catch (submissionError) {
      setError(submissionError instanceof Error ? submissionError.message : "Unable to create your account.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthShell
      headingId="register-heading"
      title="Create your SubTasker account"
      description="Set up your account to start organizing your tasks."
      footer={<p className="mt-8 text-center text-sm text-[#687776]">Already have an account? <a className="font-bold text-[#166b67] hover:underline" href="/auth">Sign in</a></p>}
    >
      <form className="mt-10 grid gap-5" onSubmit={handleSubmit}>
        <AuthField id="username" name="username" label="Username" type="text" autoComplete="username" placeholder="Choose a username" required />
        <AuthField id="email" name="email" label="Email address" type="email" autoComplete="email" placeholder="you@example.com" required />
        <AuthField id="password" name="password" label="Password" type="password" autoComplete="new-password" placeholder="Create a password" minLength={6} required />
        <AuthField id="confirm-password" name="confirmPassword" label="Confirm password" type="password" autoComplete="new-password" placeholder="Re-enter your password" minLength={6} required />
        <AuthSubmitButton isSubmitting={isSubmitting} label="Create account" submittingLabel="Creating account..." />
      </form>
      {error && <p className="mt-4 text-sm text-red-700" role="alert">{error}</p>}
    </AuthShell>
  );
}