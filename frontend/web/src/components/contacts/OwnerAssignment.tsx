"use client";

import { useMemo, useState } from "react";
import { assignOwner } from "@/lib/contactsApi";
import { toast } from "sonner";
import { toastProblem } from "@/lib/toast-problem";
import { getCurrentUserIdFromJwt } from "@/lib/jwt";
import { getLabel } from "@/lib/labels";

type Props = {
  contactId: string;
  currentOwnerId?: string | null;
  onAssigned?: (newOwnerId: string) => void;
};

export default function OwnerAssignment({ contactId, currentOwnerId, onAssigned }: Props) {
  const [ownerId, setOwnerId] = useState<string>(currentOwnerId ?? "");
  const [pending, setPending] = useState(false);
  const assignedByAgentId = useMemo(() => getCurrentUserIdFromJwt(), []);
  const LABEL = getLabel("contactOwner"); // Vertical-friendly label (e.g., Assigned Agent / Contact Owner / Client Owner)

  async function onAssign(e: React.FormEvent) {
    e.preventDefault();
    if (!ownerId) {
      toast.error("Owner user ID is required");
      return;
    }
    if (!assignedByAgentId) {
      toast.error("Signed-in user not found in token");
      return;
    }
    setPending(true);
    try {
      await assignOwner(contactId, { userId: ownerId, assignedByAgentId });
      toast.success("Owner updated");
      onAssigned?.(ownerId);
    } catch (e: any) {
      toastProblem(e);
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="rounded-lg border bg-white p-4 space-y-3">
      <h2 className="text-base font-semibold">{LABEL}</h2>
      <form onSubmit={onAssign} className="grid gap-2">
        <label className="grid gap-1">
          <span className="text-sm font-medium">{LABEL} User ID</span>
          <input
            className="border rounded-lg px-3 py-2"
            placeholder="00000000-0000-0000-0000-000000000000"
            value={ownerId}
            onChange={(e) => setOwnerId(e.target.value)}
          />
        </label>
        <div>
          <button
            type="submit"
            disabled={pending}
            className="rounded-lg bg-black text-white px-4 py-2 disabled:opacity-60"
          >
            {pending ? "Assigning…" : `Assign ${LABEL.toLowerCase()}`}
          </button>
        </div>
      </form>
      <p className="text-xs text-muted-foreground">This uses PUT /api/Contact/{"{id}"}/assign. Your user ID is read from JWT (assignedByAgentId).</p>
    </div>
  );
}
