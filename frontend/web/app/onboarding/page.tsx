import SubscriptionPlanSelector from "@/components/onboarding/SubscriptionPlanSelector";

export default function OnboardingStartPage() {
  return (
    <main className="min-h-dvh bg-neutral-50">
      <div className="mx-auto max-w-5xl p-6 md:py-12">
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 md:p-8">
          <SubscriptionPlanSelector />
        </div>
      </div>
    </main>
  );
}
