"use client";

import { useEffect, useMemo, useState } from "react";
import { createInteraction, listInteractions, type InteractionCreateDto, type InteractionReadDto } from "@/lib/interactionsApi";
import { toast } from "sonner";
import { toastProblem } from "@/lib/toast-problem";
import { normalizeTags } from "@/lib/tags";

type Props = { contactId: string };

const TYPES: InteractionCreateDto["type"][] = ["call", "email", "sms", "meeting", "note", "task", "other"];
const DIRECTIONS: NonNullable<InteractionCreateDto["direction"]>[] = ["inbound", "outbound", "system"];
const PRIVACY: InteractionCreateDto["privacyLevel"][] = ["public", "internal", "private", "confidential"];

export default function InteractionsPanel({ contactId }: Props) {
  const [items, setItems] = useState<InteractionReadDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);

  // form state
  const [type, setType] = useState<InteractionCreateDto["type"]>("call");
  const [direction, setDirection] = useState<InteractionCreateDto["direction"]>("outbound");
  const [timestamp, setTimestamp] = useState<string>(() => new Date().toISOString().slice(0, 16)); // yyyy-MM-ddTHH:mm
  const [privacyLevel, setPrivacy] = useState<InteractionCreateDto["privacyLevel"]>("internal");
  const [tags, setTags] = useState<string>("");
  const [subject, setSubject] = useState<string>("");
  const [content, setContent] = useState<string>("");

  const isoTimestamp = useMemo(() => {
    try { return new Date(timestamp).toISOString(); } catch { return new Date().toISOString(); }
  }, [timestamp]);

  useEffect(() => {
    let on = true;
    (async () => {
      try {
        const data = await listInteractions(contactId);
        if (on) setItems(data ?? []);
      } catch (e: any) {
        console.debug("List interactions failed:", e);
      } finally {
        if (on) setLoading(false);
      }
    })();
    return () => { on = false; };
  }, [contactId]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setPending(true);
    try {
      const dedupedTags = normalizeTags(tags);
      const payload: InteractionCreateDto = {
        type,
        direction,
        timestamp: isoTimestamp,
        privacyLevel,
        subject: subject?.trim() || undefined,
        content: content?.trim() || undefined,
        tags: dedupedTags.length ? dedupedTags : undefined,
      };
      const created = await createInteraction(contactId, payload);
      toast.success("Interaction saved");
      // optimistic clear & refresh
      setSubject("");
      setContent("");
      setTags("");
      setItems((prev) => [{
        id: created?.id ?? crypto.randomUUID(),
        timestamp: new Date(isoTimestamp).toISOString(),
        type,
        subject: payload.subject,
        contentPreview: payload.content?.slice(0, 160),
        tags: payload.tags,
      }, ...prev]);
    } catch (e: any) {
      toastProblem(e);
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="rounded-lg border p-4 bg-white space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="font-medium">Interactions</h3>
      </div>

      {/* Create form */}
      <form onSubmit={onSubmit} className="grid gap-3">
        <div className="grid gap-2 sm:grid-cols-3">
          <label className="grid gap-1">
            <span className="text-sm font-medium">Type</span>
            <select
              className="border rounded-lg px-3 py-2"
              value={type}
              onChange={(e) => setType(e.target.value as any)}
            >
              {TYPES.map(t => <option key={t} value={t}>{t}</option>)}
            </select>
          </label>

          <label className="grid gap-1">
            <span className="text-sm font-medium">Direction</span>
            <select
              className="border rounded-lg px-3 py-2"
              value={direction ?? ""}
              onChange={(e) => setDirection((e.target.value || undefined) as any)}
            >
              {DIRECTIONS.map(d => <option key={d} value={d}>{d}</option>)}
            </select>
          </label>

          <label className="grid gap-1">
            <span className="text-sm font-medium">When</span>
            <input
              type="datetime-local"
              className="border rounded-lg px-3 py-2"
              value={timestamp}
              onChange={(e) => setTimestamp(e.target.value)}
              required
            />
          </label>
        </div>

        <div className="grid gap-2 sm:grid-cols-2">
          <label className="grid gap-1">
            <span className="text-sm font-medium">Privacy</span>
            <select
              className="border rounded-lg px-3 py-2"
              value={privacyLevel}
              onChange={(e) => setPrivacy(e.target.value as any)}
            >
              {PRIVACY.map(p => <option key={p} value={p}>{p}</option>)}
            </select>
          </label>

          <label className="grid gap-1">
            <span className="text-sm font-medium">Tags (comma-separated)</span>
            <input
              className="border rounded-lg px-3 py-2"
              placeholder="CRM,followup"
              value={tags}
              onChange={(e) => setTags(e.target.value)}
            />
          </label>
        </div>

        <label className="grid gap-1">
          <span className="text-sm font-medium">Subject</span>
          <input
            className="border rounded-lg px-3 py-2"
            placeholder="Short subject"
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
          />
        </label>

        <label className="grid gap-1">
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium">Notes / content</span>
            <span className="text-xs text-muted-foreground">{content.length}/8000{content.length === 8000 ? " (max)" : ""}</span>
          </div>
          <textarea
            className="border rounded-lg px-3 py-2 min-h-28"
            value={content}
            onChange={(e) => setContent(e.target.value.slice(0, 8000))}
            placeholder="What happened? Key details, commitments, next steps…"
          />
          {content.length >= 8000 && (
            <span className="text-xs text-amber-600">Max length reached. For long transcripts, upload to blob and reference it.</span>
          )}
        </label>

        <div className="flex gap-3">
          <button
            type="submit"
            disabled={pending}
            className="rounded-lg bg-black text-white px-4 py-2 disabled:opacity-60"
          >
            {pending ? "Saving…" : "Save interaction"}
          </button>
        </div>
      </form>

      {/* List */}
      <div className="space-y-2">
        <h4 className="text-sm font-medium">Recent</h4>
        {loading ? (
          <div className="rounded-lg border p-3 animate-pulse bg-white/40">Loading…</div>
        ) : items.length === 0 ? (
          <div className="text-sm text-muted-foreground">No interactions yet.</div>
        ) : (
          <ul className="space-y-2">
            {items.map((it) => (
              <li key={it.id} className="rounded-lg border p-3">
                <div className="text-sm font-medium flex items-center justify-between">
                  <span>{it.type}</span>
                  <span className="text-xs text-muted-foreground">
                    {new Date(it.timestamp).toLocaleString()}
                  </span>
                </div>
                {it.tags?.length ? (
                  <div className="mt-1 flex flex-wrap gap-2">
                    {it.tags.map(t => <span key={t} className="text-[10px] rounded-full border px-2 py-0.5">{t}</span>)}
                  </div>
                ) : null}
                {it.subject && (
                  <p className="text-sm mt-1 text-gray-900">{it.subject}</p>
                )}
                {it.contentPreview && (
                  <p className="text-sm mt-1 text-muted-foreground">{it.contentPreview}</p>
                )}
              </li>
            ))}
          </ul>
        )}
        {!loading && items.length === 0 && (
          <p className="text-xs text-muted-foreground">
            Listing may be unavailable until the interactions list API is enabled; creation still works.
          </p>
        )}
      </div>
    </div>
  );
}
