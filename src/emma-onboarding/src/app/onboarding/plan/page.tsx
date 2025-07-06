'use client';
// SPRINT1: SubscriptionPlanSelector onboarding step
import React, { useEffect, useState } from 'react';
import type { Plan } from '../context/OnboardingContext';

// import { Plan } from '../context/OnboardingContext'; // Removed this line

import { useRouter } from 'next/navigation';
import { useOnboarding } from '../../context/OnboardingContext';

export default function SubscriptionPlanSelector() {
  const [plans, setPlans] = useState<Plan[]>([]);
  const [selectedPlan, setSelectedPlan] = useState<Plan | null>(null);
  const [seatCount, setSeatCount] = useState(1);
  const [seatError, setSeatError] = useState<string | null>(null);
  const router = useRouter();
  const { setData } = useOnboarding();

  useEffect(() => {
    fetch('/api/enums/Plan')
      .then((res) => res.json())
      .then((data) => {
        setPlans(data);
        setSelectedPlan(data[0]);
      });
  }, []);

  if (!selectedPlan) return <div>Loading plans...</div>;

  const total = selectedPlan.price * seatCount;

  const handlePlanChange = (planKey: string) => {
    const plan = plans.find((p) => p.key === planKey);
    if (plan) setSelectedPlan(plan);
  };

  const handleSeatChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!selectedPlan) return;
    const value = Math.max(1, parseInt(e.target.value, 10) || 1);
    setSeatCount(value);
    // Validate seat count based on plan metadata
    if (selectedPlan.minSeats && value < selectedPlan.minSeats) {
      setSeatError(`Minimum seats for this plan: ${selectedPlan.minSeats}`);
    } else if (selectedPlan.maxSeats && value > selectedPlan.maxSeats) {
      setSeatError(`Maximum seats for this plan: ${selectedPlan.maxSeats}`);
    } else {
      setSeatError(null);
    }
  };

  const handleContinue = () => {
    if (seatError) return;
    setData((prev) => ({
      ...prev,
      plan: selectedPlan, // store the full plan object
      seatCount,
    }));
    router.push('/onboarding/checkout');
  };

  return (
    <main className="max-w-xl mx-auto py-12 px-4">
      <h1 className="text-3xl font-bold mb-6">Choose your subscription plan</h1>
      <div className="flex flex-col gap-4 mb-6">
        {plans.map((plan) => (
          <label key={plan.key} className={`flex items-center border rounded-lg p-4 cursor-pointer ${selectedPlan.key === plan.key ? "border-blue-600 bg-blue-50" : "border-gray-200"}`}>
            <input
              type="radio"
              name="plan"
              value={plan.key}
              checked={selectedPlan.key === plan.key}
              onChange={() => handlePlanChange(plan.key)}
              className="mr-3"
            />
            <span className="font-semibold text-lg mr-2">{plan.displayName}</span>
            <span className="text-gray-500">${plan.price}/seat/month</span>
            <span className="text-xs text-gray-400 ml-4">{plan.description}</span>
            {plan.features && (
  <ul className="ml-8 list-disc text-xs text-gray-500">
    {plan.features.map((f: string) => <li key={f}>{f}</li>)}
  </ul>
)}
          </label>
        ))}
      </div>
      <div className="mb-6">
        <label className="block font-medium mb-2">Number of seats</label>
        <input
          type="number"
          min={selectedPlan?.minSeats || 1}
          max={selectedPlan?.maxSeats || undefined}
          value={seatCount}
          onChange={handleSeatChange}
          className="border rounded px-3 py-2 w-24"
          placeholder="Number of seats"
          aria-label="Seat count"
        />
        {seatError && (
          <div className="text-red-600 text-sm mt-1">{seatError}</div>
        )}
      </div>
      <div className="mb-8">
        <div className="text-lg font-medium">Total: <span className="font-bold">${total}/month</span></div>
      </div>
      {/* TODO: Add navigation to CheckoutPreview and persist state */}
      <button
  className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700 transition"
  onClick={handleContinue}
>
  Continue
</button>
    </main>
  );
}
