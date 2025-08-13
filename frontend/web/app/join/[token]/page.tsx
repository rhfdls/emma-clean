"use client";
import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { apiGet, apiPost } from "@/lib/api";

interface InvitationDto {
  id: string;
  organizationId: string;
  email: string;
  role: string;
  token: string;
  expiresAt?: string;
  acceptedAt?: string;
  revokedAt?: string;
  isActive: boolean;
}

export default function JoinByTokenPage() {
  const { token } = useParams<{ token: string }>();
  const router = useRouter();
  const [loading, setLoading] = useState(true);
  const [invitation, setInvitation] = useState<InvitationDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [accepting, setAccepting] = useState(false);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        setLoading(true);
        setError(null);
        const data = await apiGet<InvitationDto>(`/api/organization/invitations/${token}`);
        if (active) setInvitation(data);
      } catch (e: any) {
        if (active) setError(e?.message || "Invitation not found.");
      } finally {
        if (active) setLoading(false);
      }
    }
    if (token) load();
    return () => {
      active = false;
    };
  }, [token]);

  async function accept() {
    try {
      setAccepting(true);
      setError(null);
      await apiPost(`/api/organization/invitations/${token}/accept`, {});
      router.push("/onboarding");
    } catch (e: any) {
      setError(e?.message || "Failed to accept invitation.");
    } finally {
      setAccepting(false);
    }
  }

  return (
    <main className="min-h-dvh bg-white">
      <div className="mx-auto max-w-xl p-6 md:py-12">
        {loading ? (
          <div>Loading invitation…</div>
        ) : error ? (
          <div className="rounded border border-red-300 bg-red-50 p-3 text-red-700">{error}</div>
        ) : invitation ? (
          <div className="space-y-4">
            <h1 className="text-2xl font-semibold">Join organization</h1>
            <div className="text-sm text-gray-700">
              Email: <b>{invitation.email}</b>
            </div>
            {!invitation.isActive && (
              <div className="rounded border border-amber-300 bg-amber-50 p-3 text-amber-800 text-sm">
                This invitation is not active (revoked, accepted, or expired).
              </div>
            )}
            <button
              onClick={accept}
              disabled={accepting || !invitation.isActive}
              className="rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700 disabled:opacity-60"
            >
              {accepting ? "Joining…" : "Accept Invitation"}
            </button>
          </div>
        ) : null}
      </div>
    </main>
  );
}
