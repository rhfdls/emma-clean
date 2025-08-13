"use client";
import { useMemo } from "react";
import CheckoutPreview from "@/components/onboarding/CheckoutPreview";
import StripeCheckoutContainer from "@/components/onboarding/StripeCheckoutContainer";
import { useBilling } from "@/context/BillingContext";
import { useRouter } from "next/navigation";

const priceMap: Record<string, number> = { free: 0, pro: 19, business: 49 };

export default function OnboardingCheckoutPage() {
  const router = useRouter();
  const { plan, seats } = useBilling();
  const amount = useMemo(() => priceMap[plan] * seats, [plan, seats]);

  return (
    <main className="min-h-dvh bg-white">
      <div className="mx-auto grid max-w-4xl gap-6 p-6 md:grid-cols-2 md:py-12">
        <CheckoutPreview />
        <StripeCheckoutContainer amount={amount} onMockCheckout={() => router.push("/onboarding/register")} />
      </div>
    </main>
  );
}
