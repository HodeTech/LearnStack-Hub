# Working in this repository — agent guidance

**This file is a redirect.** The single source of truth for project guidance, hard rules, conventions, and documentation layout is [CLAUDE.md](CLAUDE.md). Read that file first; everything in it applies to **every** AI agent operating on this repository, not only to Claude Code.

`AGENTS.md` exists because some agent runtimes (notably OpenAI Codex and tools that follow the `AGENTS.md` convention) look for that filename specifically before falling back to `CLAUDE.md`. The repository keeps both filenames so neither runtime is left without a guide.

## Differences from CLAUDE.md

There are no rule differences, and the one thing that varies between agent runtimes — the **`Co-Authored-By` commit trailer**, which names the agent that actually contributed — is defined in [CLAUDE.md § Commit conventions](CLAUDE.md#commit-conventions) along with the per-runtime strings and the one-trailer-per-contributing-agent rule. It is not restated here, because two copies of a string that must match a third ([LearnStack's Git Workflow Standards § Trailers](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/14-git-workflow.md#trailers), which is the authority) is how the copies drift.

## Maintaining this file

Do **not** copy CLAUDE.md content into AGENTS.md. If a guidance rule needs to change, change CLAUDE.md; the rule applies everywhere by virtue of the redirect above. Nothing in this file is a rule of its own — the sections above describe only why this filename exists and where to read instead.
