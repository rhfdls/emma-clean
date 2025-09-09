"use client";
import { useEffect, useState } from "react";
import { Toaster } from "sonner";
import { API_URL } from "@/lib/api";

export default function ClientBoot() {
  const [apiWarning, setApiWarning] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function checkApi() {
      try {
        const res = await fetch(`${API_URL}/api/health/live`, { cache: "no-store" });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        if (!cancelled) setApiWarning(null);
      } catch (e: any) {
        if (!cancelled) setApiWarning(`API not reachable at ${API_URL}. Check NEXT_PUBLIC_API_URL and that the API is running.`);
      }
    }
    checkApi();
  }, []);

  return (
    <>
      <Toaster richColors position="top-right" />
      {apiWarning && (
        <div className="w-full bg-amber-100 text-amber-900 text-sm px-4 py-2 border-b border-amber-200">
          {apiWarning}
        </div>
      )}
    </>
  );
}
