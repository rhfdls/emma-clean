// SPRINT1: UserRegistrationForm onboarding step
import React from "react";

export default function UserRegistrationForm() {
  // TODO: Implement form fields for name, email, password, org name, etc.
  return (
    <main className="max-w-xl mx-auto py-12 px-4">
      <h1 className="text-3xl font-bold mb-6">Sign Up</h1>
      <form className="flex flex-col gap-4">
        {/* TODO: Add form fields here */}
        <button type="submit" className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700 transition">Create Account</button>
      </form>
    </main>
  );
}
