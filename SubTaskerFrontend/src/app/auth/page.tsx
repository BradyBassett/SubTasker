export default function AuthPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-[#f5f7f2] px-4 py-8 text-[#1e2a2a] sm:px-6 sm:py-12">
      <section className="w-full max-w-md rounded-lg bg-white p-6 shadow-sm sm:p-10" aria-labelledby="login-heading">
        <div>
          <div>
            <h1 id="login-heading" className="text-2xl font-semibold tracking-tight sm:text-3xl">
              Sign in to SubTasker
            </h1>
            <p className="mt-3 text-[#687776]">Enter your account details to continue.</p>
          </div>

          <form className="mt-8 grid gap-6 sm:mt-12">
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
              <div className="flex items-center justify-between gap-4">
                <label className="text-xs font-bold" htmlFor="password">Password</label>
                <a className="text-xs font-bold text-[#166b67] hover:underline" href="#forgot-password">
                  Forgot password?
                </a>
              </div>
              <input
                className="w-full rounded border border-[#d9e1dc] bg-[#fbfcfa] px-4 py-3.5 outline-none transition focus:border-[#166b67] focus:ring-4 focus:ring-[#166b67]/15"
                id="password"
                name="password"
                type="password"
                autoComplete="current-password"
                placeholder="Enter your password"
                minLength={6}
                required
              />
            </div>

            <button className="flex items-center justify-between rounded bg-[#166b67] px-5 py-4 font-bold text-white transition hover:-translate-y-px hover:bg-[#0f504d]" type="submit">
              <span>Log in</span>
              <span aria-hidden="true" className="text-xl font-normal">&rarr;</span>
            </button>
          </form>

          <div className="mt-8 flex flex-wrap justify-center gap-x-1 gap-y-0.5 text-center text-sm text-[#687776]">
            <span>New to SubTasker?</span>
            <a className="font-bold text-[#166b67] hover:underline" href="/auth/register">
              Create new user
            </a>
          </div>
        </div>
      </section>
    </main>
  );
}