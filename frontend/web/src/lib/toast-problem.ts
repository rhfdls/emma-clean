import { toast } from "sonner";
import type { Problem } from "./api";

export function toastProblem(p: Problem) {
  const title = p.title || `Error ${p.status ?? ""}`.trim();
  const description = p.detail || p.type || "";
  toast.error(title, description ? { description } : undefined);
}
