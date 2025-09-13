"use client";
import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api";

type Profile = { email: string; organizationId: string; organizationName?: string; roles?: string[] };
type Counts = { users: number; contacts: number };

export default function AdminPage() {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [counts, setCounts] = useState<Counts | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        const p = await apiGet<Profile>("/api/account/profile");
        setProfile(p);
        const c = await apiGet<Counts>("/api/admin/summary");
        setCounts(c);
      } catch (e: any) {
        setErr(e?.title || "Failed to load");
      }
    })();
  }, []);

  if (err) return <div className="p-6 text-red-600">{err}</div>;

  return (
    <main className="max-w-3xl mx-auto p-6 space-y-6">
      <h1 className="text-2xl font-semibold">Admin</h1>
      <section className="rounded border p-4">
        <h2 className="font-medium">Organization</h2>
        <p><b>ID:</b> {profile?.organizationId}</p>
        <p><b>Name:</b> {profile?.organizationName ?? "—"}</p>
        <p><b>Your email:</b> {profile?.email}</p>
        <p><b>Roles:</b> {profile?.roles?.join(", ") || "—"}</p>
      </section>

      <section className="rounded border p-4 grid grid-cols-2 gap-4">
        <div className="border rounded p-3">
          <div className="text-sm text-neutral-500">Users</div>
          <div className="text-2xl">{counts?.users ?? "…"}</div>
        </div>
        <div className="border rounded p-3">
          <div className="text-sm text-neutral-500">Contacts</div>
          <div className="text-2xl">{counts?.contacts ?? "…"}</div>
        </div>
      </section>

      <div className="flex gap-3">
        <a className="underline" href="/admin/users">Manage users</a>
        <a className="underline" href="/contacts">Contacts</a>
      </div>
    </main>
  );
}
