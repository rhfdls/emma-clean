"use client";
import PageContainer from "@/components/ui/PageContainer";
import { Card, CardContent } from "@/components/ui/Card";
export default function VerifyRequiredPage() {
  return (
    <main className="min-h-dvh bg-neutral-50">
      <PageContainer>
        <Card>
          <CardContent className="p-6 space-y-3">
            <h1 className="text-2xl font-bold text-gray-900">Email verification required</h1>
            <p className="text-sm text-gray-800">
              Please check your email for a verification link. After verifying, return to this tab and continue.
            </p>
          </CardContent>
        </Card>
      </PageContainer>
    </main>
  );
}
