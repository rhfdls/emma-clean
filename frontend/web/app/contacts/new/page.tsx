"use client";
import { useState } from "react";
import { api } from "@/lib/api";
import { VerifiedGuard } from "@/lib/guards";
import PageContainer from "@/components/ui/PageContainer";
import { Card, CardContent } from "@/components/ui/Card";
import Input from "@/components/ui/Input";
import Label from "@/components/ui/Label";
import Button from "@/components/ui/Button";

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
      <main className="min-h-dvh bg-neutral-50">
        <PageContainer>
          <Card>
            <CardContent className="p-6 max-w-md">
              <h1 className="text-2xl font-bold text-gray-900">New Contact</h1>
              {error && (
                <div className="mt-4 rounded-md border border-red-300 bg-red-50 p-3 text-red-700 text-sm">
                  {error}
                  {(error.includes("401") || error.includes("403")) && (
                    <div className="mt-2">
                      You must verify your email before creating contacts. Please check your inbox for a verification link.
                    </div>
                  )}
                </div>
              )}
              {successId && (
                <div className="mt-4 rounded-md border border-green-300 bg-green-50 p-3 text-green-800 text-sm">
                  Contact created. ID: <b>{successId}</b>
                </div>
              )}
              <form onSubmit={onSubmit} className="mt-4 space-y-4">
                <div>
                  <Label htmlFor="fullName">Full name</Label>
                  <Input
                    id="fullName"
                    placeholder="Full name"
                    value={form.fullName}
                    onChange={(e) => setForm({ ...form, fullName: e.target.value })}
                    required
                  />
                </div>
                <div>
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    placeholder="Email (optional)"
                    value={form.email ?? ""}
                    onChange={(e) => setForm({ ...form, email: e.target.value })}
                  />
                </div>
                <div>
                  <Label htmlFor="phone">Phone</Label>
                  <Input
                    id="phone"
                    placeholder="Phone (optional)"
                    value={form.phone ?? ""}
                    onChange={(e) => setForm({ ...form, phone: e.target.value })}
                  />
                </div>
                <Button type="submit" disabled={saving} className="w-full">
                  {saving ? "Saving…" : "Create Contact"}
                </Button>
              </form>
            </CardContent>
          </Card>
        </PageContainer>
      </main>
    </VerifiedGuard>
  );
}
