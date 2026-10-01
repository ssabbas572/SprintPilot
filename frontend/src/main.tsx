import React, { useEffect, useRef, useState } from "react";
import { createRoot } from "react-dom/client";
import {
  ArrowUpRight,
  ArrowRight,
  ArrowLeft,
  Plus,
  X,
  Check,
  CheckCheck,
  Search,
  LayoutGrid,
  FilePenLine,
  FlaskConical,
  AlignLeft,
  Sparkles,
  ChevronDown,
  ChevronRight,
  GripVertical,
  Flag,
  Clock3,
  LogOut,
  Download,
  LoaderCircle,
  Circle,
  MessageSquare,
  CheckSquare,
  TriangleAlert,
  RefreshCw,
  Send,
  MoveRight,
  Route,
} from "lucide-react";
import {
  api,
  type Story,
  type Content,
  type Proposal,
  type Plan,
  type User,
  type Detail,
  type Summary,
} from "./api";
import "./styles.css";
const columns = ["Backlog", "In progress", "In review", "Done"];
const priorities = ["Low", "Medium", "High"];
const kinds = ["Positive", "Negative", "Boundary"];
const results = ["Not run", "Passed", "Failed"];
const sample =
  "Customers should be able to book an appointment by selecting a provider, date and available time.\nCustomers should be able to reschedule an existing appointment without creating a duplicate booking.\nAfter a successful booking, show a confirmation with the appointment details.";
