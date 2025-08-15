"use client";
import React, { createContext, useContext, useMemo, useState } from "react";

export type Session = {
  userId?: string;
  email?: string;
  isVerified?: boolean;
};

type SessionCtx = {
  session: Session;
  setSession: (s: Session) => void;
};

const Ctx = createContext<SessionCtx | null>(null);

export function SessionProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<Session>({});

  const value = useMemo(() => ({ session, setSession }), [session]);
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useSession() {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error("useSession must be used within SessionProvider");
  return ctx;
}
