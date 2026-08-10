# Working in this repository — agent guidance

**This file is a redirect.** The single source of truth for project guidance, hard rules, conventions, and documentation layout is [CLAUDE.md](CLAUDE.md). Read that file first; everything in it applies to **every** AI agent operating on this repository, not only to Claude Code.

`AGENTS.md` exists because some agent runtimes (notably OpenAI Codex and tools that follow the `AGENTS.md` convention) look for that filename specifically before falling back to `CLAUDE.md`. The repository keeps both filenames so neither runtime is left without a guide.

## Differences from CLAUDE.md

There are no rule differences. The only thing that varies between agent runtimes is the **`Co-Authored-By` commit trailer**, which names the assistant that contributed:

- Claude Code sessions:
  `Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>`
- OpenAI Codex sessions:
  `Co-Authored-By: Codex Opus 4.7 (1M context) <noreply@anthropic.com>`

If multiple agents contributed materially to the same commit, include one trailer per agent. [LearnStack's Git Workflow Standards § Trailers](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/14-git-workflow.md#trailers) is the authority for both strings; if this file and that section disagree, that section wins.

## Maintaining this file

Do **not** copy CLAUDE.md content into AGENTS.md. If a guidance rule needs to change, change CLAUDE.md; the rule applies everywhere by virtue of the redirect above. The section above is the only content that legitimately lives here, because it describes this file's own runtime rather than the project's rules. Everything else — the skill catalogue and its entry points, the sibling on-disk layout, and the absolute-URL rule for cross-repository links — is in [CLAUDE.md](CLAUDE.md) and is not restated here.
