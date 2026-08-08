# Working in this repository — agent guidance

**This file is a redirect.** The single source of truth for project guidance, hard rules, conventions, and documentation layout is [CLAUDE.md](CLAUDE.md). Read that file first; everything in it applies to **every** AI agent operating on this repository, not only to Claude Code.

`AGENTS.md` exists because some agent runtimes (notably OpenAI Codex and tools that follow the `AGENTS.md` convention) look for that filename specifically before falling back to `CLAUDE.md`. The repository keeps both filenames so neither runtime is left without a guide.

## Differences from CLAUDE.md

There are no rule differences. The only thing that varies between agent runtimes is the **`Co-Authored-By` commit trailer**, which names the assistant that contributed:

- Claude Code sessions:
  `Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>`
- OpenAI Codex sessions:
  `Co-Authored-By: Codex Opus 4.7 (1M context) <noreply@anthropic.com>`

If multiple agents contributed materially to the same commit, include one trailer per agent. [LearnStack's Git Workflow Standards § Trailers](../LearnStack/docs/standards/14-git-workflow.md#trailers) is the authority for both strings; if this file and that section disagree, that section wins.

## Skills

**This repository maintains its own skill catalogue** at [`.claude/skills/`](.claude/skills/README.md) — 18 skills, git-tracked through an un-ignore rule in `.gitignore`. Load them from here, not from the sibling LearnStack repo.

The Hub catalogue is Hub-tailored: the `add-hub-*` workflows encode Hub's deltas from LearnStack core — no Row Level Security, `OperatorId` rather than `UserId`, a six-step MediatR pipeline rather than eight, the `hub` schema, the `learnstack_hub` database. Skills cite LearnStack's standards and ADRs by sibling path (`../LearnStack/docs/...`) for cross-cutting authority and carry only the Hub-specific workflow on top; they do not duplicate the standards.

Entry-point selection is the same as in LearnStack core: [implement-task](.claude/skills/implement-task/SKILL.md) for substantive work, [start-task](.claude/skills/start-task/SKILL.md) for scoping only, [standards-check](.claude/skills/standards-check/SKILL.md) followed by [code-review](.claude/skills/code-review/SKILL.md) for review. Pick exactly one entry point; it dispatches the rest.

## Sibling paths

The two repositories sit side by side on disk as `LearnStack/` and `LearnStack-Hub/`, with those exact capitalisations. Cross-repo links are written `../LearnStack/...`. A link written `../LearnStack/...` resolves on a case-insensitive filesystem and breaks everywhere else, and the CI link audit skips cross-repo links — so nothing catches it before a reader does.

## Maintaining this file

Do **not** copy CLAUDE.md content into AGENTS.md. If a guidance rule needs to change, change CLAUDE.md; the rule applies everywhere by virtue of the redirect above. The two sections above are the only content that legitimately lives here, because they describe this file's own runtime rather than the project's rules.
