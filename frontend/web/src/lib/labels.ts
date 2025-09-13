// Centralized label registry with industry-aware variants.
// Industry can be set with NEXT_PUBLIC_INDUSTRY (e.g., "real_estate", "legal", "general").
// Default: "general".

const INDUSTRY = (process.env.NEXT_PUBLIC_INDUSTRY || "general").toLowerCase();

// Base labels use stable keys that components reference.
// Values are maps of industry -> string, with a "default" fallback.
const LABELS: Record<string, Record<string, string>> = {
  contactOwner: {
    real_estate: "Assigned Agent",
    legal: "Client Owner",
    general: "Contact Owner",
    default: "Contact Owner",
  },
};

export type IndustryKey = "real_estate" | "legal" | "general" | string;

export function getLabel(key: keyof typeof LABELS, industry?: IndustryKey): string {
  const i = (industry || INDUSTRY || "general").toLowerCase();
  const map = LABELS[key] || {};
  return map[i] || map.default || key;
}

export function getCurrentIndustry(): IndustryKey {
  return INDUSTRY as IndustryKey;
}
