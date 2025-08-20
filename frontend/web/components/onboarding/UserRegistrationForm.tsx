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
    <form onSubmit={handleSubmit} className="space-y-5">
      {error && <div className="rounded-md border border-red-300 bg-red-50 p-3 text-sm text-red-700">{error}</div>}
      <div>
        <label className="block text-sm font-semibold mb-1 text-gray-900">Organization name</label>
        <input
          value={orgName}
          onChange={(e) => setOrgName(e.target.value)}
          className="w-full rounded-md border border-gray-300 bg-white px-3 py-2.5 text-gray-900 placeholder:text-gray-400 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-200"
          placeholder="Acme Inc."
        />
      </div>
      <div>
        <label className="block text-sm font-semibold mb-1 text-gray-900">Work email</label>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="w-full rounded-md border border-gray-300 bg-white px-3 py-2.5 text-gray-900 placeholder:text-gray-400 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-200"
          placeholder="you@company.com"
        />
      </div>
      <div>
        <label className="block text-sm font-semibold mb-1 text-gray-900">Password</label>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="w-full rounded-md border border-gray-300 bg-white px-3 py-2.5 text-gray-900 placeholder:text-gray-400 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-200"
          placeholder="••••••••"
        />
      </div>
      <label className="flex items-start gap-2 text-sm text-gray-800">
        <input className="mt-0.5" type="checkbox" checked={agree} onChange={(e) => setAgree(e.target.checked)} />
        <span>I agree that new accounts start as <b>PendingVerification</b> and must verify via email.</span>
      </label>
      <button type="submit" className="inline-flex items-center justify-center rounded-md bg-blue-600 px-5 py-2.5 text-white font-semibold shadow-sm hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-300">
        Create account
      </button>
    </form>
  );
}
