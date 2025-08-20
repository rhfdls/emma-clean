export const API_URL = process.env.NEXT_PUBLIC_API_URL!;

export async function api<T = any>(
  path: string,
  init: RequestInit & { json?: any } = {}
): Promise<T> {
  if (!API_URL) throw new Error("NEXT_PUBLIC_API_URL is not set");
  const { json, ...rest } = init;
  const url = `${API_URL}${path}`;
  let res: Response;
  try {
    res = await fetch(url, {
      ...rest,
      headers: {
        "Content-Type": "application/json",
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
    throw new Error(msg || `HTTP ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}
