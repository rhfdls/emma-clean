"use client";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import PageContainer from "@/components/ui/PageContainer";
import { Card, CardContent, CardHeader } from "@/components/ui/Card";
import Button from "@/components/ui/Button";
import { apiGet } from "@/lib/api";
import { toastProblem } from "@/lib/toast-problem";
import { getOrgIdFromJwt } from "@/lib/jwt";

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
  const [showNoOrgCard, setShowNoOrgCard] = useState(false);

  useEffect(() => {
    const id = getOrgIdFromJwt();
    if (id) setOrgId(id);
    else setShowNoOrgCard(true);
  }, []);

  useEffect(() => {
    async function load() {
      if (!orgId) {
        setLoading(false);
        toastProblem({
          title: "Missing org context",
          status: 400,
          type: "/problems/validation-error",
          detail: "Save a dev token on /dev-token.",
        } as any);
        setShowNoOrgCard(true);
        return;
      }
      setLoading(true);
      setError(null);
      try {
        const data = await apiGet<ContactItem[]>(`/api/Contact?orgId=${orgId}`);
        setItems(data || []);
      } catch (e: any) {
        setError("Failed to load contacts");
        toastProblem(e);
        try {
          if (e && typeof e === "object") {
            const { traceId, title, detail, status } = e as any;
            // Helpful in dev to correlate with server logs
            console.debug("Contacts load error", { traceId, title, detail, status });
          }
        } catch {}
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
            {showNoOrgCard ? (
              <div className="rounded-md border p-4 bg-amber-50 text-amber-800 text-sm">
                Missing org context. Save a dev token.
                <div className="mt-3">
                  <Link href="/dev-token" className="inline-flex items-center rounded-md bg-amber-700 px-3 py-1.5 text-white hover:bg-amber-800">Go to /dev-token</Link>
                </div>
              </div>
            ) : loading ? (
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
