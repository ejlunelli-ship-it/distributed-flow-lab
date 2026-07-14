# Commit Standards

These standards govern **every commit** in the **Distributed Flow Lab (DFL)** repository —
backend (`src/` + `tests/`) and frontend (`web/`). A clean, consistent history is part of the
product: it powers changelog generation, `git bisect`, and fast code archaeology.

Standards are non-negotiable unless superseded by a documented ADR. When in doubt, prefer a
**small, single-purpose commit with a clear message** over a large, mixed one.

---

## 1. Conventional Commits

DFL uses **[Conventional Commits](https://www.conventionalcommits.org/)**. Every commit message
follows this shape:

```
<type>(<scope>): <imperative summary>

<body — the WHY, wrapped at 72 columns>

<footer — BREAKING CHANGE / issue refs>
```

- **Summary** — imperative mood ("add", not "added"/"adds"), lower-case, no trailing period,
  ≤ 72 characters.
- **Body** — optional but expected for anything non-trivial. Explain **why** the change exists
  and any context a future reader needs, not a restatement of the diff. Wrap at 72 columns.
- **Footer** — optional. `BREAKING CHANGE:` description, and issue/PR references
  (`Refs #12`, `Closes #12`).
- **Language** — English, always (consistent with the rest of the canon).

### 1.1 Types

| Type | Use for |
|------|---------|
| `feat` | a new user-facing feature or capability |
| `fix` | a bug fix |
| `docs` | documentation only (e.g. `docs/**`, `README.md`) |
| `refactor` | behavior-preserving code change |
| `test` | adding or fixing tests |
| `chore` | build, tooling, dependencies, repo config |
| `perf` | performance improvement |
| `ci` | CI/CD pipeline changes |
| `style` | formatting only (whitespace, Prettier) with no logic change |

### 1.2 Scopes

Scopes align with the architecture and feature areas:

`domain`, `application`, `infrastructure`, `api`, `engine`, `signalr`, `realtime`, `canvas`,
`simulation`, `catalog`, `inspector`, `web`, `docker`, `ci`, `docs`.

Use the most specific scope that fits. Omit the scope only when a change is genuinely
repo-wide.

### 1.3 Examples

```
feat(simulation): add RabbitMQ message animation
fix(engine): emit DeadLettered when retry budget is exhausted
docs(dev): add commit standards
refactor(application): extract event sequencing into a dedicated port
test(realtime): cover SignalR reconnection backoff
```

---

## 2. Authorship & trailers

- **Do not add AI / assistant co-authorship trailers.** Commits must **never** contain a
  `Co-Authored-By: Claude …` line (or any equivalent AI-tool attribution). The commit author
  is the human engineer who owns the change. This applies to commits authored with any
  assistant, including Claude Code.
- **Do not add tool-generated advertising footers** (e.g. "Generated with …"). Keep the footer
  reserved for `BREAKING CHANGE:` notes and issue references.
- **`Co-Authored-By:` is allowed only for real human co-authors** who genuinely pair-authored
  the change, using their own name and email.
- Configure `git` so authorship is correct and consistent:

  ```bash
  git config user.name  "Your Name"
  git config user.email "you@example.com"
  ```

> Rationale: the history should attribute work to accountable humans. AI assistance is a tool,
> like an IDE or a linter — it is not a co-author of record. Removing these trailers keeps the
> log clean, portable, and unambiguous about ownership.

---

## 3. Commit hygiene

- **One logical change per commit.** Do not mix a refactor with a feature, or formatting with
  behavior. If a commit needs the word "and" to describe it, split it.
- **Commits build and pass gates.** Each commit should leave the tree in a compiling, testable
  state (`dotnet format`, build, `tsc`, ESLint, and the relevant test suites green — see
  [Testing](./testing.md)). Avoid "WIP" commits on shared branches.
- **No secrets, no generated artifacts, no local config** in a commit (enforced by
  `.gitignore` / `.dockerignore`).
- **No dead code or unexplained `TODO`s** land in a commit (per `CLAUDE.md`).
- Reference the backlog item / roadmap phase in the body when relevant.

---

## 4. History & rewriting

- **`main` is protected.** Never force-push `main` except for a deliberate, reviewed history
  correction (e.g. stripping accidental co-authorship trailers or secrets), and always with
  `git push --force-with-lease` — never a bare `--force` — so a diverged remote is not silently
  overwritten.
- **Rewrite feature branches freely** (interactive rebase, squash, reword) *before* they are
  merged, to present a clean, reviewable history.
- **Preserve content when rewriting messages.** Editing commit messages must not change trees;
  verify with an empty `git diff <old> <new>` before pushing.

---

## 5. Pull requests

Every PR (per `CLAUDE.md`) includes:

- **Summary** — what and why, tied to a backlog item / roadmap phase.
- **Technical details** — layers touched, ports/adapters added, event types emitted.
- **Testing performed** — which suites ran and their result (see [Testing](./testing.md)).
- **Screenshots / recordings** — for any UI change.
- **Documentation updates** — architecture/backlog/roadmap edits, and an ADR link if an
  architectural decision was made.

PRs are small and single-purpose (one feature at a time). CI must be green before merge:
`dotnet format`, build, analyzers, `tsc`, ESLint, and the [Testing](./testing.md) gates.

---

## Related documents

- [Coding Standards](./coding-standards.md)
- [Testing](./testing.md)
- [Deployment](./deployment.md)
- [Folder Structure](./folder-structure.md)
