"use client";
import React, { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";


export default function VerifyPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [status, setStatus] = useState<"idle" | "verifying" | "success" | "error">("idle");
  const [error, setError] = useState<string>("");
  const [token, setToken] = useState<string>(searchParams.get("token") || "");

  const handleVerify = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setStatus("verifying");
    setError("");
    try {
      const res = await fetch("http://localhost:5000/api/account/verify", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token }),
      });
      if (res.ok) {
        setStatus("success");
        setTimeout(() => router.push("/dashboard"), 1200);
      } else {
        const data = await res.json();
        setError(data.message || "Verification failed.");
        setStatus("error");
      }
    } catch (err) {
      setError("Network error. Please try again.");
      setStatus("error");
    }
  };

  return (
    <main className="max-w-md mx-auto py-12 px-4">
      <h1 className="text-2xl font-bold mb-6">Verify Your Email</h1>
      <form onSubmit={handleVerify} className="space-y-4">
        <input
          type="text"
          name="token"
          placeholder="Verification Token"
          value={token}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setToken(e.target.value)}
          required
          className="border px-3 py-2 rounded shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-300 w-full"
        />
        <button
          type="submit"
          disabled={status === "verifying"}
          className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700 transition disabled:opacity-50 font-semibold tracking-wide shadow"
        >
          {status === "verifying" ? "Verifying..." : "Verify"}
        </button>
      </form>
      {status === "success" && (
        <div className="mt-4 text-green-600">Verification successful! Redirecting...</div>
      )}
      {status === "error" && (
        <div className="mt-4 text-red-600">{error}</div>
      )}
    </main>
  );
}
