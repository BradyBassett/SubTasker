"use client";

import { useRouter } from "next/navigation";
import { useEffect, useSyncExternalStore } from "react";

const subscribeToStorage = (onStoreChange: () => void) => {
  window.addEventListener("storage", onStoreChange);
  return () => window.removeEventListener("storage", onStoreChange);
};

const getTokenSnapshot = () => localStorage.getItem("subtasker_token") !== null;
const getServerTokenSnapshot = () => false;

export default function Home() {
  const router = useRouter();
  const hasToken = useSyncExternalStore(
    subscribeToStorage,
    getTokenSnapshot,
    getServerTokenSnapshot,
  );

  useEffect(() => {
    if (!hasToken) {
      router.replace("/auth");
    }
  }, [hasToken, router]);

  if (!hasToken) {
    return <main className="min-h-screen bg-[#f5f7f2]" />;
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-[#f5f7f2] px-4 py-8 text-[#1e2a2a]">
      <section className="w-full max-w-2xl rounded-lg bg-white p-8 shadow-sm sm:p-10">
        <h1 className="text-3xl font-semibold tracking-tight">Welcome to SubTasker</h1>
        <p className="mt-3 text-[#687776]">Your authenticated task workspace will appear here.</p>
      </section>
    </main>
  );
}
