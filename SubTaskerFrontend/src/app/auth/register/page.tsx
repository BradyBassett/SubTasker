export default function RegisterPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-[#f5f7f2] px-4 py-8 text-[#1e2a2a] sm:px-6 sm:py-12">
      <section className="w-full max-w-md rounded-lg bg-white p-6 shadow-sm sm:p-10" aria-labelledby="register-heading">
        <div>
          <h1 id="register-heading" className="text-2xl font-semibold tracking-tight sm:text-3xl">
            Create your SubTasker account
          </h1>
          <p className="mt-3 text-[#687776]">Set up your account to start organizing your tasks.</p>
        </div>

        <form className="mt-10 grid gap-5">
          <div className="grid gap-2">
            <label className="text-xs font-bold" htmlFor="username">Username</label>
            <input
              className="w-full rounded border border-[#d9e1dc] bg-[#fbfcfa] px-4 py-3.5 outline-none transition focus:border-[#166b67] focus:ring-4 focus:ring-[#166b67]/15"
              id="username"
              name="username"
              type="text"
              autoComplete="username"
              placeholder="Choose a username"
              required
            />
          </div>

          <div className="grid gap-2">
            <label className="text-xs font-bold" htmlFor="email">Email address</label>
            <input
              className="w-full rounded border border-[#d9e1dc] bg-[#fbfcfa] px-4 py-3.5 outline-none transition focus:border-[#166b67] focus:ring-4 focus:ring-[#166b67]/15"
              id="email"
              name="email"
              type="email"
              autoComplete="email"
              placeholder="you@example.com"
              required
            />
          </div>

          <div className="grid gap-2">
            <label className="text-xs font-bold" htmlFor="password">Password</label>
            <input
              className="w-full rounded border border-[#d9e1dc] bg-[#fbfcfa] px-4 py-3.5 outline-none transition focus:border-[#166b67] focus:ring-4 focus:ring-[#166b67]/15"
              id="password"
              name="password"
              type="password"
              autoComplete="new-password"
              placeholder="Create a password"
              minLength={6}
              required
            />
          </div>

          <div className="grid gap-2">
            <label className="text-xs font-bold" htmlFor="confirm-password">Confirm password</label>
            <input
              className="w-full rounded border border-[#d9e1dc] bg-[#fbfcfa] px-4 py-3.5 outline-none transition focus:border-[#166b67] focus:ring-4 focus:ring-[#166b67]/15"
              id="confirm-password"
              name="confirmPassword"
              type="password"
              autoComplete="new-password"
              placeholder="Re-enter your password"
              minLength={6}
              required
            />
          </div>

          <button className="mt-2 flex items-center justify-between rounded bg-[#166b67] px-5 py-4 font-bold text-white transition hover:-translate-y-px hover:bg-[#0f504d]" type="submit">
            <span>Create account</span>
            <span aria-hidden="true" className="text-xl font-normal">&rarr;</span>
          </button>
        </form>

        <p className="mt-8 text-center text-sm text-[#687776]">
          Already have an account? <a className="font-bold text-[#166b67] hover:underline" href="/auth">Sign in</a>
        </p>
      </section>
    </main>
  );
}