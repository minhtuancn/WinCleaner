# GitHub Labels Reference

This document defines the standard labels used in the WinCleaner repository. Labels should be created in GitHub repository settings.

## Priority Labels

| Label | Color | Description |
|-------|-------|-------------|
| `priority:critical` | `#B60205` | Blocks release, security, data loss |
| `priority:high` | `#D93F0B` | Major feature broken, urgent |
| `priority:medium` | `#FBCA04` | Minor feature broken, workaround exists |
| `priority:low` | `#0E8A16` | Cosmetic, minor inconvenience, nice to have |

## Type Labels

| Label | Color | Description |
|-------|-------|-------------|
| `bug` | `#D73A4A` | Something isn't working |
| `enhancement` | `#A2EEEF` | New feature or improvement |
| `documentation` | `#0075CA` | Documentation improvements |
| `question` | `#D876E3` | Further information requested |
| `help wanted` | `#008672` | Extra attention needed |
| `good first issue` | `#7057FF` | Good for newcomers |

## Component Labels

| Label | Color | Description |
|-------|-------|-------------|
| `area:core` | `#1D76DB` | Core library (parser, services, models) |
| `area:infrastructure` | `#5319E7` | Windows infrastructure (converters, theme) |
| `area:ui` | `#BF8700` | WPF Application (ViewModels, Views) |
| `area:cli` | `#006B75` | Command-line interface |
| `area:inspector` | `#F9D0C4` | Blazor WASM Inspector |
| `area:tests` | `#C2E0C6` | Unit/Integration tests |
| `area:build` | `#D4C5F9` | Build system, CI/CD |
| `area:installer` | `#FEF2C0` | Installer, packaging |
| `area:docs` | `#BFD4F2` | Documentation |

## Status Labels

| Label | Color | Description |
|-------|-------|-------------|
| `status:blocked` | `#B60205` | Blocked by external dependency |
| `status:needs-review` | `#FBCA04` | Ready for code review |
| `status:needs-testing` | `#FEF2C0` | Needs manual testing |
| `status:ready` | `#0E8A16` | Ready to merge |
| `status:wip` | `#D4C5F9` | Work in progress |

## Special Labels

| Label | Color | Description |
|-------|-------|-------------|
| `security` | `#B60205` | Security-related issue/PR |
| `dependencies` | `#0366D6` | Dependency updates |
| `breaking-change` | `#D73A4A` | Breaking change |
| `needs-design` | `#D876E3` | Needs design/UX review |
| `performance` | `#A2EEEF` | Performance improvement |

## Creating Labels in GitHub

Run this script via GitHub CLI (`gh`) to create all labels:

```bash
#!/bin/bash
# Create labels using GitHub CLI
# Run: gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="label" -f color="color" -f description="description"

# Priority
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="priority:critical" -f color="B60205" -f description="Blocks release, security, data loss"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="priority:high" -f color="D93F0B" -f description="Major feature broken, urgent"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="priority:medium" -f color="FBCA04" -f description="Minor feature broken, workaround exists"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="priority:low" -f color="0E8A16" -f description="Cosmetic, minor inconvenience"

# Type
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="bug" -f color="D73A4A" -f description="Something isn't working"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="enhancement" -f color="A2EEEF" -f description="New feature or improvement"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="documentation" -f color="0075CA" -f description="Documentation improvements"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="question" -f color="D876E3" -f description="Further information requested"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="help wanted" -f color="008672" -f description="Extra attention needed"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="good first issue" -f color="7057FF" -f description="Good for newcomers"

# Component
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="area:core" -f color="1D76DB" -f description="Core library"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="area:infrastructure" -f color="5319E7" -f description="Windows infrastructure"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="area:ui" -f color="BF8700" -f description="WPF Application"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="area:cli" -f color="006B75" -f description="Command-line interface"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="area:inspector" -f color="F9D0C4" -f description="Blazor WASM Inspector"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="area:tests" -f color="C2E0C6" -f description="Unit/Integration tests"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="area:build" -f color="D4C5F9" -f description="Build system, CI/CD"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="area:installer" -f color="FEF2C0" -f description="Installer, packaging"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="area:docs" -f color="BFD4F2" -f description="Documentation"

# Status
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="status:blocked" -f color="B60205" -f description="Blocked by external dependency"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="status:needs-review" -f color="FBCA04" -f description="Ready for code review"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="status:needs-testing" -f color="FEF2C0" -f description="Needs manual testing"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="status:ready" -f color="0E8A16" -f description="Ready to merge"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="status:wip" -f color="D4C5F9" -f description="Work in progress"

# Special
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="security" -f color="B60205" -f description="Security-related"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="dependencies" -f color="0366D6" -f description="Dependency updates"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="breaking-change" -f color="D73A4A" -f description="Breaking change"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="needs-design" -f color="D876E3" -f description="Needs design/UX review"
gh api repos/minhtuancn/WinCleaner/labels -X POST -f name="performance" -f color="A2EEEF" -f description="Performance improvement"