const newStory = (): Content => ({
  title: "Define the next user capability",
  description:
    "As a user, I want a clearly defined capability so I can complete the agreed workflow.",
  priority: "Medium",
  points: 3,
  acceptance: ["Define an observable acceptance criterion."],
  tasks: ["Confirm the implementation approach."],
  tests: kinds.map((kind) => ({
    kind,
    scenario: "Describe the " + kind.toLowerCase() + " scenario.",
    expected: "Describe the observable expected result.",
    result: "Not run",
  })),
});
function Logo() {
  return (
    <span className="logo">
      <span className="logo-mark">
        <Route size={23} />
      </span>
      sprintpilot
    </span>
  );
}
function Initials({ name }: { name: string }) {
  return (
    <span className={"avatar " + (name ? "filled" : "")}>
      {name
        ? name
            .split(" ")
            .map((s) => s[0])
            .slice(0, 2)
            .join("")
        : "—"}
    </span>
  );
}
function useDialog(close: () => void) {
  const ref = useRef(close);
  ref.current = close;
  useEffect(() => {
    const prev = document.activeElement as HTMLElement;
    const get = () =>
      Array.from(
        document.querySelectorAll<HTMLElement>(
          '[role="dialog"] button:not([disabled]),[role="dialog"] input,[role="dialog"] textarea,[role="dialog"] select,[role="dialog"] a',
        ),
      );
    get()[0]?.focus();
    const key = (e: KeyboardEvent) => {
      if (e.key === "Escape") ref.current();
      if (e.key === "Tab") {
        const a = get();
        if (e.shiftKey && document.activeElement === a[0]) {
          e.preventDefault();
          a.at(-1)?.focus();
        } else if (!e.shiftKey && document.activeElement === a.at(-1)) {
          e.preventDefault();
          a[0]?.focus();
        }
      }
    };
    document.addEventListener("keydown", key);
    const old = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = old;
      document.removeEventListener("keydown", key);
      prev?.focus();
    };
  }, []);
}
function App() {
  const [user, setUser] = useState<User | null>(null),
    [loaded, setLoaded] = useState(false);
  useEffect(() => {
    api<User>("/auth/me")
      .then(setUser)
      .catch(() => {})
      .finally(() => setLoaded(true));
  }, []);
  return !loaded ? (
    <div className="loading">
      <LoaderCircle className="spin" />
      Opening your workspace…
    </div>
  ) : user ? (
    <Workspace user={user} logout={() => setUser(null)} />
  ) : (
    <Login done={setUser} />
  );
}
function Login({ done }: { done: (u: User) => void }) {
  const [email, setEmail] = useState("demo@sprintpilot.local"),
    [password, setPassword] = useState("DemoPass123!"),
    [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  return (
    <div className="login">
      <header>
        <Logo />
        <span>A LITTLE STRUCTURE. A LOT OF POSSIBILITY.</span>
      </header>
      <main className="login-main">
        <section className="login-story">
          <span className="eyebrow">MEET YOUR NEXT CHAPTER</span>
          <h1>
            From a spark
            <br />
            to <em>shipped.</em>
          </h1>
          <p>
            Give your ideas a place to grow.
            <br />
            Turn a brief into a plan, and a plan into progress.
          </p>
          <div className="paper-stack">
            <div className="paper-one">
              <span>01 / THE IDEA</span>
              <p>
                “What if booking
                <br />
                was effortless?”
              </p>
              <FilePenLine size={24} />
            </div>
            <div className="paper-two">
              <span>02 / THE PLAN</span>
              <strong>Make it happen.</strong>
              <p>
                <Check size={15} />
                User stories
              </p>
              <p>
                <Check size={15} />
                Acceptance criteria
              </p>
              <p>
                <Check size={15} />A clear path forward
              </p>
            </div>
            <span className="paper-caption">Less blank-page energy.</span>
          </div>
        </section>
        <form
          className="login-form"
          onSubmit={async (e) => {
            e.preventDefault();
            setBusy(true);
            setError("");
            try {
              await api("/auth/login", { email, password });
              done(await api<User>("/auth/me"));
            } catch (e) {
              setError((e as Error).message);
            } finally {
              setBusy(false);
            }
          }}
        >
          <span className="step-label">YOUR WORKSPACE AWAITS</span>
          <h2>Let’s make a little progress.</h2>
          <p>Sign in to pick up where the ideas left off.</p>
          <label>
            Email address
            <input
              autoComplete="username"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </label>
          <label>
            Password
            <input
              autoComplete="current-password"
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </label>
          {error && (
            <div className="error" role="alert">
              {error}
            </div>
          )}
          <button className="primary wide" disabled={busy}>
            {busy ? "Opening…" : "Step inside"}
            <ArrowRight size={17} />
          </button>
          <div className="demo">
            <strong>Take it for a spin.</strong>
            <p>
              Both demo accounts use <code>DemoPass123!</code>
            </p>
            <button
              type="button"
              onClick={() => setEmail("demo@sprintpilot.local")}
            >
              Sample board <ArrowUpRight size={13} />
            </button>
            <button
              type="button"
              onClick={() => setEmail("second@sprintpilot.local")}
            >
              Fresh start <ArrowUpRight size={13} />
            </button>
          </div>
        </form>
      </main>
      <footer>
        .NET 9 · REACT · OPENAI{" "}
        <span>Thoughtful planning. Human decisions.</span>
      </footer>
    </div>
  );
}
function Workspace({ user, logout }: { user: User; logout: () => void }) {
  const [tab, setTab] = useState("Board"),
    [stories, setStories] = useState<Story[]>([]),
    [proposals, setProposals] = useState<Proposal[]>([]),
    [selected, setSelected] = useState<number | null>(null),
    [search, setSearch] = useState(""),
    [priority, setPriority] = useState("All"),
    [blockedOnly, setBlockedOnly] = useState(false),
    [busy, setBusy] = useState(false),
    [drag, setDrag] = useState<number | null>(null),
    [over, setOver] = useState(""),
    [error, setError] = useState(""),
    [notice, setNotice] = useState("");
  async function refresh() {
    const [s, p] = await Promise.all([
      api<Story[]>("/board"),
      api<Proposal[]>("/proposals"),
    ]);
    setStories(s);
    setProposals(p);
  }
  useEffect(() => {
    void refresh().catch((e) => setError(e.message));
    const id = setInterval(
      () => void refresh().catch((e) => setError(e.message)),
      4000,
    );
    return () => clearInterval(id);
  }, []);
  async function move(id: number, status: string) {
    if (busy) return;
    const story = stories.find((s) => s.id === id);
    if (!story || story.status === status) return;
    setBusy(true);
    setError("");
    try {
      await api("/stories/" + id + "/move", { version: story.version, status });
      await refresh();
      setNotice(`${story.code} moved to ${status}.`);
    } catch (e) {
      setError((e as Error).message);
      await refresh();
    } finally {
      setBusy(false);
      setDrag(null);
      setOver("");
    }
  }
  const visible = stories.filter(
    (s) =>
      (priority === "All" || s.content.priority === priority) &&
      (!blockedOnly || s.blocked) &&
      `${s.code} ${s.content.title} ${s.assignee}`
        .toLowerCase()
        .includes(search.toLowerCase()),
  );
  const done = stories.filter((s) => s.status === "Done").length;
  const ready = proposals.filter((p) => p.state === "Ready").length;
  return (
    <div className="app">
      <header className="topnav">
        <a href="/" aria-label="SprintPilot home">
          <Logo />
        </a>
        <nav>
          {[
            { name: "Board", icon: LayoutGrid },
            { name: "Brief studio", icon: FilePenLine },
            { name: "Test plan", icon: FlaskConical },
            { name: "Summary", icon: AlignLeft },
          ].map((n) => (
            <button
              key={n.name}
              className={tab === n.name ? "active" : ""}
              onClick={() => setTab(n.name)}
            >
              <n.icon size={16} />
              {n.name}
              {n.name === "Brief studio" && ready > 0 && <b>{ready}</b>}
            </button>
          ))}
        </nav>
        <div className="account">
          <span className="provider">
            <i />
            {user.provider === "Mock" ? "Mock mode" : "OpenAI mode"}
          </span>
          <span className="user-avatar" title={user.email}>
            SP
          </span>
          <button
            className="icon"
            aria-label="Sign out"
            onClick={async () => {
              try {
                await api("/auth/logout", {});
                logout();
              } catch (e) {
                setError((e as Error).message);
              }
            }}
          >
            <LogOut size={16} />
          </button>
        </div>
      </header>
      <div className="workspace-context">
        <span>
          PERSONAL WORKSPACE <ChevronRight size={11} /> CUSTOMER EXPERIENCE
        </span>
        <span>YOUR IDEAS. YOUR PACE.</span>
      </div>
      {error && (
        <div className="error toast" role="alert">
          {error}
          <button
            className="icon"
            aria-label="Dismiss error"
            onClick={() => setError("")}
          >
            <X size={16} />
          </button>
        </div>
      )}
      {notice && (
        <div className="notice toast" role="status">
          {notice}
          <button
            className="icon"
            aria-label="Dismiss notice"
            onClick={() => setNotice("")}
          >
            <X size={16} />
          </button>
        </div>
      )}
      {tab === "Board" ? (
        <main className="board-page">
          <section className="hero">
            <div>
              <span className="eyebrow">
                <span /> A LITTLE PLAN GOES A LONG WAY
              </span>
              <h1>
                Good ideas.
                <br />
                <em>Great execution.</em>
              </h1>
              <p>
                A shared direction for every detail. Move the work forward, one
                story at a time.
              </p>
            </div>
            <div className="hero-right">
              <div className="margin-note">
                <span className="note-star">✳</span>
                <p>
                  Big thinking.
                  <br />
                  Small, shippable steps.
                </p>
                <span className="note-line" />
              </div>
              <button
                className="primary"
                onClick={() => setTab("Brief studio")}
              >
                <Sparkles size={17} />
                Turn a brief into a plan
                <ArrowUpRight size={16} />
              </button>
              <small>AI drafts. You decide what makes the board.</small>
            </div>
          </section>
          <section className="board-toolbar">
            <div className="board-heading">
              <LayoutGrid size={17} />
              <h2>Delivery board</h2>
              <span>{stories.length} stories</span>
              <div className="progress">
                <i
                  style={{
                    width: `${stories.length ? (done / stories.length) * 100 : 0}%`,
                  }}
                />
              </div>
              <small>
                {done}/{stories.length} done
              </small>
            </div>
            <div className="filters">
              <label className="search">
                <Search size={15} />
                <input
                  aria-label="Search stories"
                  placeholder="Find a story…"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </label>
              <select
                aria-label="Priority filter"
                value={priority}
                onChange={(e) => setPriority(e.target.value)}
              >
                <option value="All">All priorities</option>
                {priorities.map((p) => (
                  <option key={p}>{p}</option>
                ))}
              </select>
              <button
                className={"filter-button " + (blockedOnly ? "selected" : "")}
                aria-pressed={blockedOnly}
                onClick={() => setBlockedOnly(!blockedOnly)}
              >
                <Flag size={14} />
                Blocked
              </button>
            </div>
          </section>
          <div className="board" aria-label="Delivery board">
            {columns.map((col, n) => {
              const list = visible.filter((s) => s.status === col);
              return (
                <section
                  className={
                    "column col" + n + (over === col ? " drop-target" : "")
                  }
                  key={col}
                  data-column={col}
                  aria-label={col + " column"}
                  onDragOver={(e) => {
                    e.preventDefault();
                    if (drag !== null) setOver(col);
                  }}
                  onDragLeave={(e) => {
                    if (!e.currentTarget.contains(e.relatedTarget as Node))
                      setOver("");
                  }}
                  onDrop={(e) => {
                    e.preventDefault();
                    const id = Number(e.dataTransfer.getData("text/plain"));
                    if (id) void move(id, col);
                    setOver("");
                  }}
                >
                  <div className="column-heading">
                    <span className="column-dot" />
                    <h3>{col}</h3>
                    <span className="column-count">{list.length}</span>
                    <span className="column-hint">
                      {
                        [
                          "IDEAS IN WAITING",
                          "MAKING IT HAPPEN",
                          "ONE LAST LOOK",
                          "OVER THE LINE",
                        ][n]
                      }
                    </span>
                  </div>
                  <div className="cards">
                    {list.map((s) => (
                      <article
                        className={
                          "story-card " + (drag === s.id ? "dragging" : "")
                        }
                        key={s.id}
                        data-story={s.id}
                        draggable={!busy}
                        onDragStart={(e) => {
                          setDrag(s.id);
                          e.dataTransfer.setData("text/plain", String(s.id));
                          e.dataTransfer.effectAllowed = "move";
                        }}
                        onDragEnd={() => {
                          setDrag(null);
                          setOver("");
                        }}
                      >
                        <div className="card-meta">
                          <span>{s.code}</span>
                          <span
                            className={
                              "priority " + s.content.priority.toLowerCase()
                            }
                          >
                            <i />
                            {s.content.priority}
                          </span>
                          <GripVertical size={13} />
                        </div>
                        <button
                          className="card-title"
                          onClick={() => setSelected(s.id)}
                        >
                          {s.content.title}
                        </button>
                        <p className="card-description">
                          {s.content.description}
                        </p>
                        {s.blocked && (
                          <div className="blocked-label">
                            <Flag size={12} />
                            Waiting on a decision
                          </div>
                        )}
                        <div className="card-kind">
                          <span className="story-type">User story</span>
                          <span>{s.content.points} pts</span>
                        </div>
                        <div className="card-bottom">
                          <div>
                            <span title="Acceptance criteria">
                              <CheckSquare size={13} />
                              {s.content.acceptance.length}
                            </span>
                            <span title="Test cases">
                              <FlaskConical size={13} />
                              {s.content.tests.length}
                            </span>
                          </div>
                          <span className="assignee-name">
                            {s.assignee
                              ? s.assignee.split(" ")[0]
                              : "Unassigned"}
                          </span>
                          <Initials name={s.assignee} />
                        </div>
                      </article>
                    ))}
                    {!list.length && (
                      <div className="column-empty">
                        <Circle size={19} />
                        <p>
                          {stories.length
                            ? "A little room to move."
                            : "Your next idea belongs here."}
                        </p>
                        <span>
                          {stories.length
                            ? "Drag a story here or change its status in the inspector."
                            : "Start with a brief to build your first plan."}
                        </span>
                      </div>
                    )}
                  </div>
                  <button
                    className="column-add"
                    onClick={() => setTab("Brief studio")}
                  >
                    <Plus size={14} />
                    Plan new work
                  </button>
                </section>
              );
            })}
          </div>
          <div className="board-foot">
            <span>
              <GripVertical size={13} />
              Drag stories between columns. Open a story for keyboard-accessible
              status changes.
            </span>
            <span>
              <span className="live-dot" />
              Saved to your workspace
            </span>
          </div>
        </main>
      ) : tab === "Brief studio" ? (
        <BriefStudio
          proposals={proposals}
          refresh={refresh}
          provider={user.provider}
          board={() => setTab("Board")}
        />
      ) : tab === "Test plan" ? (
        <TestPlan stories={stories} open={setSelected} />
      ) : (
        <SummaryPage open={setSelected} />
      )}
      <footer className="site-footer">
        <Logo />
        <span>LESS GUESSWORK. MORE FORWARD.</span>
        <div>
          <a href="/api/export?format=md">
            <Download size={13} />
            Markdown
          </a>
          <a href="/api/export?format=json">
            JSON <ArrowUpRight size={13} />
          </a>
        </div>
      </footer>
      {selected !== null && (
        <Inspector
          id={selected}
          close={() => setSelected(null)}
          changed={refresh}
        />
      )}
    </div>
  );
}
function BriefStudio({
  proposals,
  refresh,
  provider,
  board,
}: {
  proposals: Proposal[];
  refresh: () => Promise<void>;
  provider: string;
  board: () => void;
}) {
  const [active, setActive] = useState<string | null>(null),
    [title, setTitle] = useState("A better appointment experience"),
    [source, setSource] = useState(sample),
    [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  const current = proposals.find((p) => p.id === active);
  return (
    <main className="studio-page">
      <div className="section-intro">
        <span className="eyebrow">THE BRIEF STUDIO</span>
        <h1>
          Make room for <em>the idea.</em>
        </h1>
        <p>
          Start with what you know. Find the questions. Shape the next step.
        </p>
      </div>
      <div className="brief-history">
        <button
          className={!active ? "active" : ""}
          onClick={() => setActive(null)}
        >
          <Plus size={14} />
          New brief
        </button>
        {proposals.map((p) => (
          <button
            className={active === p.id ? "active" : ""}
            key={p.id}
            onClick={() => setActive(p.id)}
          >
            <span className={"state-dot " + p.state.toLowerCase()} />
            {p.title}
            <small>{p.state}</small>
          </button>
        ))}
      </div>
      {error && (
        <div className="error" role="alert">
          {error}
        </div>
      )}
      <div className="studio-grid">
        <section className="brief-input">
          <div className="panel-label">
            <span>01</span>
            <h2>The raw material</h2>
            <FilePenLine size={19} />
          </div>
          {current ? (
            <>
              <h3>{current.title}</h3>
              <p className="brief-state">
                {current.state} · {current.provider || provider}
              </p>
              <pre className="source-brief">{current.source}</pre>
              <div className="note-box">
                Original requirements are kept with this proposal. To change the
                source, create a new brief.
              </div>
            </>
          ) : (
            <form
              onSubmit={async (e) => {
                e.preventDefault();
                setBusy(true);
                setError("");
                try {
                  const p = await api<Proposal>("/proposals", {
                    title,
                    source,
                  });
                  await refresh();
                  setActive(p.id);
                } catch (e) {
                  setError((e as Error).message);
                } finally {
                  setBusy(false);
                }
              }}
            >
              <label>
                Give your idea a name
                <input
                  required
                  minLength={3}
                  maxLength={100}
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                />
              </label>
              <label>
                What should people be able to do?
                <textarea
                  aria-label="Requirements"
                  required
                  minLength={20}
                  maxLength={20000}
                  rows={13}
                  value={source}
                  onChange={(e) => setSource(e.target.value)}
                />
              </label>
              <div className="input-footer">
                <span>
                  {source.length.toLocaleString()} / 20,000 characters
                </span>
                <button
                  type="button"
                  className="text-button"
                  onClick={() => {
                    setTitle("A better appointment experience");
                    setSource(sample);
                  }}
                >
                  Use example
                </button>
              </div>
              <button className="primary wide" disabled={busy}>
                {busy ? (
                  <LoaderCircle className="spin" size={16} />
                ) : (
                  <Sparkles size={17} />
                )}
                Generate a proposal
                <ArrowRight size={16} />
              </button>
              <p className="provider-note">
                {provider === "Mock"
                  ? "Mock mode builds a generic template from up to three requirement lines. Review and tailor it before approval."
                  : "OpenAI receives the brief text. Review generated assumptions, estimates and tests before approval."}
              </p>
            </form>
          )}
        </section>
        <section className="proposal-panel">
          <div className="panel-label">
            <span>02</span>
            <h2>A plan taking shape</h2>
            <Sparkles size={19} />
          </div>
          {!current ? (
            <div className="proposal-empty">
              <div className="outline-spark">
                <Sparkles size={32} />
              </div>
              <h3>
                Good plans start
                <br />
                with good questions.
              </h3>
              <p>
                Your proposal will appear here with editable stories, acceptance
                criteria, tasks and test cases.
              </p>
              <div>
                <span>CLARIFY</span>
                <ArrowRight size={13} />
                <span>SHAPE</span>
                <ArrowRight size={13} />
                <span>APPROVE</span>
              </div>
            </div>
          ) : ["Queued", "Generating"].includes(current.state) ? (
            <div className="proposal-empty">
              <LoaderCircle className="spin" size={30} />
              <h3>
                Giving your idea
                <br />
                some structure.
              </h3>
              <p>
                Generation runs in the background. This page refreshes
                automatically.
              </p>
            </div>
          ) : current.state === "Failed" ? (
            <div className="proposal-empty">
              <TriangleAlert size={27} />
              <h3>Let’s take another pass.</h3>
              <p>{current.error}</p>
              <button
                className="secondary"
                disabled={busy}
                onClick={async () => {
                  setBusy(true);
                  setError("");
                  try {
                    await api("/proposals/" + current.id + "/retry", {
                      version: current.version,
                    });
                    await refresh();
                  } catch (e) {
                    setError((e as Error).message);
                  } finally {
                    setBusy(false);
                  }
                }}
              >
                <RefreshCw size={14} />
                Retry generation
              </button>
            </div>
          ) : (
            current.plan && (
              <PlanEditor
                key={current.id}
                proposal={current}
                refresh={refresh}
                board={board}
              />
            )
          )}
        </section>
      </div>
    </main>
  );
}
function PlanEditor({
  proposal,
  refresh,
  board,
}: {
  proposal: Proposal;
  refresh: () => Promise<void>;
  board: () => void;
}) {
  const [plan, setPlan] = useState<Plan>(structuredClone(proposal.plan!)),
    [savedVersion, setSavedVersion] = useState(proposal.version),
    [dirty, setDirty] = useState(false),
    [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  const applied = proposal.state === "Applied";
  function update(next: Plan) {
    setPlan(next);
    setDirty(true);
  }
  function story(n: number, c: Content) {
    update({ ...plan, stories: plan.stories.map((s, i) => (i === n ? c : s)) });
  }
  async function save(approve = false) {
    setBusy(true);
    setError("");
    try {
      let version = savedVersion;
      if (dirty) {
        const p = await api<Proposal>(
          "/proposals/" + proposal.id,
          { version, plan },
          "PUT",
        );
        version = p.version;
      }
      if (approve)
        await api("/proposals/" + proposal.id + "/approve", { version });
      setSavedVersion(version);
      setDirty(false);
      setBusy(false);
      await refresh();
      if (approve) board();
    } catch (e) {
      setError((e as Error).message);
      setBusy(false);
    }
  }
  return (
    <div className="plan-editor">
      {error && (
        <div className="error" role="alert">
          {error}
        </div>
      )}
      <div className="plan-banner">
        <span>
          <Sparkles size={14} />
          {proposal.provider} draft
        </span>
        <span>
          {applied
            ? "Added to board"
            : dirty
              ? "Unsaved edits"
              : "Ready for your review"}
        </span>
      </div>
      <button
        className="text-button"
        disabled={busy}
        onClick={() => {
          setPlan(structuredClone(proposal.plan!));
          setSavedVersion(proposal.version);
          setDirty(false);
          setError("");
        }}
      >
        {dirty ? "Discard edits & reload" : "Reload proposal"}
      </button>
      <label>
        Proposal summary
        <textarea
          rows={3}
          maxLength={1500}
          disabled={applied || busy}
          value={plan.summary}
          onChange={(e) => update({ ...plan, summary: e.target.value })}
        />
      </label>
      <details className="questions" open>
        <summary>
          <MessageSquare size={16} />
          Questions worth asking <span>{plan.questions.length}</span>
        </summary>
        <label>
          <span className="sr-only">Clarification questions</span>
          <textarea
            rows={4}
            disabled={applied || busy}
            value={plan.questions.join("\n")}
            onChange={(e) =>
              update({ ...plan, questions: e.target.value.split("\n") })
            }
          />
        </label>
        <small>
          Resolve these with the stakeholder; approval does not validate
          assumptions.
        </small>
      </details>
      <div className="proposed-heading">
        <h3>
          Proposed stories <span>{plan.stories.length}</span>
        </h3>
        {!applied && (
          <button
            className="text-button"
            disabled={busy || plan.stories.length >= 6}
            onClick={() =>
              update({ ...plan, stories: [...plan.stories, newStory()] })
            }
          >
            <Plus size={14} />
            Add story
          </button>
        )}
      </div>
      {plan.stories.map((s, n) => (
        <details
          className="proposed-story"
          key={n}
          open={plan.stories.length === 1 ? true : undefined}
        >
          <summary>
            <span className="story-index">
              {String(n + 1).padStart(2, "0")}
            </span>
            <strong>{s.title}</strong>
            <span>{s.points} pts</span>
            <ChevronDown size={14} />
          </summary>
          <ContentEditor
            content={s}
            change={(c) => story(n, c)}
            disabled={applied || busy}
            draft
          />
          {!applied && (
            <button
              className="remove-story"
              disabled={busy || plan.stories.length === 1}
              onClick={() =>
                update({
                  ...plan,
                  stories: plan.stories.filter((_, i) => i !== n),
                })
              }
            >
              Remove this story
            </button>
          )}
        </details>
      ))}
      {applied ? (
        <div className="applied-note">
          <CheckCheck size={20} />
          <div>
            <strong>This plan is on the board.</strong>
            <p>
              Further edits happen on individual stories. Approval cannot
              duplicate them.
            </p>
          </div>
          <button className="text-button" onClick={board}>
            Open board <ArrowRight size={15} />
          </button>
        </div>
      ) : (
        <div className="plan-actions">
          <p>
            Review every story before adding it. All proposed tests start as
            “Not run”.
          </p>
          <div>
            <button
              className="secondary"
              disabled={!dirty || busy}
              onClick={() => void save()}
            >
              Save proposal
            </button>
            <button
              className="primary"
              disabled={busy}
              onClick={() => void save(true)}
            >
              {busy ? (
                <LoaderCircle className="spin" size={16} />
              ) : (
                <CheckCheck size={16} />
              )}
              Approve & add to board
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
function ContentEditor({
  content: c,
  change,
  disabled = false,
  draft = false,
}: {
  content: Content;
  change: (c: Content) => void;
  disabled?: boolean;
  draft?: boolean;
}) {
  return (
    <div className="content-editor">
      <label>
        Story title
        <input
          required
          maxLength={140}
          disabled={disabled}
          value={c.title}
          onChange={(e) => change({ ...c, title: e.target.value })}
        />
      </label>
      <label>
        User story
        <textarea
          rows={3}
          maxLength={2000}
          disabled={disabled}
          value={c.description}
          onChange={(e) => change({ ...c, description: e.target.value })}
        />
      </label>
      <div className="two-fields">
        <label>
          Priority
          <select
            disabled={disabled}
            value={c.priority}
            onChange={(e) => change({ ...c, priority: e.target.value })}
          >
            {priorities.map((p) => (
              <option key={p}>{p}</option>
            ))}
          </select>
        </label>
        <label>
          Estimate · story points
          <select
            disabled={disabled}
            value={c.points}
            onChange={(e) => change({ ...c, points: Number(e.target.value) })}
          >
            {[1, 2, 3, 5, 8, 13].map((n) => (
              <option key={n}>{n}</option>
            ))}
          </select>
        </label>
      </div>
      <label>
        Acceptance criteria <small>One per line</small>
        <textarea
          rows={4}
          disabled={disabled}
          value={c.acceptance.join("\n")}
          onChange={(e) =>
            change({ ...c, acceptance: e.target.value.split("\n") })
          }
        />
      </label>
      <label>
        Implementation tasks <small>One per line</small>
        <textarea
          rows={4}
          disabled={disabled}
          value={c.tasks.join("\n")}
          onChange={(e) => change({ ...c, tasks: e.target.value.split("\n") })}
        />
      </label>
      <h4>
        <FlaskConical size={15} />
        Test cases
      </h4>
      {c.tests.map((t, n) => (
        <div className="test-editor" key={n}>
          <div>
            <span className={"test-kind " + t.kind.toLowerCase()}>
              {t.kind}
            </span>
            {!draft && (
              <select
                aria-label={"Result for test " + (n + 1)}
                disabled={disabled}
                value={t.result}
                onChange={(e) =>
                  change({
                    ...c,
                    tests: c.tests.map((x, i) =>
                      i === n ? { ...x, result: e.target.value } : x,
                    ),
                  })
                }
              >
                {results.map((r) => (
                  <option key={r}>{r}</option>
                ))}
              </select>
            )}
          </div>
          <label>
            Scenario
            <textarea
              rows={2}
              maxLength={700}
              disabled={disabled}
              value={t.scenario}
              onChange={(e) =>
                change({
                  ...c,
                  tests: c.tests.map((x, i) =>
                    i === n ? { ...x, scenario: e.target.value } : x,
                  ),
                })
              }
            />
          </label>
          <label>
            Expected result
            <textarea
              rows={2}
              maxLength={700}
              disabled={disabled}
              value={t.expected}
              onChange={(e) =>
                change({
                  ...c,
                  tests: c.tests.map((x, i) =>
                    i === n ? { ...x, expected: e.target.value } : x,
                  ),
                })
              }
            />
          </label>
        </div>
      ))}
    </div>
  );
}
function Inspector({
  id,
  close,
  changed,
}: {
  id: number;
  close: () => void;
  changed: () => Promise<void>;
}) {
  useDialog(close);
  const [data, setData] = useState<Detail | null>(null),
    [edit, setEdit] = useState<Story | null>(null),
    [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [dirty, setDirty] = useState(false);
  useEffect(() => {
    api<Detail>("/stories/" + id)
      .then((d) => {
        setData(d);
        setEdit(structuredClone(d.story));
      })
      .catch((e) => setError(e.message));
  }, [id]);
  function update(s: Story) {
    setEdit(s);
    setDirty(true);
  }
  return (
    <div className="drawer-overlay">
      <div className="drawer-scrim" onClick={close} />
      <aside
        className="drawer"
        role="dialog"
        aria-modal="true"
        aria-labelledby="inspector-title"
      >
        <header>
          <span className="eyebrow" id="inspector-title">
            STORY INSPECTOR {edit && " / " + edit.code}
          </span>
          <button className="icon" aria-label="Close inspector" onClick={close}>
            <X size={20} />
          </button>
        </header>
        {error && (
          <div className="error" role="alert">
            {error}
          </div>
        )}
        {edit && data ? (
          <form
            onSubmit={async (e) => {
              e.preventDefault();
              setBusy(true);
              setError("");
              try {
                await api(
                  "/stories/" + id,
                  {
                    version: edit.version,
                    content: edit.content,
                    status: edit.status,
                    assignee: edit.assignee,
                    blocked: edit.blocked,
                    blocker: edit.blocker,
                  },
                  "PUT",
                );
                await changed();
                const d = await api<Detail>("/stories/" + id);
                setData(d);
                setEdit(d.story);
                setDirty(false);
              } catch (e) {
                setError((e as Error).message);
              } finally {
                setBusy(false);
              }
            }}
          >
            <div className="drawer-body">
              <h2>{edit.content.title}</h2>
              <div className="two-fields workflow-fields">
                <label>
                  Status
                  <select
                    aria-label="Story status"
                    disabled={busy}
                    value={edit.status}
                    onChange={(e) =>
                      update({ ...edit, status: e.target.value })
                    }
                  >
                    {columns.map((c) => (
                      <option key={c}>{c}</option>
                    ))}
                  </select>
                </label>
                <label>
                  Assignee
                  <input
                    placeholder="Unassigned"
                    maxLength={80}
                    disabled={busy}
                    value={edit.assignee}
                    onChange={(e) =>
                      update({ ...edit, assignee: e.target.value })
                    }
                  />
                </label>
              </div>
              <div className="blocker">
                <label>
                  <input
                    type="checkbox"
                    disabled={busy}
                    checked={edit.blocked}
                    onChange={(e) =>
                      update({ ...edit, blocked: e.target.checked })
                    }
                  />
                  <Flag size={14} />
                  This story is blocked
                </label>
                {edit.blocked && (
                  <label>
                    What’s in the way?
                    <textarea
                      rows={2}
                      required
                      minLength={3}
                      maxLength={500}
                      disabled={busy}
                      value={edit.blocker}
                      onChange={(e) =>
                        update({ ...edit, blocker: e.target.value })
                      }
                    />
                  </label>
                )}
              </div>
              <ContentEditor
                content={edit.content}
                change={(content) => update({ ...edit, content })}
                disabled={busy}
              />
              <section className="activity">
                <h3>
                  <Clock3 size={16} />
                  The story so far
                </h3>
                {data.activity.map((a) => (
                  <article key={a.id}>
                    <p>{a.text}</p>
                    <small>{new Date(a.at).toLocaleString()}</small>
                  </article>
                ))}
              </section>
            </div>
            <footer>
              <span>
                {dirty ? "Unsaved changes" : "All changes saved"}
                <small>Test results are recorded manually.</small>
              </span>
              <button className="primary" disabled={busy || !dirty}>
                {busy ? (
                  <LoaderCircle className="spin" size={16} />
                ) : (
                  <Check size={16} />
                )}
                Save story
              </button>
            </footer>
          </form>
        ) : (
          <div className="loading">Opening story…</div>
        )}
      </aside>
    </div>
  );
}
function TestPlan({
  stories,
  open,
}: {
  stories: Story[];
  open: (id: number) => void;
}) {
  const [kind, setKind] = useState("All"),
    [result, setResult] = useState("All");
  const cases = stories
    .flatMap((s) => s.content.tests.map((t, n) => ({ s, t, n })))
    .filter(
      (x) =>
        (kind === "All" || x.t.kind === kind) &&
        (result === "All" || x.t.result === result),
    );
  return (
    <main className="test-page">
      <div className="section-intro">
        <span className="eyebrow">CONFIDENCE, ONE CHECK AT A TIME</span>
        <h1>
          Leave less to <em>chance.</em>
        </h1>
        <p>
          A test plan for the work you approved. Results are recorded by you,
          not automatically executed.
        </p>
      </div>
      <div className="test-toolbar">
        <h2>{cases.length} test cases</h2>
        <div>
          <select
            aria-label="Test kind filter"
            value={kind}
            onChange={(e) => setKind(e.target.value)}
          >
            <option>All</option>
            {kinds.map((k) => (
              <option key={k}>{k}</option>
            ))}
          </select>
          <select
            aria-label="Test result filter"
            value={result}
            onChange={(e) => setResult(e.target.value)}
          >
            <option>All</option>
            {results.map((k) => (
              <option key={k}>{k}</option>
            ))}
          </select>
        </div>
      </div>
      <div className="test-table-scroll">
        <table className="test-table">
          <thead>
            <tr>
              <th>STORY</th>
              <th>TYPE</th>
              <th>SCENARIO & EXPECTED RESULT</th>
              <th>RESULT</th>
              <th>
                <span className="sr-only">Open</span>
              </th>
            </tr>
          </thead>
          <tbody>
            {cases.map(({ s, t, n }) => (
              <tr key={s.id + "-" + n}>
                <td>
                  <button className="story-link" onClick={() => open(s.id)}>
                    {s.code}
                    <small>{s.content.title}</small>
                  </button>
                </td>
                <td>
                  <span className={"test-kind " + t.kind.toLowerCase()}>
                    {t.kind}
                  </span>
                </td>
                <td>
                  <strong>{t.scenario}</strong>
                  <p>{t.expected}</p>
                </td>
                <td>
                  <span
                    className={
                      "test-result " + t.result.toLowerCase().replace(" ", "-")
                    }
                  >
                    {t.result}
                  </span>
                </td>
                <td>
                  <button
                    className="icon"
                    aria-label={"Edit tests for " + s.code}
                    onClick={() => open(s.id)}
                  >
                    <ArrowUpRight size={16} />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {!cases.length && (
          <div className="empty">
            <FlaskConical size={28} />
            <p>
              No test cases match this view. Approve a proposal or change your
              filters.
            </p>
          </div>
        )}
      </div>
    </main>
  );
}
function SummaryPage({ open }: { open: (id: number) => void }) {
  const [data, setData] = useState<Summary | null>(null),
    [error, setError] = useState("");
  async function refresh() {
    try {
      setData(await api<Summary>("/summary"));
      setError("");
    } catch (e) {
      setError((e as Error).message);
    }
  }
  useEffect(() => {
    void refresh();
  }, []);
  return (
    <main className="summary-page">
      <div className="section-intro">
        <span className="eyebrow">A MOMENT TO TAKE STOCK</span>
        <h1>
          Look how far
          <br />
          you’ve <em>moved.</em>
        </h1>
        <p>
          A snapshot of recorded work. No guessed progress, no invented
          deadlines.
        </p>
      </div>
      {error && (
        <div className="error" role="alert">
          {error}
        </div>
      )}
      {data ? (
        <div className="summary-sheet">
          <div className="summary-heading">
            <span>WORKSPACE FIELD NOTES</span>
            <button className="text-button" onClick={() => void refresh()}>
              <RefreshCw size={14} />
              Refresh snapshot
            </button>
          </div>
          <h2>
            {data.done} of {data.total} stories
            <br />
            have crossed the line.
          </h2>
          <div className="summary-progress">
            <i
              style={{
                width: `${data.total ? (data.done / data.total) * 100 : 0}%`,
              }}
            />
          </div>
          <p>
            {data.inProgress} in progress. {data.inReview} in review.{" "}
            {data.backlog} in the backlog.
          </p>
          <div className="summary-columns">
            <section>
              <h3>
                <CheckCheck size={17} />
                What’s done
              </h3>
              {data.completed.length ? (
                data.completed.map((s) => (
                  <button key={s.id} onClick={() => open(s.id)}>
                    <span>{s.code}</span>
                    {s.content.title}
                    <ArrowUpRight size={14} />
                  </button>
                ))
              ) : (
                <p>No completed stories yet.</p>
              )}
            </section>
            <section>
              <h3>
                <Flag size={17} />
                What needs a decision
              </h3>
              {data.blockers.length ? (
                data.blockers.map((s) => (
                  <button key={s.id} onClick={() => open(s.id)}>
                    <span>{s.code}</span>
                    <strong>{s.content.title}</strong>
                    <p>{s.blocker}</p>
                  </button>
                ))
              ) : (
                <p>No blockers recorded.</p>
              )}
            </section>
          </div>
          <div className="summary-tests">
            <FlaskConical size={20} />
            <div>
              <strong>
                {data.passed} passed · {data.failed} failed · {data.notRun} not
                run
              </strong>
              <p>
                Manual test results. Story completion does not imply tests have
                passed.
              </p>
            </div>
          </div>
          <footer>
            <span>
              Estimated scope: {data.points} points · Completed:{" "}
              {data.completedPoints} points
            </span>
            <small>
              Snapshot: {new Date(data.generatedAt).toLocaleString()}
            </small>
          </footer>
        </div>
      ) : (
        <div className="loading">Collecting your progress…</div>
      )}
    </main>
  );
}
createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
