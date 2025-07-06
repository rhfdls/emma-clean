'use client';
// SPRINT1: /checkout step in onboarding
import React from 'react';
import { useRouter } from 'next/navigation';
import { useOnboarding } from '../../context/OnboardingContext';

export default function CheckoutPage() {
  const router = useRouter();
  const { data, setData } = useOnboarding();
  const { plan, seatCount } = data;

  // Prepare Stripe-style metadata
  const pricePerSeat = plan?.price ?? 0;
  const total = pricePerSeat * seatCount;
  const stripeMetadata = {
    planId: `gog_${plan?.key ?? ''}_monthly`,
    total,
  };

  // Sync Stripe metadata to context (only if plan exists)
  React.useEffect(() => {
    if (plan) {
      setData((prev) => ({ ...prev, stripeMetadata }));
    }
    // eslint-disable-next-line
  }, [plan, seatCount]);

  if (!plan) {
    return <div className="text-red-600">No plan selected. Please go back and select a plan.</div>;
  }

  return (
    <main className="max-w-xl mx-auto py-12 px-4">
      <h1 className="text-3xl font-bold mb-6">Confirm your subscription</h1>
      <div className="mb-4 flex flex-col gap-2">
        <div className="flex justify-between"><span>Plan:</span> <span className="font-medium">{plan.label}</span></div>
        <div className="flex justify-between text-xs text-gray-500"><span>Description:</span> <span>{plan.description}</span></div>
        <div className="flex justify-between"><span>Price per seat:</span> <span>${pricePerSeat}/mo</span></div>
        <div className="flex justify-between"><span>Seats:</span> <span>{seatCount}</span></div>
        <div className="flex justify-between text-lg font-bold border-t pt-2 mt-2"><span>Total:</span> <span>${total}/mo</span></div>
      </div>
      <div className="flex gap-4 mt-8">
        <button
          className="px-6 py-2 rounded border border-gray-400 bg-white text-gray-700 hover:bg-gray-100"
          onClick={() => router.push('/onboarding/plan')}
        >
          Back
        </button>
        <button
          className="px-6 py-2 rounded bg-blue-600 text-white hover:bg-blue-700"
          onClick={() => router.push('/onboarding/signup')}
        >
          Continue to Signup
        </button>
      </div>
    </main>
  );
}
