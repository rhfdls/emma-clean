"use client";
import React, { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useSession } from "@/context/SessionContext";

export function VerifiedGuard({ children }: { children: React.ReactNode }) {
  const { session } = useSession();
  const router = useRouter();

  useEffect(() => {
    if (!session?.isVerified) {
      router.replace("/verify-required");
    }
  }, [session?.isVerified, router]);

  if (session?.isVerified) return <>{children}</>;
  // Fallback while redirecting
  return null;
}
