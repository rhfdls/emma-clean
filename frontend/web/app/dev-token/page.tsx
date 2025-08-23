"use client";
import { useEffect, useState } from "react";
import PageContainer from "@/components/ui/PageContainer";
import { Card, CardContent, CardHeader } from "@/components/ui/Card";
import Label from "@/components/ui/Label";
import Input from "@/components/ui/Input";
import Button from "@/components/ui/Button";
import { getDevToken, setDevToken } from "@/lib/api";

export default function DevTokenPage() {
  const [token, setToken] = useState("");
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    const t = getDevToken();
    if (t) setToken(t);
  }, []);

  function save() {
    setDevToken(token || null);
    alert("Dev token saved to localStorage.");
  }

  function clear() {
    setToken("");
    setDevToken(null);
    alert("Dev token cleared.");
  }

  async function copy() {
    try {
      await navigator.clipboard.writeText(token);
      setCopied(true);
      setTimeout(() => setCopied(false), 1200);
    } catch {}
  }

  return (
    <main className="min-h-dvh bg-neutral-50">
      <PageContainer className="py-6">
        <Card>
          <CardHeader>
            <h1 className="text-xl font-semibold text-gray-900">Developer Token Helper</h1>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-gray-700">
              Store a JWT for local development. Requests via the api() helper will include it as
              <code className="mx-1 rounded bg-gray-100 px-1">Authorization: Bearer &lt;token&gt;</code>.
            </p>
            <div>
              <Label htmlFor="token">Token</Label>
              <Input
                id="token"
                placeholder="paste JWT here"
                value={token}
                onChange={(e) => setToken(e.target.value)}
              />
            </div>
            <div className="flex gap-2">
              <Button onClick={save}>Save</Button>
              <Button onClick={clear} className="bg-gray-800 hover:bg-gray-900">
                Clear
              </Button>
              <Button onClick={copy} className="bg-gray-600 hover:bg-gray-700">
                {copied ? "Copied" : "Copy"}
              </Button>
            </div>
            <div className="text-xs text-gray-600">
              API base: <code>{process.env.NEXT_PUBLIC_API_URL || "(unset)"}</code>
            </div>
          </CardContent>
        </Card>
      </PageContainer>
    </main>
  );
}
