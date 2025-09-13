"use client";
import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api";
import { useParams, useRouter } from "next/navigation";
import { toast } from "sonner";

type User = { id: string; email: string; isActive: boolean; roles: string[] };

export default function EditUserPage() {
  const params = useParams<{ id: string }>();
  const id = params?.id as string;
  const router = useRouter();

  const [u, setU] = useState<User | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [confirming, setConfirming] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const data = await apiGet<User>(`/api/admin/users/${id}`);
        setU(data);
      } catch (e: any) {
        setErr(e?.title || "Failed to load user");
      }
    })();
  }, [id]);

  function requestDeactivate() {
    console.log("Deactivate clicked", { id });
    setConfirming(true);
  }

  async function confirmDeactivate() {
    setPending(true);
    try {
      await apiPost(`/api/admin/users/${id}/deactivate`);
      toast.success("User deactivated");
      router.push("/admin/users");
    } catch (e: any) {
      console.error("Deactivate failed", e);
      toast.error(e?.title || "Deactivate failed");
      setConfirming(false);
    } finally {
      setPending(false);
    }
  }

  async function reactivate() {
    setPending(true);
    try {
      await apiPost(`/api/admin/users/${id}/reactivate`);
      toast.success("User reactivated");
      router.push("/admin/users");
    } catch (e: any) {
      console.error("Reactivate failed", e);
      toast.error(e?.title || "Reactivate failed");
    } finally {
      setPending(false);
    }
  }

  if (err) return <div className="p-6 text-red-600">{err}</div>;
  if (!u) return <div className="p-6">Loading…</div>;

  return (
    <main className="max-w-md mx-auto p-6 space-y-3">
      <h1 className="text-2xl font-semibold">Edit User</h1>
      <div className="border rounded p-3">
        <div><b>Email:</b> {u.email}</div>
        <div><b>Active:</b> {String(u.isActive)}</div>
        <div><b>Roles:</b> {u.roles?.join(", ") || "—"}</div>
      </div>
      {!confirming && u.isActive ? (
        <button
          type="button"
          onClick={requestDeactivate}
          disabled={pending}
          className="px-4 py-2 rounded bg-red-600 text-white disabled:opacity-60 cursor-pointer"
        >
          Deactivate
        </button>
      ) : confirming && u.isActive ? (
        <div className="flex items-center gap-2">
          <span className="text-sm">Confirm deactivation?</span>
          <button
            type="button"
            onClick={confirmDeactivate}
            disabled={pending}
            className="px-3 py-2 rounded bg-red-700 text-white disabled:opacity-60 cursor-pointer"
          >
            {pending ? "Deactivating…" : "Confirm"}
          </button>
          <button
            type="button"
            onClick={() => setConfirming(false)}
            disabled={pending}
            className="px-3 py-2 rounded bg-gray-200 text-gray-900 disabled:opacity-60 cursor-pointer"
          >
            Cancel
          </button>
        </div>
      ) : (
        <button
          type="button"
          onClick={reactivate}
          disabled={pending}
          className="px-4 py-2 rounded bg-green-600 text-white disabled:opacity-60 cursor-pointer"
        >
          {pending ? "Reactivating…" : "Reactivate"}
        </button>
      )}
    </main>
  );
}
