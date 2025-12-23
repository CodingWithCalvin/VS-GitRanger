# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Critical Rules

**These rules override all other instructions:**

1. **NEVER commit directly to main** - Always create a feature branch and submit a pull request
2. **Conventional commits** - Format: `type(scope): description`
3. **GitHub Issues for TODOs** - Use `gh` CLI to manage issues, no local TODO files. Use conventional commit format for issue titles
4. **Pull Request titles** - Use conventional commit format (same as commits)
5. **Branch naming** - Use format: `type/scope/short-description` (e.g., `feat/ui/settings-dialog`)
6. **Working an issue** - Always create a new branch from an updated main branch
7. **Check branch status before pushing** - Verify the remote tracking branch still exists. If a PR was merged/deleted, create a new branch from main instead
8. **WPF for all UI** - All UI must be implemented using WPF (XAML/C#). No web-based technologies (HTML, JavaScript, WebView)

---

### GitHub CLI Commands

```bash
gh issue list                    # List open issues
gh issue view <number>           # View details
gh issue create --title "type(scope): description" --body "..."
gh issue close <number>
```

### Conventional Commit Types

| Type | Description |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test` | Adding or updating tests |
| `chore` | Maintenance tasks |

---

## Project Overview

Git Ranger is a Visual Studio 2022/2026 extension for Git management, bringing GitLens-style functionality with theme-adaptive vibrant colors. Features include inline blame annotations, blame gutter margins, file history, and an interactive git graph.

## Build Commands

```bash
# Restore NuGet packages
nuget restore CodingWithCalvin.GitRanger.sln

# Build (Release)
msbuild src/CodingWithCalvin.GitRanger/CodingWithCalvin.GitRanger.csproj /p:configuration=Release /p:DeployExtension=False
```

The `/p:DeployExtension=False` flag prevents auto-deployment during CI builds. Remove it for local development to auto-deploy to the VS experimental instance.

## Architecture

### VS Extension Pattern

The extension follows the standard Visual Studio SDK pattern:

1. **GitRangerPackage** (ToolkitPackage) - Entry point that registers with VS and initializes services
2. **BlameCommands/HistoryCommands/GraphCommands** - Command handlers for menu actions
3. **BlameAdornment** - Editor text adornment for inline blame annotations
4. **BlameMargin** - Editor margin for blame gutter visualization

### Key Files

- `VSCommandTable.vsct` - Defines menu commands, groups, and placement in VS menus
- `source.extension.vsixmanifest` - VSIX package metadata (version, dependencies, assets)
- `GitRangerPackage.cs` - Package initialization and service registration

### Services

- **GitService** - LibGit2Sharp wrapper for Git repository operations
- **BlameService** - Blame data caching and background loading
- **ThemeService** - VS theme detection and color adaptation

### Editor Integration

- **BlameAdornmentFactory** (MEF) - Creates inline blame adornments for text views
- **BlameMarginFactory** (MEF) - Creates blame margin for editor gutter

## Technology Stack

- .NET Framework 4.8
- Visual Studio SDK 17.x (VS 2022/2026)
- Community.VisualStudio.Toolkit.17
- LibGit2Sharp for Git operations
- SkiaSharp for graph rendering (planned)
- WPF for UI
- MSBuild + NuGet for builds
- VSIX packaging format

## CI/CD

GitHub Actions workflows in `.github/workflows/`:
- `commit-lint.yml` - Validates PR titles and commit messages
- `release_build_and_deploy.yml` - Builds on PR and main branch push, creates VSIX artifact
- `preview-changelog.yml` - Preview release notes
- `publish.yml` - Manual trigger to publish to VS Marketplace

## Development Prerequisites

- Visual Studio 2022 with "Visual Studio extension development" workload
- A Git repository for testing blame functionality
