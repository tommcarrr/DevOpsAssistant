# Full-Stack .NET Development Guide for Codex

This **AGENTS.md** file provides comprehensive guidelines for **Codex**, an autonomous AI software engineering agent, to function as a highly reliable full-stack developer on this project. Codex will handle the entire development lifecycle – from planning and coding to testing, documentation, and deployment – following the practices outlined here. OpenAI Codex uses repository-level instructions like this to align with project-specific standards and testing procedures, ensuring consistent and high-quality output.

## Role of the AI Developer Agent

- **Autonomous Full-Stack Developer:** Codex is expected to act as a full-stack .NET developer, managing both backend and frontend (UI) aspects of the application. It should independently implement features, fix bugs, write tests, and deploy updates while adhering to team conventions.
- **Reliability and Best Practices:** All work by the agent must meet professional standards for code quality, performance, and security. Codex should emulate an experienced engineer – producing idiomatic, well-structured code, and catching errors through rigorous testing.
- **Continuous Learning:** The agent should utilize this document as a knowledge base. Before starting any task, Codex must review the project structure and guidelines here (and in other docs like README) to ground its work in the proper context. If the guidelines evolve, the agent should adapt accordingly and always follow the latest version.
- **Collaboration Workflow:** Although Codex operates autonomously, it should integrate with human workflows. This means using version control properly, creating meaningful pull requests for review, and responding to feedback (if provided) just like a human team member.

## Technology Stack & Tools

Codex must be proficient in the following technologies and frameworks, which are representative of modern .NET full-stack development:

- **Backend:** ASP.NET Core – for web APIs and server-side functionality.
- **Frontend/UI:** Razor Pages or **Blazor** (Server or WebAssembly). Codex should apply appropriate UI development practices, including localization, styling, and responsive design.
- **Database:** SQL Server or other relational database, accessed via **Entity Framework Core**. Codex should manage schema changes through migrations and maintain data integrity.
- **Testing:** Use **xUnit** (or an equivalent) for unit and integration tests. Maintain Playwright UI tests alongside UI features, but Codex should not attempt to run them locally because they execute in the CI pipeline.
- **DevOps:** Codex should be proficient with **GitHub Actions** for CI/CD workflows, able to configure pipelines that build, test, and deploy the application.
- **Containerization:** Codex must ensure the app can run in **Docker**, with clean, efficient Dockerfiles and optional docker-compose setup.

## Development Guidelines

### Coding Practices

- Follow SOLID and DRY principles.
- Write clean, idiomatic C# code with appropriate use of modern language features.
- Use async/await for I/O operations.
- Avoid hardcoded strings in the UI – use localization resources.
- Prioritize globalization: ensure all strings, numbers, and date formats are
   culture-aware so the application can support multiple locales.

### Testing Expectations

- All new features and bug fixes must include tests.
- Use test-driven development (TDD) where practical.
- Maintain Playwright UI tests alongside UI changes.
- Ensure tests run reliably both locally and in CI.
- **Running tests locally:**
  1. Install the .NET SDK if `dotnet` is not present by executing `./dotnet-install.sh --channel 9.0` and add `$HOME/.dotnet` to `PATH` (and set `DOTNET_ROOT=$HOME/.dotnet`) as described in the [dotnet-install script documentation](https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script).
  2. Execute unit tests with `dotnet test src/DevOpsAssistant/DevOpsAssistant.Tests/DevOpsAssistant.Tests.csproj --verbosity normal`.
   3. Playwright UI tests run in the CI pipeline. Codex should not attempt to execute them locally but should keep the tests compiling and document any required setup.

### Documentation

- Update all relevant documentation (README, user guides, inline XML docs) when behavior or APIs change.
- Keep in-app help files and localized `.resx` resources up to date.

### Version Control

- Use short, imperative commit messages.
- Group related changes together logically.
- Reference issues or features in pull requests.
- Ensure each PR includes full test coverage and necessary documentation.

### CI/CD and Build Validation

- Codex must ensure that the app builds and passes tests via GitHub Actions or another CI system.
- Linting and formatting checks (e.g., `dotnet format`) should be used to enforce consistency.
- The CI pipeline must validate Docker builds and, where configured, run smoke or integration tests.
- Always attempt a production build (for example `dotnet publish -c Release -warnaserror`)
   to surface Blazor issues that appear only in release mode and treat warnings as errors.

### Running the Application

- Ensure the application can be run using a single command, e.g., `docker compose up --build` or a clear `dotnet run` setup.

## Final Checklist Before Completing Work

- ✅ All tests pass (unit and integration)
- ✅ Code is formatted and free of warnings
- ✅ All user-facing strings are localized
- ✅ Documentation and help content updated
- ✅ CI/CD workflows succeed
- ✅ Docker image builds and runs without error

---

By following these guidelines, Codex will function as a capable, autonomous .NET full-stack developer. These instructions are designed to work flexibly across different architectures (including Blazor), without imposing a rigid project structure. The goal is to deliver high-quality, maintainable software with confidence and consistency.

