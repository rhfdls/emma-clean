"use client";
import { useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { apiGet } from "@/lib/api";
import { toastProblem } from "@/lib/toast-problem";
import type { ContactReadDto } from "@/types/contact";
import InteractionsPanel from "@/components/interactions/InteractionsPanel";
import OwnerAssignment from "@/components/contacts/OwnerAssignment";

export default function ContactDetailPage() {
  const params = useParams<{ id: string }>();
  const contactId = useMemo(() => params?.id, [params]);

  const [contact, setContact] = useState<ContactReadDto | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    async function load() {
      if (!contactId) return;
      setLoading(true);
      try {
        const data = await apiGet<ContactReadDto>(`/api/Contact/${contactId}`);
        setContact(data);
      } catch (err: any) {
        toastProblem(err);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [contactId]);

  if (loading) return <p className="p-6 text-sm text-gray-600">Loading…</p>;
  if (!contact) return <p className="p-6 text-sm text-gray-600">No contact found.</p>;

  return (
    <div className="space-y-6">
      <header className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">{contact.name || `${contact.firstName ?? ""} ${contact.lastName ?? ""}`.trim()}</h1>
          <p className="text-sm text-muted-foreground">Contact detail</p>
        </div>
      </header>

      <section className="grid gap-4 md:grid-cols-3">
        <div className="rounded-lg border bg-white p-4 md:col-span-2">
          <h2 className="mb-3 text-base font-semibold">Profile</h2>
          <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div>
              <dt className="text-xs uppercase text-gray-500">First name</dt>
              <dd className="text-sm text-gray-900">{contact.firstName || "—"}</dd>
            </div>
            <div>
              <dt className="text-xs uppercase text-gray-500">Last name</dt>
              <dd className="text-sm text-gray-900">{contact.lastName || "—"}</dd>
            </div>
            <div>
              <dt className="text-xs uppercase text-gray-500">Email</dt>
              <dd className="text-sm text-gray-900">{contact.email || "—"}</dd>
            </div>
            <div>
              <dt className="text-xs uppercase text-gray-500">Phone</dt>
              <dd className="text-sm text-gray-900">{contact.phone || "—"}</dd>
            </div>
            <div className="sm:col-span-2">
              <dt className="text-xs uppercase text-gray-500">Tags</dt>
              <dd className="text-sm text-gray-900">{contact.tags?.join(", ") || "—"}</dd>
            </div>
          </dl>
        </div>

        <div className="rounded-lg border bg-white p-4">
          <h2 className="mb-2 text-base font-semibold">Next Best Action</h2>
          <p className="text-sm text-gray-600">Coming soon…</p>
          <div className="mt-4">
            <OwnerAssignment
              contactId={contact.id}
              currentOwnerId={contact.ownerId ?? null}
              onAssigned={(newOwnerId) => {
                setContact((c) => (c ? { ...c, ownerId: newOwnerId } as ContactReadDto : c));
              }}
            />
          </div>
        </div>
      </section>

      {/* Interactions */}
      {contactId && (
        <section>
          <InteractionsPanel contactId={contactId as string} />
        </section>
      )}
    </div>
  );
}
