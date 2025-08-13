"use client";
import React from "react";
import Link from "next/link";
import { useBilling, PlanTier } from "@/context/BillingContext";

const plans: { id: PlanTier; name: string; pricePerSeat: number; desc: string }[] = [
  { id: "free", name: "Free", pricePerSeat: 0, desc: "Basic features for testing" },
  { id: "pro", name: "Pro", pricePerSeat: 19, desc: "For small teams" },
  { id: "business", name: "Business", pricePerSeat: 49, desc: "Advanced features & support" },
];

export function SubscriptionPlanSelector() {
  const { plan, setPlan, seats, setSeats } = useBilling();

  return (
    <div className="mx-auto max-w-3xl p-6">
      <h1 className="text-2xl font-semibold mb-4">Choose your plan</h1>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {plans.map((p) => (
          <button
            key={p.id}
            onClick={() => setPlan(p.id)}
            className={`rounded border p-4 text-left hover:shadow transition ${
              plan === p.id ? "border-blue-600 ring-2 ring-blue-400" : "border-gray-300"
            }`}
          >
            <div className="flex items-baseline justify-between">
              <span className="font-medium">{p.name}</span>
              <span className="text-sm text-gray-600">${p.pricePerSeat}/seat</span>
            </div>
            <p className="text-sm text-gray-500 mt-2">{p.desc}</p>
          </button>
        ))}
      </div>

      <div className="mt-6">
        <label htmlFor="seats" className="block text-sm font-medium mb-1">Seats</label>
        <input
          id="seats"
          title="Number of seats"
          type="number"
          min={1}
          value={seats}
          onChange={(e) => setSeats(Math.max(1, Number(e.target.value)))}
          className="w-32 rounded border border-gray-300 px-3 py-2"
        />
      </div>

      <div className="mt-8 flex items-center justify-between">
        <p className="text-sm text-gray-600">Selected: {plan} • {seats} seat(s)</p>
        <Link href="/onboarding/checkout" className="inline-flex items-center rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700">
          Continue
        </Link>
      </div>
    </div>
  );
}

export default SubscriptionPlanSelector;
