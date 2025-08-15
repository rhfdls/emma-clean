"use client";
export default function VerifyRequiredPage() {
  return (
    <div className="max-w-md p-6 space-y-3">
      <h1 className="text-2xl font-bold">Email verification required</h1>
      <p className="text-sm opacity-80">
        Please check your email for a verification link. After verifying, return to this tab and continue.
      </p>
    </div>
  );
}
