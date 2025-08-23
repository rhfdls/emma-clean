"use client";
import { useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";
import PageContainer from "@/components/ui/PageContainer";
import { Card, CardContent, CardHeader } from "@/components/ui/Card";
import Label from "@/components/ui/Label";
import Input from "@/components/ui/Input";
import Button from "@/components/ui/Button";
import { api } from "@/lib/api";
import Link from "next/link";

interface InteractionItem {
  id: string;
  timestamp: string;
  type: string;
  subject?: string | null;
  content?: string | null;
  analysisSummary?: string | null;
}

export default function ContactDetailPage() {
  const params = useParams<{ id: string }>();
  const contactId = useMemo(() => params?.id, [params]);

  const [items, setItems] = useState<InteractionItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [errorStatus, setErrorStatus] = useState<number | null>(null);

  // Composer state
  const [subject, setSubject] = useState("");
  const [content, setContent] = useState("");
  const [consentGranted, setConsentGranted] = useState(true);
  const [occurredAt, setOccurredAt] = useState<string>("");
  const [posting, setPosting] = useState(false);
  const [agentBusy, setAgentBusy] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [modalContent, setModalContent] = useState<any>(null);

  async function load() {
    if (!contactId) return;
    setLoading(true);
    setError(null);
    setErrorStatus(null);
    try {
      const data = await api<InteractionItem[]>(`/api/contacts/${contactId}/interactions`, { method: "GET" });
      setItems(data);
    } catch (e: any) {
      setError(e?.message || "Failed to load interactions");
      if (typeof e?.status === "number") setErrorStatus(e.status);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [contactId]);

  async function postInteraction() {
    if (!contactId) return;
    setPosting(true);
    setError(null);
    try {
      const body: any = {
        subject: subject || undefined,
        content: content || undefined,
        consentGranted,
      };
      if (occurredAt) body.occurredAt = new Date(occurredAt).toISOString();
      await api(`/api/contacts/${contactId}/interactions`, {
        method: "POST",
        json: body,
      });
      setSubject("");
      setContent("");
      setOccurredAt("");
      await load();
    } catch (e: any) {
      setError(e?.message || "Failed to post interaction");
      if (typeof e?.status === "number") setErrorStatus(e.status);
    } finally {
      setPosting(false);
    }
  }

  async function suggestFollowup() {
    setAgentBusy(true);
    setError(null);
    setErrorStatus(null);
    try {
      // Dev-only endpoint; may not be enabled. Handle 404/500 gracefully.
      const json = await api(`/api/agent/suggest-followup`, {
        method: "POST",
        json: { contactId },
      });
      setModalContent(json);
      setModalOpen(true);
    } catch (e: any) {
      setError(e?.message || "Suggest follow-up failed (dev endpoint may be disabled)");
      if (typeof e?.status === "number") setErrorStatus(e.status);
    } finally {
      setAgentBusy(false);
    }
  }

  return (
    <main className="min-h-dvh bg-neutral-50">
      <PageContainer className="py-6">
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          <Card className="lg:col-span-2">
            <CardHeader>
              <h1 className="text-xl font-semibold text-gray-900">Interactions</h1>
            </CardHeader>
            <CardContent>
              {loading ? (
                <p className="text-sm text-gray-600">Loading…</p>
              ) : error ? (
                <div className="rounded-md border border-red-300 bg-red-50 p-3 text-sm text-red-700">
                  <div>{error}</div>
                  {(errorStatus === 401 || errorStatus === 403) && (
                    <div className="mt-2">
                      <Link href="/dev-token" className="font-medium text-blue-700 underline">Sign in (dev token)</Link>
                    </div>
                  )}
                </div>
              ) : items.length === 0 ? (
                <p className="text-sm text-gray-700">No interactions yet.</p>
              ) : (
                <ul className="space-y-3">
                  {items.map((i) => (
                    <li key={i.id} className="rounded-md border border-gray-200 bg-white p-3">
                      <div className="flex items-center justify-between">
                        <div className="text-sm text-gray-700">
                          <span className="font-medium">{i.type}</span>
                          <span className="mx-2 text-gray-400">•</span>
                          <span>{new Date(i.timestamp).toLocaleString()}</span>
                        </div>
                      </div>
                      {i.subject && <div className="mt-1 text-sm font-medium text-gray-900">{i.subject}</div>}
                      {i.content && <div className="mt-1 whitespace-pre-wrap text-sm text-gray-800">{i.content}</div>}
                      {i.analysisSummary && (
                        <div className="mt-2 text-sm text-gray-600">
                          <span className="font-semibold">AI summary:</span> {i.analysisSummary}
                        </div>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900">Log interaction</h2>
            </CardHeader>
            <CardContent className="space-y-3">
              <div>
                <Label htmlFor="subject">Subject</Label>
                <Input id="subject" value={subject} onChange={(e) => setSubject(e.target.value)} placeholder="Subject" />
              </div>
              <div>
                <Label htmlFor="content">Content</Label>
                <textarea
                  id="content"
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  placeholder="What happened?"
                  className="mt-1 block w-full min-h-28 rounded-md border border-gray-300 px-3 py-2 text-sm text-gray-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-300"
                />
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div>
                  <Label htmlFor="occurredAt">Occurred at (optional)</Label>
                  <Input
                    id="occurredAt"
                    type="datetime-local"
                    value={occurredAt}
                    onChange={(e) => setOccurredAt(e.target.value)}
                  />
                </div>
                <div className="flex items-end">
                  <label className="inline-flex items-center gap-2 text-sm text-gray-700">
                    <input
                      type="checkbox"
                      checked={consentGranted}
                      onChange={(e) => setConsentGranted(e.target.checked)}
                      className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    />
                    Consent for AI analysis
                  </label>
                </div>
              </div>
              <div className="flex gap-2">
                <Button onClick={postInteraction} disabled={posting || !contactId}>
                  {posting ? "Posting…" : "Create interaction"}
                </Button>
                <Button onClick={suggestFollowup} disabled={agentBusy} className="bg-gray-800 hover:bg-gray-900">
                  {agentBusy ? "Thinking…" : "Suggest follow-up"}
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
        {modalOpen && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
            <div className="w-full max-w-xl rounded-lg bg-white shadow-lg">
              <div className="flex items-center justify-between border-b px-4 py-2">
                <h3 className="text-base font-semibold text-gray-900">Suggested follow-up</h3>
                <button
                  className="rounded p-1 text-gray-500 hover:bg-gray-100"
                  onClick={() => setModalOpen(false)}
                  aria-label="Close"
                >
                  ✕
                </button>
              </div>
              <div className="max-h-[60vh] overflow-auto p-4">
                <pre className="whitespace-pre-wrap text-sm text-gray-800">{JSON.stringify(modalContent, null, 2)}</pre>
              </div>
              <div className="flex justify-end gap-2 border-t px-4 py-3">
                <Button className="bg-gray-700 hover:bg-gray-800" onClick={() => setModalOpen(false)}>Close</Button>
              </div>
            </div>
          </div>
        )}
      </PageContainer>
    </main>
  );
}
