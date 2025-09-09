import { apiPut } from "@/lib/api";

export async function assignOwner(
  contactId: string,
  params: { userId: string; assignedByAgentId: string }
): Promise<void> {
  const body = {
    contactId,
    userId: params.userId,
    assignedByAgentId: params.assignedByAgentId,
  };
  await apiPut<void>(`/api/Contact/${contactId}/assign`, body);
}
