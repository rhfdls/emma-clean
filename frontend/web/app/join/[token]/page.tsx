"use client";
import { useEffect, useState } from "react";
import { useRouter, useParams } from "next/navigation";
import { api } from "@/lib/api";

type InvitationView = {
  organizationId: string;
  organizationName: string;
  email?: string;
  role?: string;
  token: string;
  expiresAt?: string;
};

export default function JoinByTokenPage() {
  const router = useRouter();
  const params = useParams<{ token: string }>();
  const token = params.token;
  const [invite, setInvite] = useState<InvitationView | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({ email: "", password: "", fullName: "" });
  const [submitted, setSubmitted] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const data = await api<InvitationView>(`/api/organization/invitations/${token}`);
        setInvite(data);
      } catch (e: any) {
        setError(e.message ?? "Failed to load invitation");
      } finally {
        setLoading(false);
      }
    })();
  }, [token]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await api(`/api/organization/invitations/${token}/register`, {
        method: "POST",
        json: {
          email: form.email || invite?.email,
          password: form.password,
          fullName: form.fullName,
        },
      });
      setSubmitted(true);
    } catch (e: any) {
      setError(e.message ?? "Registration failed");
    }
  }

  if (loading) return <div className="p-6">Loading invitation…</div>;
  if (error) return <div className="p-6 text-red-600">Error: {error}</div>;
  if (!invite) return <div className="p-6">Invitation not found.</div>;

  if (submitted) {
    return (
      <div className="p-6 space-y-2">
        <h1 className="text-xl font-semibold">Check your email</h1>
        <p>
          We sent a verification link to <b>{form.email || invite.email}</b>. After verifying,
          you can continue to the app.
        </p>
      </div>
    );
  }

  return (
    <div className="max-w-md p-6 space-y-4">
      <h1 className="text-2xl font-bold">Join {invite.organizationName}</h1>
      <p className="text-sm opacity-80">Role: {invite.role ?? "Member"}</p>
      <form onSubmit={onSubmit} className="space-y-3">
        <input
          className="w-full border rounded-lg p-2"
          placeholder="Full name"
          value={form.fullName}
          onChange={(e) => setForm({ ...form, fullName: e.target.value })}
          required
        />
        <input
          className="w-full border rounded-lg p-2"
          type="email"
          placeholder="Email"
          value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
          required={!invite.email}
        />
        <input
          className="w-full border rounded-lg p-2"
          type="password"
          placeholder="Password"
          value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
          required
        />
        <button className="w-full rounded-xl p-2 bg-black text-white">Create account</button>
      </form>
    </div>
  );
}
