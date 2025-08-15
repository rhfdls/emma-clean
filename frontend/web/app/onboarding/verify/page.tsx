"use client";
import { useEffect, useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { useSession } from "@/context/SessionContext";

export default function VerifyPage() {
  const sp = useSearchParams();
  const router = useRouter();
  const token = sp.get("token");
  const { session, setSession } = useSession();
  const [status, setStatus] = useState<"pending"|"ok"|"error">("pending");
  const [message, setMessage] = useState<string>("Verifying…");

  useEffect(() => {
    (async () => {
      if (!token) { setStatus("error"); setMessage("Missing token"); return; }
      try {
        await api("/api/auth/verify-email", { method: "POST", json: { token } });
        setSession({ ...session, isVerified: true });
        setStatus("ok"); setMessage("Email verified! Redirecting…");
        setTimeout(() => router.push("/contacts/new"), 800);
      } catch (e: any) {
        setStatus("error"); setMessage(e.message ?? "Verification failed");
      }
    })();
  }, [token, router]);

  return (
    <div className="p-6">
      <h1 className="text-xl font-semibold">{status === "pending" ? "Verifying…" : "Verification"}</h1>
      <p className={status==="error" ? "text-red-600" : ""}>{message}</p>
    </div>
  );
}
