"use client";
import React, { createContext, useContext, useMemo, useState } from "react";

export type OrgInfo = {
  orgId?: string;
  orgName?: string;
  planId?: string; // 'free' | 'pro' | 'business'
};

type OrgCtx = {
  org: OrgInfo;
  setOrg: (o: OrgInfo) => void;
};

const Ctx = createContext<OrgCtx | null>(null);

export function OrgProvider({ children }: { children: React.ReactNode }) {
  const [org, setOrg] = useState<OrgInfo>({ planId: "free" });
  const value = useMemo(() => ({ org, setOrg }), [org]);
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useOrg() {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error("useOrg must be used within OrgProvider");
  return ctx;
}
