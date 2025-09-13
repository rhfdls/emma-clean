"use client";
import { useState } from "react";
import { apiPost } from "@/lib/api";
import { useRouter } from "next/navigation";

export default function NewUserPage() {
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);
  const router = useRouter();

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      await apiPost("/api/admin/users", { email });
      router.push("/admin/users");
    } catch (e: any) {
      alert(e?.title || "Create failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="max-w-md mx-auto p-6 space-y-4">
      <h1 className="text-2xl font-semibold">New User</h1>
      <form className="space-y-3" onSubmit={submit}>
        <input
          className="w-full border p-2 rounded"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <button disabled={busy} className="px-4 py-2 rounded bg-black text-white">
          {busy ? "Saving..." : "Save"}
        </button>
      </form>
    </main>
  );
}
