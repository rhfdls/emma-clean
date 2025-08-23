"use client";
import { useState } from "react";
import { api } from "@/lib/api";
import { VerifiedGuard } from "@/lib/guards";
import PageContainer from "@/components/ui/PageContainer";
import { Card, CardContent } from "@/components/ui/Card";
import Input from "@/components/ui/Input";
import Label from "@/components/ui/Label";
import Button from "@/components/ui/Button";
import { useOrg } from "@/context/OrgContext";

interface ContactCreateForm {
  firstName: string;
  lastName: string;
}

export default function NewContactPage() {
  const { org } = useOrg();
  const [form, setForm] = useState<ContactCreateForm>({ firstName: "", lastName: "" });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successId, setSuccessId] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSaving(true);
    setSuccessId(null);
    try {
      if (!org.orgId) throw new Error("Missing orgId in context");
      const payload = {
        organizationId: org.orgId,
        firstName: form.firstName,
        lastName: form.lastName,
      };
      const created = await api<{ id: string }>("/api/contact", {
        method: "POST",
        json: payload,
      });
      setSuccessId(created.id);
      setForm({ firstName: "", lastName: "" });
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
                </div>
              )}
              {successId && (
                <div className="mt-4 rounded-md border border-green-300 bg-green-50 p-3 text-green-800 text-sm">
                  Contact created. ID: <b>{successId}</b>
                </div>
              )}
              <form onSubmit={onSubmit} className="mt-4 space-y-4">
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <Label htmlFor="firstName">First name</Label>
                    <Input
                      id="firstName"
                      placeholder="First name"
                      value={form.firstName}
                      onChange={(e) => setForm({ ...form, firstName: e.target.value })}
                      required
                    />
                  </div>
                  <div>
                    <Label htmlFor="lastName">Last name</Label>
                    <Input
                      id="lastName"
                      placeholder="Last name"
                      value={form.lastName}
                      onChange={(e) => setForm({ ...form, lastName: e.target.value })}
                      required
                    />
                  </div>
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
