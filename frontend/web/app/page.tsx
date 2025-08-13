import Link from "next/link";

export default function Home() {
  return (
    <main className="min-h-dvh bg-white">
      <div className="mx-auto max-w-2xl p-8 md:py-16">
        <h1 className="text-3xl font-semibold">Welcome to EMMA</h1>
        <p className="mt-2 text-gray-600">AI-first CRM onboarding demo.</p>
        <div className="mt-8 flex gap-4">
          <Link href="/onboarding" className="rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700">Start onboarding</Link>
          <Link href="/join/testtoken123" className="rounded border border-gray-300 px-4 py-2 hover:bg-gray-50">Try join with token</Link>
        </div>
        <p className="mt-6 text-sm text-gray-500">
          Backend base URL is controlled by <code className="px-1 py-0.5 rounded bg-gray-100">NEXT_PUBLIC_API_BASE_URL</code>.
        </p>
      </div>
    </main>
  );
}
