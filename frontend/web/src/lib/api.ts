export const API_URL = process.env.NEXT_PUBLIC_API_URL!;

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

export async function api<T = any>(
  path: string,
  init: RequestInit & { json?: any } = {}
): Promise<T> {
  if (!API_URL) throw new Error("NEXT_PUBLIC_API_URL is not set");
  const { json, ...rest } = init;
  const url = `${API_URL}${path}`;
  let res: Response;
  try {
    const token = getDevToken();
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
    // Surface network issues clearly (CORS, DNS, refused connection, etc.)
    const msg = err?.message || "Failed to fetch";
    throw new Error(`Network error calling ${url}: ${msg}`);
  }
  if (!res.ok) {
    const msg = await res.text().catch(() => res.statusText);
    const error: any = new Error(msg || `HTTP ${res.status}`);
    (error.status = res.status);
    throw error;
  }
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}
