"use client";
import { useEffect, useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { verifyEmail } from "@/lib/onboardingApi";
import { toastProblem } from "@/lib/toast-problem";
import Link from "next/link";

export default function VerifyPage() {
  const sp = useSearchParams();
  const token = sp.get("token") || "";
  const router = useRouter();
  const [status, setStatus] = useState<"checking" | "ok" | "err">("checking");

  useEffect(() => {
    let on = true;
    (async () => {
      if (!token) { setStatus("err"); return; }
      try {
        await verifyEmail(token);
        if (on) setStatus("ok");
        // Optionally redirect after a short delay
        setTimeout(() => router.push("/contacts"), 800);
      } catch (e: any) {
        toastProblem(e);
        if (on) setStatus("err");
      }
    })();
    return () => { on = false; };
  }, [token, router]);

  return (
    <div className="max-w-xl space-y-4">
      <h1 className="text-2xl font-semibold">Email verification</h1>
      {status === "checking" && <div className="rounded-lg border p-3 animate-pulse">Verifying…</div>}
      {status === "ok" && (
        <div className="rounded-lg border p-4 bg-white">
          <p className="mb-2">Your email is verified. Redirecting…</p>
          <Link className="underline" href="/contacts">Go to contacts</Link>
        </div>
      )}
      {status === "err" && (
        <div className="rounded-lg border p-4 bg-white">
          <p className="mb-2">Verification failed. Check the link or request a new one.</p>
          <Link className="underline" href="/onboarding/register">Back to registration</Link>
        </div>
      )}
    </div>
  );
}
