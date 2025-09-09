import { apiGet, apiPost } from "@/lib/api";

export async function createInvite(orgId: string, email: string, role: string = "Agent") {
  return apiPost<{ token: string }>(`/api/Organization/${orgId}/invitations`, { email, role });
}

export async function getInvite(token: string) {
  return apiGet<{ status: string; orgId: string; email?: string; role?: string }>(
    `/api/Organization/invitations/${token}`
  );
}

export async function registerViaInvite(
  token: string,
  body: { firstName: string; lastName: string; email: string; password: string }
) {
  return apiPost<void>(`/api/Organization/invitations/${token}/register`, body);
}
