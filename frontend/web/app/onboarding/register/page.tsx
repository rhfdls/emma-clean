"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { registerOwner } from "@/lib/onboardingApi";
import { toast } from "sonner";
import { toastProblem } from "@/lib/toast-problem";

export default function RegisterOwnerPage() {
  const [pending, setPending] = useState(false);
  const [orgName, setOrg] = useState("");
  const [firstName, setFirst] = useState("");
  const [lastName, setLast] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPw] = useState("");
  const router = useRouter();

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setPending(true);
    try {
      const res = await registerOwner({ orgName, firstName, lastName, email, password });
      toast.success("Registered. Check email to verify.");
      if (res?.verificationToken) {
        router.push(`/onboarding/verify?token=${encodeURIComponent(res.verificationToken)}`);
      }
    } catch (e: any) {
      toastProblem(e);
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="space-y-6 max-w-xl">
      <header>
        <h1 className="text-2xl font-semibold">Create your organization</h1>
        <p className="text-sm text-muted-foreground">Register the owner account and org.</p>
      </header>

      <form onSubmit={onSubmit} className="grid gap-3">
        <label className="grid gap-1">
          <span className="text-sm font-medium">Organization name</span>
          <input className="border rounded-lg px-3 py-2" required value={orgName} onChange={(e)=>setOrg(e.target.value)} />
        </label>
        <div className="grid gap-3 sm:grid-cols-2">
          <label className="grid gap-1">
            <span className="text-sm font-medium">First name</span>
            <input className="border rounded-lg px-3 py-2" required value={firstName} onChange={(e)=>setFirst(e.target.value)} />
          </label>
          <label className="grid gap-1">
            <span className="text-sm font-medium">Last name</span>
            <input className="border rounded-lg px-3 py-2" required value={lastName} onChange={(e)=>setLast(e.target.value)} />
          </label>
        </div>
        <label className="grid gap-1">
          <span className="text-sm font-medium">Email</span>
          <input type="email" className="border rounded-lg px-3 py-2" required value={email} onChange={(e)=>setEmail(e.target.value)} />
        </label>
        <label className="grid gap-1">
          <span className="text-sm font-medium">Password</span>
          <input type="password" className="border rounded-lg px-3 py-2" required value={password} onChange={(e)=>setPw(e.target.value)} />
        </label>
        <div className="flex gap-3">
          <button disabled={pending} className="rounded-lg bg-black text-white px-4 py-2">{pending ? "Registering…" : "Register"}</button>
        </div>
      </form>
    </div>
  );
}
