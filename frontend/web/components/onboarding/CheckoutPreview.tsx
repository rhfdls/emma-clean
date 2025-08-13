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
    <div className="rounded border border-gray-200 p-4">
      <h2 className="text-lg font-medium mb-2">Pricing Summary</h2>
      <div className="text-sm space-y-1">
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
        <div className="mt-2 h-px bg-gray-200" />
        <div className="flex justify-between mt-2 text-base">
          <span>Total due today</span>
          <span className="font-semibold">${subtotal}</span>
        </div>
      </div>
    </div>
  );
}
