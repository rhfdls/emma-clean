"use client";
import React, { useMemo } from "react";
import { useBilling } from "@/context/BillingContext";

const priceMap: Record<string, number> = {
  free: 0,
  pro: 19,
  business: 49,
};

export default function CheckoutPreview() {
  const { plan, seats } = useBilling();
  const pricePerSeat = priceMap[plan];
  const subtotal = useMemo(() => seats * pricePerSeat, [seats, pricePerSeat]);

  return (
    <div className="rounded-lg border border-gray-200 p-5 bg-white shadow-sm">
      <h2 className="text-xl font-semibold mb-3 text-gray-900">Pricing Summary</h2>
      <div className="text-sm space-y-1 text-gray-900">
        <div className="flex justify-between">
          <span>Plan</span>
          <span className="font-medium">{plan}</span>
        </div>
        <div className="flex justify-between">
          <span>Seats</span>
          <span className="font-medium">{seats}</span>
        </div>
        <div className="flex justify-between">
          <span>Price per seat</span>
          <span className="font-medium">${pricePerSeat}</span>
        </div>
        <div className="mt-3 h-px bg-gray-200" />
        <div className="flex justify-between mt-3 text-base">
          <span>Total due today</span>
          <span className="font-bold">${subtotal}</span>
        </div>
      </div>
    </div>
  );
}
