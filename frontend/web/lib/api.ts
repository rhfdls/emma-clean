// Re-export unified API from src/lib/api to prevent duplication.
export { api, API_URL } from "@/lib/api";
export type { Problem } from "@/lib/api";

// Legacy shims to ease migration where apiGet/apiPost were used.
export async function apiGet<T = any>(path: string, init?: RequestInit): Promise<T> {
  return await (await import("@/lib/api")).api<T>(path, { ...init, method: "GET" });
}

export async function apiPost<T = any>(path: string, body?: any, init?: RequestInit): Promise<T> {
  return await (await import("@/lib/api")).api<T>(path, { ...init, method: "POST", json: body });
}
