import { apiGet, apiPost } from "@/lib/api";
import type { Problem } from "@/lib/api";

export type InteractionCreateDto = {
  type: "call" | "email" | "sms" | "meeting" | "note" | "task" | "other";
  direction?: "inbound" | "outbound" | "system";
  timestamp: string; // ISO 8601
  subject?: string;
  privacyLevel: "public" | "internal" | "private" | "confidential";
  tags?: string[];
  content?: string; // keep <= 8000 chars
  agentId?: string;
};

export type InteractionReadDto = {
  id: string;
  timestamp: string; // ISO
  type: string;
  subject?: string | null;
  contentPreview?: string;
  tags?: string[];
};

const MAX_CONTENT = 8000;

// Shapes returned by the backend list endpoint
// GET /api/contacts/{contactId}/interactions returns items with:
// { id, timestamp, type, subject, content, analysisSummary }
// We'll shape to InteractionReadDto.

type BackendInteractionListItem = {
  id: string;
  timestamp: string;
  type: string;
  subject?: string | null;
  content?: string | null;
  analysisSummary?: string | null;
};

export async function listInteractions(contactId: string): Promise<InteractionReadDto[]> {
  try {
    const data = await apiGet<BackendInteractionListItem[]>(`/api/contacts/${contactId}/interactions`);
    return (data || []).map((i) => ({
      id: i.id,
      timestamp: i.timestamp,
      type: i.type,
      subject: i.subject ?? undefined,
      contentPreview: i.content ? i.content.slice(0, 160) : undefined,
      // tags are not projected by the backend list currently
    }));
  } catch (e: any) {
    const p = e as Problem;
    if (p && p.status === 404) return [];
    throw e;
  }
}

export async function createInteraction(contactId: string, dto: InteractionCreateDto) {
  if (dto.content && dto.content.length > MAX_CONTENT) {
    const err: Problem = {
      type: "/problems/unprocessable",
      title: "Content too large",
      status: 422,
      detail: `Please keep interaction content ≤ ${MAX_CONTENT} characters.`,
    } as any;
    throw err;
  }

  // Map to backend expected shape: occurredAt + privacyLevel + tags
  const body: any = {
    type: dto.type,
    direction: dto.direction,
    subject: dto.subject,
    content: dto.content,
    occurredAt: dto.timestamp,
    privacyLevel: dto.privacyLevel,
    tags: dto.tags,
  };

  try {
    // Preferred nested route
    const res = await apiPost<{ interactionId: string }>(`/api/contacts/${contactId}/interactions`, body);
    return { id: res?.interactionId };
  } catch (e: any) {
    const p = e as Problem;
    if (!p || p.status !== 404) throw e;
    // Legacy fallback: if ever needed (not expected with current backend)
    const res = await apiPost<{ interactionId: string }>(`/api/Interaction/log`, { contactId, ...body });
    return { id: res?.interactionId };
  }
}
