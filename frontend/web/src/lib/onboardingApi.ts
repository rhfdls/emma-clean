import { apiPost } from "@/lib/api";

export type RegisterOwnerDto = {
  orgName: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
};

export async function registerOwner(body: RegisterOwnerDto) {
  // Backend will validate; extra fields are ignored if not used
  return apiPost<{ verificationToken?: string }>("/api/Onboarding/register", body);
}

export async function verifyEmail(token: string) {
  try {
    return await apiPost<void>("/api/Account/verify", { token });
  } catch {
    // fallback if project uses alternate route name
    return apiPost<void>("/api/auth/verify-email", { token });
  }
}
