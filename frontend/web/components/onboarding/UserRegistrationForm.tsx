"use client";
import React, { useState } from "react";

export interface RegistrationPayload {
  email: string;
  password: string;
  orgName: string;
  agree: boolean;
}

export default function UserRegistrationForm({ onSubmit }: { onSubmit: (p: RegistrationPayload) => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [orgName, setOrgName] = useState("");
  const [agree, setAgree] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!email || !password || !orgName) {
      setError("Please fill in all fields.");
      return;
    }
    if (!agree) {
      setError("You must agree to email verification policy.");
      return;
    }
    onSubmit({ email, password, orgName, agree });
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {error && <div className="rounded border border-red-300 bg-red-50 p-2 text-sm text-red-700">{error}</div>}
      <div>
        <label className="block text-sm font-medium mb-1">Organization name</label>
        <input value={orgName} onChange={(e) => setOrgName(e.target.value)} className="w-full rounded border border-gray-300 px-3 py-2" placeholder="Acme Inc." />
      </div>
      <div>
        <label className="block text-sm font-medium mb-1">Work email</label>
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="w-full rounded border border-gray-300 px-3 py-2" placeholder="you@company.com" />
      </div>
      <div>
        <label className="block text-sm font-medium mb-1">Password</label>
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} className="w-full rounded border border-gray-300 px-3 py-2" placeholder="••••••••" />
      </div>
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" checked={agree} onChange={(e) => setAgree(e.target.checked)} />
        <span>I agree that new accounts start as PendingVerification and must verify via email.</span>
      </label>
      <button type="submit" className="rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700">Create account</button>
    </form>
  );
}
