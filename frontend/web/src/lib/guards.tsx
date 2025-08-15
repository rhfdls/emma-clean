"use client";
import React from "react";
import { useRouter } from "next/navigation";
import { useSession } from "@/context/SessionContext";

export function VerifiedGuard({ children }: { children: React.ReactNode }) {
  const { session } = useSession();
  const router = useRouter();

  if (session.isVerified) return <>{children}</>;

  // Soft-redirect for unverified users
  if (typeof window !== "undefined") {
    router.replace("/verify-required");
  }
  return null;
}
