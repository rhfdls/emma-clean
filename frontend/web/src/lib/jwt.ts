export function getCurrentUserIdFromJwt(): string | null {
  if (typeof window === "undefined") return null;
  const token = localStorage.getItem("jwt") || localStorage.getItem("emma_dev_token");
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    const json = atob(parts[1].replace(/-/g, "+").replace(/_/g, "/"));
    const payload = JSON.parse(json);
    return payload.userId ?? payload.uid ?? payload.sub ?? null;
  } catch {
    return null;
  }
}

export function getOrgIdFromJwt(): string | null {
  if (typeof window === "undefined") return null;
  const token = localStorage.getItem("jwt") || localStorage.getItem("emma_dev_token");
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    const json = atob(parts[1].replace(/-/g, "+").replace(/_/g, "/"));
    const payload = JSON.parse(json);
    // Server expects claim name orgId
    return payload.orgId ?? payload.organizationId ?? payload.tenantId ?? null;
  } catch {
    return null;
  }
}
