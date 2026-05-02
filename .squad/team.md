# Squad Team

> RssFeeder

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Aragorn | Lead | `.squad/agents/aragorn/charter.md` | ✅ Active |
| Gandalf | Backend Dev | `.squad/agents/gandalf/charter.md` | ✅ Active |
| Legolas | Frontend Dev | `.squad/agents/legolas/charter.md` | ✅ Active |
| Gimli | Data Engineer | `.squad/agents/gimli/charter.md` | ✅ Active |
| Frodo | Tester | `.squad/agents/frodo/charter.md` | ✅ Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 📋 Silent |
| Ralph | Work Monitor | — | 🔄 Monitor |

## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage for isolated parser, exporter, and command behaviors
- Small feed builder updates with concrete rules
- Documentation fixes and README updates

**🟡 Needs review — route to @copilot but flag for squad member PR review:**
- Medium command refactors with existing test coverage
- MVC handler additions following established patterns
- Repository plumbing changes with contained scope

**🔴 Not suitable — route to squad member instead:**
- Architecture decisions spanning console, MVC, and storage
- Parser or crawler redesigns that affect multiple feeds
- Performance investigations without clear reproduction
- Security-sensitive configuration and secret handling

## Project Context

- **Project:** RssFeeder
- **Owner:** Matthew Noonan
- **Stack:** C#, .NET, ASP.NET Core MVC, console workers, JSON configuration, repository-backed exports
- **Description:** Aggregates RSS and scraped article sources into exportable feeds through console tooling and an MVC surface.
- **Created:** 2026-04-19
