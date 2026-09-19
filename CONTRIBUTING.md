# Contributing to WinCleaner

Thank you for your interest in contributing to WinCleaner! This document provides guidelines and instructions for contributing.

## Code of Conduct

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

## Getting Started

### Prerequisites

- .NET 8 SDK
- Windows 10/11 (for WPF development)
- Visual Studio 2022 or VS Code with C# Dev Kit
- Git

### Development Setup

1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/WinCleaner.git
   cd WinCleaner
   ```
3. Add upstream remote:
   ```bash
   git remote add upstream https://github.com/minhtuancn/WinCleaner.git
   ```
4. Restore dependencies:
   ```bash
   dotnet restore WinCleaner.sln
   ```
5. Build:
   ```bash
   dotnet build WinCleaner.sln --configuration Release
   ```
6. Run tests:
   ```bash
   dotnet test WinCleaner.sln --configuration Release
   ```

## Development Workflow

### Branch Naming

- Feature: `feat/issue-XX-description`
- Bug fix: `fix/issue-XX-description`
- Documentation: `docs/issue-XX-description`
- Refactor: `refactor/issue-XX-description`
- Chore: `chore/issue-XX-description`

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding/updating tests
- `chore`: Maintenance tasks
- `perf`: Performance improvements
- `ci`: CI/CD changes

Example:
```
feat(core): add FileKey/RegKey support to Winapp2 parser

- Add FileKeyEntry and RegKeyEntry models
- Update parser to handle FileKey1-5 and RegKey1-5
- Add unit tests for new parser functionality

Closes #5
```

### Pull Request Process

1. Ensure your branch is up to date with `main`:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```
2. Run full test suite:
   ```bash
   dotnet build WinCleaner.sln --configuration Release --no-restore
   dotnet test WinCleaner.sln --configuration Release --no-build
   ```
3. Push your branch:
   ```bash
   git push origin your-branch-name
   ```
4. Create a Pull Request targeting `main`
5. Fill out the PR template completely
6. Wait for CI checks to pass
7. Address review comments
8. PR will be squash-merged after approval

## Coding Standards

### C# Style

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `var` for obvious types
- Prefer expression-bodied members for simple properties/methods
- Use nullable reference types (`#nullable enable`)
- Treat warnings as errors (configured in `Directory.Build.props`)

### Architecture

- Follow Clean Architecture principles
- Use Dependency Injection (DI) for all services
- Keep Core library platform-agnostic (net8.0)
- Windows-specific code in `WinCleaner.Infrastructure.Windows`
- UI in `WinCleaner.App` (WPF, net8.0-windows)
- CLI in `WinCleaner.Cli` (net8.0)

### Testing

- Write unit tests for all new functionality
- Target: ≥90% code coverage
- Use xUnit, Moq, FluentAssertions
- Place tests in `tests/WinCleaner.Tests.Unit`
- Integration tests in `tests/WinCleaner.Tests.Integration`

### Documentation

- Update `README.md` for user-facing changes
- Update `docs/architecture/feature-matrix.md` for feature changes
- Add XML documentation for public APIs
- Update `CHANGELOG.md` (maintained by maintainers)

## Project Structure

```
WinCleaner/
├── src/
│   ├── WinCleaner.Core/                 # Domain models, services, parser (net8.0)
│   ├── WinCleaner.Infrastructure.Windows/ # WPF converters, ThemeService (net8.0-windows)
│   ├── WinCleaner.App/                  # WPF Application (net8.0-windows)
│   ├── WinCleaner.Cli/                  # CLI Application (net8.0)
│   └── WinCleaner.Inspector/            # Blazor WASM Inspector (net8.0)
├── tests/
│   ├── WinCleaner.Tests.Unit/           # Unit tests
│   └── WinCleaner.Tests.Integration/    # Integration tests
├── docs/
│   └── architecture/                    # Architecture docs
├── .github/
│   ├── workflows/                       # CI/CD pipelines
│   └── dependabot.yml                   # Dependabot config
├── WinCleaner.sln
├── Directory.Build.props
├── Directory.Packages.props
├── Version.props
├── version.json
├── global.json
├── .editorconfig
├── renovate.json
├── CODEOWNERS
├── SECURITY.md
├── CONTRIBUTING.md
├── CHANGELOG.md
└── ROADMAP.md
```

## Reporting Issues

Before creating an issue, please:
1. Search existing issues
2. Check if it's already fixed in `main`
3. Use the appropriate issue template

## Feature Requests

Feature requests are welcome! Please:
1. Check the [Roadmap](ROADMAP.md)
2. Search existing issues
3. Create a feature request issue with the template

## License

By contributing, you agree that your contributions will be licensed under the project's [MIT License](LICENSE).

## Questions?

- Open a discussion on GitHub
- Check existing documentation in `docs/`
- Review the [Architecture Decision Records](docs/architecture/adr/)