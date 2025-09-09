export function normalizeTags(input: string): string[] {
  const arr = input
    .split(",")
    .map((t) => t.trim())
    .filter(Boolean)
    .map((t) => t.toLowerCase());
  return Array.from(new Set(arr));
}

export function stringifyTags(tags?: string[]): string {
  if (!tags || tags.length === 0) return "";
  return tags.join(", ");
}
