import Link from "next/link";
import PageContainer from "@/components/ui/PageContainer";
import { Card, CardContent } from "@/components/ui/Card";

export default function Home() {
  return (
    <main className="min-h-dvh bg-neutral-50">
      <PageContainer>
        <Card>
          <CardContent className="p-8">
            <h1 className="text-4xl font-bold text-gray-900">Welcome to EMMA</h1>
            <p className="mt-2 text-gray-800 text-base">AI-first CRM onboarding demo.</p>
            <div className="mt-8 flex gap-4">
              <Link href="/onboarding" className="rounded-md bg-blue-600 px-5 py-2.5 text-white font-semibold shadow-sm hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-300">Start onboarding</Link>
              <Link href="/join/testtoken123" className="rounded-md border border-gray-300 px-5 py-2.5 text-gray-900 hover:bg-gray-50">Try join with token</Link>
              <Link href="/contacts" className="rounded-md border border-gray-300 px-5 py-2.5 text-gray-900 hover:bg-gray-50">View contacts</Link>
            </div>
            <p className="mt-6 text-sm text-gray-600">
              Backend base URL is controlled by <code className="px-1 py-0.5 rounded bg-gray-100">NEXT_PUBLIC_API_URL</code>.
            </p>
            {process.env.NODE_ENV === "development" && (
              <p className="mt-2 text-sm">
                <Link href="/dev-token" className="text-blue-700 underline">Mint a dev token</Link> to call protected endpoints in development.
              </p>
            )}
          </CardContent>
        </Card>
      </PageContainer>
    </main>
  );
}
