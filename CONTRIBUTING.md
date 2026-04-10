# Contributing to MaksIT.CertsUI

Thank you for your interest in improving this project. This document describes how to set up a development environment, what we expect from contributions, and where to get help.

## License

By contributing, you agree that your contributions will be licensed under the same terms as the project. See [LICENSE.md](LICENSE.md) (Apache License 2.0).

## What to contribute

Useful contributions include bug fixes, documentation improvements, Helm chart updates, and small, focused feature changes that fit the architecture described in [README.md](README.md) (single agent, HTTP-01, and related limitations).

Large or architectural changes are best discussed first (see [Contact](#contact)) so effort aligns with project goals.

## Development setup

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) compatible with the `TargetFramework` values in the `.csproj` files under `src/` (the main solution currently targets **.NET 10**).
- Optional but recommended for end-to-end checks: **Docker** or **Podman**, as in the README installation sections.
- **Visual Studio 2022** (17.14+) or another editor with C# support works well; the solution file is `src/MaksIT.CertsUI.slnx` (SLNX; requires a recent `dotnet` SDK, e.g. 9.0.200+).

### Build

From the repository root:

```bash
dotnet build src/MaksIT.CertsUI.slnx -c Release
```

Use `Debug` while iterating locally if you prefer.

### Run the stack locally

Follow [README.md](README.md) for Podman Compose, Docker Compose, or Kubernetes (Helm). That is the supported way to exercise the WebAPI, WebUI, and reverse proxy together.

There is no separate automated test project in this repository today; manual verification through the WebUI and your compose or cluster setup is the practical check for most changes.

## Pull requests

1. **Branch from the branch the maintainers use for integration** (often `dev` or `main`—check the default on the host repository).
2. **Keep changes scoped**—one logical fix or feature per PR makes review and history easier.
3. **Describe the change** in the PR: what problem it solves, how you tested it, and any operational impact (config, Helm values, images).
4. **Update [CHANGELOG.md](CHANGELOG.md)** when the change is user-visible (behavior, security, deployment, or notable docs). Add entries under a new version heading or an `[Unreleased]` section at the top, following the existing [Keep a Changelog](https://keepachangelog.com/) style.
5. **Avoid unrelated formatting or drive-by refactors** in the same PR as functional changes.

## Security issues

Please do not open a public issue for undisclosed security vulnerabilities. Report them privately using the contact in [README.md](README.md) (Contact section) so they can be handled responsibly.

## Contact

Questions and coordination: see **Contact** in [README.md](README.md).
