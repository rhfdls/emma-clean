"use client";
import { useState } from "react";
import CheckoutPreview from "@/components/onboarding/CheckoutPreview";
import UserRegistrationForm, { RegistrationPayload } from "@/components/onboarding/UserRegistrationForm";
import { useBilling } from "@/context/BillingContext";
// import { apiPost } from "@/lib/api"; // TODO: wire when backend endpoints are confirmed

export default function OnboardingRegisterPage() {
  const { plan, seats } = useBilling();
  const [status, setStatus] = useState<"idle" | "submitting" | "success" | "error">("idle");
  const [message, setMessage] = useState<string>("");

  async function handleSubmit(payload: RegistrationPayload) {
    try {
      setStatus("submitting");
      setMessage("");
      // TODO: Confirm backend endpoint for account creation + org creation + plan selection
      // const res = await apiPost("/onboarding/register", { ...payload, plan, seats });
      await new Promise((r) => setTimeout(r, 700)); // mock
      setStatus("success");
      setMessage("Account created. Check your email for verification. You will be redirected after verification.");
    } catch (e: any) {
      setStatus("error");
      setMessage(e?.message || "Registration failed.");
    }
  }

  return (
    <main className="min-h-dvh bg-white">
      <div className="mx-auto grid max-w-4xl gap-8 p-6 md:grid-cols-2 md:py-12">
        <div>
          <h1 className="text-2xl font-semibold mb-4">Create your account</h1>
          <UserRegistrationForm onSubmit={handleSubmit} />
          {status !== "idle" && (
            <div className={`mt-4 text-sm ${status === "error" ? "text-red-600" : "text-gray-700"}`}>{message}</div>
          )}
        </div>
        <div>
          <CheckoutPreview />
          <div className="mt-4 text-sm text-gray-600">Selected plan: <b>{plan}</b> • Seats: <b>{seats}</b></div>
        </div>
      </div>
    </main>
  );
}
