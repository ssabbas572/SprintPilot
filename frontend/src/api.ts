export type TestCase = {
  kind: string;
  scenario: string;
  expected: string;
  result: string;
};
export type Content = {
  title: string;
  description: string;
  priority: string;
  points: number;
  acceptance: string[];
  tasks: string[];
  tests: TestCase[];
};
export type Story = {
  id: number;
  code: string;
  proposalId: string | null;
  content: Content;
  status: string;
  assignee: string;
  blocked: boolean;
  blocker: string;
  version: number;
  createdAt: string;
  updatedAt: string;
};
export type Plan = { summary: string; questions: string[]; stories: Content[] };
export type Proposal = {
  id: string;
  title: string;
  source: string;
  state: string;
  version: number;
  provider: string;
  error: string | null;
  plan: Plan | null;
  createdAt: string;
};
export type User = { email: string; provider: string };
export type Detail = {
  story: Story;
  activity: { id: number; text: string; at: string }[];
};
export type Summary = {
  generatedAt: string;
  total: number;
  done: number;
  inProgress: number;
  inReview: number;
  backlog: number;
  points: number;
  completedPoints: number;
  passed: number;
  failed: number;
  notRun: number;
  completed: Story[];
  blockers: Story[];
};
export async function api<T>(
  url: string,
  data?: unknown,
  method?: string,
): Promise<T> {
  const response = await fetch("/api" + url, {
    method: method ?? (data === undefined ? "GET" : "POST"),
    credentials: "same-origin",
    headers: { "Content-Type": "application/json", "X-SprintPilot": "1" },
    body: data === undefined ? undefined : JSON.stringify(data),
  });
  if (!response.ok) {
    let message =
      response.status === 401
        ? "Please sign in again."
        : `Request failed (${response.status}).`;
    try {
      const e = await response.json();
      message = e.error ?? e.title ?? message;
    } catch {}
    throw new Error(message);
  }
  return response.status === 204 ? (undefined as T) : response.json();
}
