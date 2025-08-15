"use client";
import { useState } from "react";
import { api } from "@/lib/api";
import { VerifiedGuard } from "@/lib/guards";

interface ContactCreateDto {
  fullName: string;
  email?: string;
  phone?: string;
}

export default function NewContactPage() {
  const [form, setForm] = useState<ContactCreateDto>({ fullName: "" });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successId, setSuccessId] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSaving(true);
    setSuccessId(null);
    try {
      const created = await api<{ id: string }>("/api/contacts", {
        method: "POST",
        json: form,
      });
      setSuccessId(created.id);
      setForm({ fullName: "" });
    } catch (e: any) {
      const msg = e?.message || "Failed to create contact";
      setError(msg);
    } finally {
      setSaving(false);
    }
  }

  return (
    <VerifiedGuard>
      <div className="max-w-md p-6 space-y-4">
      <h1 className="text-2xl font-bold">New Contact</h1>
      {error && (
        <div className="rounded border border-red-300 bg-red-50 p-3 text-red-700 text-sm">
          {error}
          {(error.includes("401") || error.includes("403")) && (
            <div className="mt-2">
              You must verify your email before creating contacts. Please check your inbox for a verification link.
            </div>
          )}
        </div>
      )}
      {successId && (
        <div className="rounded border border-green-300 bg-green-50 p-3 text-green-800 text-sm">
          Contact created. ID: <b>{successId}</b>
        </div>
      )}
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
          placeholder="Email (optional)"
          value={form.email ?? ""}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
        />
        <input
          className="w-full border rounded-lg p-2"
          placeholder="Phone (optional)"
          value={form.phone ?? ""}
          onChange={(e) => setForm({ ...form, phone: e.target.value })}
        />
        <button disabled={saving} className="w-full rounded-xl p-2 bg-black text-white disabled:opacity-60">
          {saving ? "Saving…" : "Create Contact"}
        </button>
      </form>
      </div>
    </VerifiedGuard>
  );
}
