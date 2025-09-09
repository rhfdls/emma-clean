"use client";

import { useState } from "react";
import { getOrgIdFromJwt } from "@/lib/jwt";
import { createInvite } from "@/lib/invitationsApi";
import { toast } from "sonner";
import { toastProblem } from "@/lib/toast-problem";

export default function NewInvitePage() {
  const orgId = getOrgIdFromJwt();
  const [email, setEmail] = useState("");
  const [role, setRole] = useState("Agent");
  const [token, setToken] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!orgId) {
      toast.error("Missing org context. Save a dev token on /dev-token.");
      return;
    }
    setPending(true);
    try {
      const res = await createInvite(orgId, email, role);
      setToken(res.token);
      toast.success("Invitation created");
    } catch (e: any) {
      toastProblem(e);
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="max-w-xl space-y-6">
      <header>
        <h1 className="text-2xl font-semibold">Invite a teammate</h1>
        <p className="text-sm text-muted-foreground">Owner/Admin only. Invitee will register and verify.</p>
      </header>

      <form onSubmit={onSubmit} className="grid gap-3">
        <label className="grid gap-1">
          <span className="text-sm font-medium">Email</span>
          <input type="email" className="border rounded-lg px-3 py-2" required value={email} onChange={(e)=>setEmail(e.target.value)} />
        </label>
        <label className="grid gap-1">
          <span className="text-sm font-medium">Role</span>
          <select className="border rounded-lg px-3 py-2" value={role} onChange={(e)=>setRole(e.target.value)}>
            <option>Agent</option>
            <option>Admin</option>
          </select>
        </label>
        <button disabled={pending} className="rounded-lg bg-black text-white px-4 py-2">{pending ? "Creating…" : "Create invitation"}</button>
      </form>

      {token && (
        <div className="rounded-lg border p-4 bg-white">
          <p className="text-sm">Dev note: share this tokenized link with your teammate:</p>
          <pre className="mt-2 rounded bg-gray-50 p-2 text-xs whitespace-pre-wrap break-all">{`/join/${token}`}</pre>
        </div>
      )}
    </div>
  );
}
