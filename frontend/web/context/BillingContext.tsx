"use client";
// SPRINT1: BillingContext scaffold (no Stripe yet)
import React, { createContext, useContext, useMemo, useState } from "react";

export type PlanTier = "free" | "pro" | "business";

export interface BillingState {
  plan: PlanTier;
  seats: number;
  setPlan: (plan: PlanTier) => void;
  setSeats: (seats: number) => void;
}

const BillingContext = createContext<BillingState | null>(null);

export function BillingProvider({ children }: { children: React.ReactNode }) {
  const [plan, setPlan] = useState<PlanTier>("free");
  const [seats, setSeats] = useState<number>(1);

  const value = useMemo(() => ({ plan, seats, setPlan, setSeats }), [plan, seats]);

  return <BillingContext.Provider value={value}>{children}</BillingContext.Provider>;
}

export function useBilling() {
  const ctx = useContext(BillingContext);
  if (!ctx) throw new Error("useBilling must be used within BillingProvider");
  return ctx;
}
