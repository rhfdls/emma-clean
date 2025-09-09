export const API_URL = process.env.NEXT_PUBLIC_API_URL!;

// RFC7807 ProblemDetails shape (extended friendly typing)
export type Problem = {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  [k: string]: unknown;
};

const TOKEN_KEY = "emma_dev_token";

export function setDevToken(token: string | null) {
  if (typeof window === "undefined") return;
  if (!token) {
    localStorage.removeItem(TOKEN_KEY);
  } else {
    localStorage.setItem(TOKEN_KEY, token);
  }
}

export function getDevToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

function getJwtToken(): string | null {
  if (typeof window === "undefined") return null;
  try {
    return localStorage.getItem("jwt");
  } catch {
    return null;
  }
}

export async function api<T = any>(
  path: string,
  init: RequestInit & { json?: any } = {}
): Promise<T> {
  if (!API_URL) throw new Error("NEXT_PUBLIC_API_URL is not set");
  const { json, ...rest } = init;
  const url = `${API_URL}${path}`;
  let res: Response;
  try {
    const token = getDevToken() || getJwtToken();
    res = await fetch(url, {
      ...rest,
      headers: {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(rest.headers || {}),
      },
      body: json !== undefined ? JSON.stringify(json) : rest.body,
      credentials: "include",
      cache: "no-store",
    });
  } catch (err: any) {
    const msg = err?.message || "Failed to fetch";
    const problem: Problem = { title: "Network error", status: 0, detail: `Network error calling ${url}: ${msg}` };
    throw problem;
  }

  if (!res.ok) {
    const contentType = res.headers.get("content-type") || "";
    if (contentType.includes("application/problem+json")) {
      const problem = (await res.json()) as Problem;
      // Ensure status filled
      if (typeof problem.status !== "number") problem.status = res.status;
      throw problem;
    }
    // Fallback non-problem error
    const text = await res.text().catch(() => res.statusText);
    const fallback: Problem = { title: text || "Unexpected error", status: res.status };
    throw fallback;
  }

  if (res.status === 204) return undefined as T;
  const ct = res.headers.get("content-type") || "";
  if (!ct.includes("application/json")) return undefined as T;
  return (await res.json()) as T;
}

export const apiGet = async <T = any>(path: string, init: RequestInit = {}) =>
  api<T>(path, { ...init, method: "GET" });

export const apiPost = async <T = any>(path: string, body?: unknown, init: RequestInit = {}) =>
  api<T>(path, { ...init, method: "POST", json: body });

export const apiPut = async <T = any>(path: string, body?: unknown, init: RequestInit = {}) =>
  api<T>(path, { ...init, method: "PUT", json: body });
