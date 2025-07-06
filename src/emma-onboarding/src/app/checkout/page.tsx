// SPRINT1: CheckoutPreview onboarding step
import React from "react";

export default function CheckoutPreview() {
  // TODO: Accept props or context for plan/seat/total
  return (
    <main className="max-w-xl mx-auto py-12 px-4">
      <h1 className="text-3xl font-bold mb-6">Confirm your subscription</h1>
      <div className="mb-4">{/* Plan and seat summary here */}</div>
      <div className="mb-8">{/* Total price here */}</div>
      {/* TODO: Add navigation to UserRegistrationForm */}
      <button className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700 transition">Proceed to Sign Up</button>
    </main>
  );
}
