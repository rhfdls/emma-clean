"use client";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import PageContainer from "@/components/ui/PageContainer";
import { Card, CardContent, CardHeader } from "@/components/ui/Card";
import Button from "@/components/ui/Button";
import { api } from "@/lib/api";

interface ContactItem {
  id: string;
  organizationId: string;
  firstName?: string | null;
  lastName?: string | null;
  company?: string | null;
}

export default function ContactsIndexPage() {
  const [items, setItems] = useState<ContactItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [orgId, setOrgId] = useState<string>("");

  useEffect(() => {
    // Try to decode orgId from dev token payload for convenience
    const raw = typeof window !== "undefined" ? localStorage.getItem("emma_dev_token") : null;
    if (!raw) return;
    try {
      const [, payloadB64] = raw.split(".");
      const payload = JSON.parse(atob(payloadB64));
      if (payload?.orgId) setOrgId(payload.orgId);
    } catch {}
  }, []);

  useEffect(() => {
    async function load() {
      if (!orgId) return;
      setLoading(true);
      setError(null);
      try {
        const data = await api<ContactItem[]>(`/api/contact?orgId=${orgId}`, { method: "GET" });
        setItems(data);
      } catch (e: any) {
        setError(e?.message || "Failed to load contacts");
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [orgId]);

  return (
    <main className="min-h-dvh bg-neutral-50">
      <PageContainer className="py-6">
        <div className="mb-4 flex items-center justify-between">
          <h1 className="text-xl font-semibold text-gray-900">Contacts</h1>
          <Link href="/contacts/new" className="rounded-md bg-blue-600 px-3 py-1.5 text-white hover:bg-blue-700">New contact</Link>
        </div>
        <Card>
          <CardHeader>
            <div className="text-sm text-gray-600">Org: {orgId || "(set via dev token)"}</div>
          </CardHeader>
          <CardContent>
            {loading ? (
              <p className="text-sm text-gray-600">Loading…</p>
            ) : error ? (
              <div className="rounded-md border border-red-300 bg-red-50 p-3 text-sm text-red-700">{error}</div>
            ) : items.length === 0 ? (
              <p className="text-sm text-gray-700">No contacts found. Create one or set a dev token.</p>
            ) : (
              <ul className="divide-y divide-gray-200 rounded-md border border-gray-200 bg-white">
                {items.map((c) => (
                  <li key={c.id} className="flex items-center justify-between p-3">
                    <div className="text-sm text-gray-900">
                      <div className="font-medium">
                        {c.firstName || "Unnamed"} {c.lastName || ""}
                      </div>
                      {c.company && <div className="text-gray-600">{c.company}</div>}
                    </div>
                    <Link href={`/contacts/${c.id}`} className="text-sm font-medium text-blue-700 hover:underline">Open</Link>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      </PageContainer>
    </main>
  );
}
