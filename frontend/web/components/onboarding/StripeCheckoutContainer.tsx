"use client";
import React from "react";

export default function StripeCheckoutContainer({ amount, onMockCheckout }: { amount: number; onMockCheckout: () => void }) {
  return (
    <div className="rounded border border-gray-200 p-4">
      <h2 className="text-lg font-medium mb-2">Payment</h2>
      <p className="text-sm text-gray-600 mb-3">SPRINT1 scaffold: Stripe integration placeholder.</p>
      <button onClick={onMockCheckout} className="rounded bg-emerald-600 px-4 py-2 text-white hover:bg-emerald-700">
        Pay ${amount} now (mock)
      </button>
    </div>
  );
}
