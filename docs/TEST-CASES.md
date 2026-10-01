# Test cases

## Automated backend suite — 32 cases

Run `dotnet test SprintPilot.sln -c Release`. Independent temporary SQLite databases and injected provider adapters isolate each integration test.

| Area | Cases | Assertions |
|---|---:|---|
| Validation / Mock | 13 | Valid plan; three invalid estimates; required test kinds; no proposed Passed results; required criteria; six-story limit; blocker reason; blocked Done rejection; valid column names; Markdown escaping; source-line template behavior |
| API / worker | 14 | Authentication, mutation header, draft-only generation, idempotent approval, proposal version/state rules, account isolation, versioned/logged moves, blocker workflow, saved test summary/export, two invalid brief inputs, invalid plan rejection, bounded failure/manual recovery, expired lease recovery |
| OpenAI contract | 5 | Strict schema/store flag; valid plan; two incomplete statuses; refusal; sanitized HTTP errors |
| **Total** | **32** | Parameterized inputs count separately |

## Automated browser workflow

The Playwright test signs into the empty account and:

1. Submits a unique one-line brief.
2. Waits for background generation.
3. Edits a proposed story title.
4. Approves it onto the board.
5. Uses the inspector to set In progress, an assignee and one Passed test result.
6. Saves and drags the card to Done.
7. Reloads and verifies status/test persistence.
8. Downloads the Markdown export.

Run with the API in Mock mode. Playwright starts Vite. Use `npx playwright install chromium` first. The workflow leaves an Applied proposal and story in the test account; use disposable demo data.

## Manual acceptance scenarios

These are a follow-up checklist, not a claim that every case has been executed.

| ID | Scenario | Expected |
|---|---|---|
| M01 | Empty account | Empty columns, no invented progress |
| M02 | Generate a multi-line Mock brief | Up to three generic proposed stories; no board mutation |
| M03 | Generate using live OpenAI | Relevant proposed scope, questions and Not run tests; human review required |
| M04 | Add/remove proposal stories | 1–6 stories permitted; zero rejected |
| M05 | Blank criteria/test scenario | Save rejects invalid plan |
| M06 | Two tabs save same proposal version | Stale edit/approval returns conflict |
| M07 | Repeat Applied approval request | Same IDs; story count unchanged |
| M08 | Invalid/foreign story ID | 404; no cross-account data |
| M09 | Move blocked story to Done | Rejected; source column retained |
| M10 | Record Failed while story is Done | Allowed; summary shows done and failed separately |
| M11 | Restart during generation | Expired lease recovered with bounded attempts |
| M12 | Provider timeout/429 | Bounded retries; sanitized Failed state |
| M13 | 390px viewport | Page fits; board scrolls internally; inspector usable |
| M14 | Keyboard navigation | Story button opens inspector; focus trapped; Escape closes; status select works |
| M15 | Export Unicode / Markdown-like text | Attachment preserves text safely without raw HTML execution |
| M16 | Hit 30 briefs / 100 stories | Quota prevents further additions |
| M17 | Reload an edited proposal | Explicit action discards local edits and adopts current server version |

## Future planning-quality evaluation

Build a held-out requirements set and have a reviewer score scope coverage, unsupported assumptions, clarity of acceptance criteria, test relevance, missing failure paths and estimate usefulness. Include ambiguous, contradictory, long and instruction-injection briefs. Track provider/model/prompt version and cost/latency. Current tests verify workflow contracts, not real-model planning accuracy.
