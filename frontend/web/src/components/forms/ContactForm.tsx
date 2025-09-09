"use client";

import { useState, FormEvent } from "react";
import { useRouter } from "next/navigation";
import { apiPost } from "@/lib/api";
import { toast } from "sonner";
import { toastProblem } from "@/lib/toast-problem";
import type { ContactCreateDto } from "@/types/contact";
import { normalizeTags } from "@/lib/tags";

export default function ContactForm() {
  const [pending, setPending] = useState(false);
  const [firstName, setFirst] = useState("");
  const [lastName, setLast] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [tags, setTags] = useState("");
  const router = useRouter();

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setPending(true);
    try {
      const payload: ContactCreateDto = {
        firstName,
        lastName,
        ...(email ? { email } : {}),
        ...(phone ? { phone } : {}),
        ...(tags.trim()
          ? { tags: normalizeTags(tags) }
          : {}),
      };

      const created = await apiPost<{ id: string }>("/api/Contact", payload);
      toast.success("Contact created");
      router.push(`/contacts/${created.id}`);
    } catch (err: any) {
      toastProblem(err);
    } finally {
      setPending(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="grid gap-4 max-w-xl">
      <div className="grid gap-1">
        <label htmlFor="firstName" className="text-sm font-medium">First name</label>
        <input
          id="firstName"
          className="border rounded-lg px-3 py-2 outline-none focus:ring w-full"
          placeholder="First name"
          required
          value={firstName}
          onChange={(e) => setFirst(e.target.value)}
        />
      </div>

      <div className="grid gap-1">
        <label htmlFor="lastName" className="text-sm font-medium">Last name</label>
        <input
          id="lastName"
          className="border rounded-lg px-3 py-2 outline-none focus:ring w-full"
          placeholder="Last name"
          required
          value={lastName}
          onChange={(e) => setLast(e.target.value)}
        />
      </div>

      <div className="grid gap-1">
        <label htmlFor="email" className="text-sm font-medium">Email (optional)</label>
        <input
          id="email"
          type="email"
          className="border rounded-lg px-3 py-2 outline-none focus:ring w-full"
          placeholder="name@example.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
      </div>

      <div className="grid gap-1">
        <label htmlFor="phone" className="text-sm font-medium">Phone (optional)</label>
        <input
          id="phone"
          className="border rounded-lg px-3 py-2 outline-none focus:ring w-full"
          placeholder="(555) 123-4567"
          value={phone}
          onChange={(e) => setPhone(e.target.value)}
        />
      </div>

      <div className="grid gap-1">
        <label htmlFor="tags" className="text-sm font-medium">Tags (comma-separated)</label>
        <input
          id="tags"
          placeholder="buyer,warm,ottawa"
          className="border rounded-lg px-3 py-2 outline-none focus:ring w-full"
          value={tags}
          onChange={(e) => setTags(e.target.value)}
        />
      </div>

      <div className="flex gap-3">
        <button
          type="submit"
          disabled={pending}
          className="rounded-lg bg-black text-white px-4 py-2 disabled:opacity-60"
        >
          {pending ? "Creating…" : "Create contact"}
        </button>
        <button
          type="button"
          disabled={pending}
          onClick={() => {
            setFirst("");
            setLast("");
            setEmail("");
            setPhone("");
            setTags("");
          }}
          className="rounded-lg border px-4 py-2"
        >
          Reset
        </button>
      </div>
    </form>
  );
}
