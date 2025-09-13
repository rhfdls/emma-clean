"use client";
import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api";

type User = { id: string; email: string; isActive: boolean; roles: string[] };

export default function UsersPage() {
  const [rows, setRows] = useState<User[]>([]);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        const items = await apiGet<User[]>("/api/admin/users");
        setRows(items);
      } catch (e: any) {
        setErr(e?.title || "Failed to load users");
      }
    })();
  }, []);

  if (err) return <div className="p-6 text-red-600">{err}</div>;

  return (
    <main className="max-w-3xl mx-auto p-6 space-y-4">
      <h1 className="text-2xl font-semibold">Users</h1>
      <a className="underline" href="/admin/users/new">+ New User</a>
      <ul className="divide-y border rounded">
        {rows.map((u) => (
          <li key={u.id} className="p-3 flex items-center justify-between">
            <div>
              <div className="font-medium">{u.email}</div>
              <div className="text-xs text-neutral-500">{u.roles?.join(", ") || "—"}</div>
            </div>
            <a className="underline" href={`/admin/users/${u.id}`}>Edit</a>
          </li>
        ))}
      </ul>
    </main>
  );
}
