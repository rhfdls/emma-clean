"use client";
import { useEffect, useState } from "react";
import { useRouter, useParams } from "next/navigation";
import { getInvite, registerViaInvite } from "@/lib/invitationsApi";
import { toast } from "sonner";
import { toastProblem } from "@/lib/toast-problem";

type InvitationView = {
  orgId: string;
  email?: string;
  role?: string;
  status?: string;
};

export default function JoinByTokenPage() {
  const router = useRouter();
  const params = useParams<{ token: string }>();
  const token = params.token;
  const [invite, setInvite] = useState<InvitationView | null>(null);
  const [loading, setLoading] = useState(true);
  const [firstName, setFirst] = useState("");
  const [lastName, setLast] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPw] = useState("");
  const [submitted, setSubmitted] = useState(false);

  useEffect(() => {
    let on = true;
    (async () => {
      try {
        const res = await getInvite(token);
        if (on) setInvite(res);
      } catch (e: any) {
        toastProblem(e);
      } finally {
        if (on) setLoading(false);
      }
    })();
    return () => { on = false; };
  }, [token]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    try {
      await registerViaInvite(token, { firstName, lastName, email: email || invite?.email || "", password });
      setSubmitted(true);
      toast.success("Registered. Please verify your email.");
      setTimeout(() => router.push("/onboarding/verify"), 800);
    } catch (e: any) {
      toastProblem(e);
    }
  }

  if (loading) return <div className="p-6">Loading invitation…</div>;
  if (!invite) return <div className="p-6">Invitation not found or expired.</div>;

  if (submitted) {
    return (
      <div className="p-6 space-y-2">
        <h1 className="text-xl font-semibold">Check your email</h1>
        <p>
          We sent a verification link to <b>{email || invite.email}</b>. After verifying, you can continue to the app.
        </p>
      </div>
    );
  }

  return (
    <div className="max-w-md p-6 space-y-4">
      <h1 className="text-2xl font-bold">Join organization</h1>
      <p className="text-sm opacity-80">Role: {invite.role ?? "Agent"}</p>
      <form onSubmit={onSubmit} className="space-y-3">
        <div className="grid gap-3 sm:grid-cols-2">
          <input
            className="w-full border rounded-lg p-2"
            placeholder="First name"
            value={firstName}
            onChange={(e) => setFirst(e.target.value)}
            required
          />
          <input
            className="w-full border rounded-lg p-2"
            placeholder="Last name"
            value={lastName}
            onChange={(e) => setLast(e.target.value)}
            required
          />
        </div>
        <input
          className="w-full border rounded-lg p-2"
          type="email"
          placeholder="Email"
          value={email || invite.email || ""}
          onChange={(e) => setEmail(e.target.value)}
          required={!invite.email}
        />
        <input
          className="w-full border rounded-lg p-2"
          type="password"
          placeholder="Password"
          value={password}
          onChange={(e) => setPw(e.target.value)}
          required
        />
        <button className="w-full rounded-xl p-2 bg-black text-white">Create account</button>
      </form>
    </div>
  );
}
