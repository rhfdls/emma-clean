"use client";
import { useState } from "react";
import CheckoutPreview from "@/components/onboarding/CheckoutPreview";
import UserRegistrationForm, { RegistrationPayload } from "@/components/onboarding/UserRegistrationForm";
import PageContainer from "@/components/ui/PageContainer";
import { Card, CardContent } from "@/components/ui/Card";
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
    <main className="min-h-dvh bg-neutral-50">
      <PageContainer>
        <div className="grid gap-8 md:grid-cols-2">
          <Card>
            <CardContent>
              <h1 className="text-3xl font-bold mb-4 text-gray-900">Create your account</h1>
              <UserRegistrationForm onSubmit={handleSubmit} />
              {status !== "idle" && (
                <div className={`mt-4 text-sm ${status === "error" ? "text-red-600" : "text-gray-900"}`}>{message}</div>
              )}
            </CardContent>
          </Card>
          <Card>
            <CardContent>
              <CheckoutPreview />
              <div className="mt-4 text-sm text-gray-800">Selected plan: <b>{plan}</b> • Seats: <b>{seats}</b></div>
            </CardContent>
          </Card>
        </div>
      </PageContainer>
    </main>
  );
}
