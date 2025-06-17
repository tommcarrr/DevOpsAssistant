# AGENTS Instructions

The following guidelines apply to the entire repository and inform how Codex should work.

## Development workflow

- After modifying files, run `dotnet format --no-restore` to keep style consistent.
- Run `dotnet restore` and then `dotnet test src/DevOpsAssistant/DevOpsAssistant.sln` to verify the solution builds and tests succeed.
- Run `dotnet build src/DevOpsAssistant/DevOpsAssistant.sln -c Release -warnaserror` to ensure there are no Roslyn warnings.
- If these commands fail because the environment lacks `dotnet`, note this in the PR's testing section.
- Ensure all user-facing strings come from localization resources. Add new `.resx` entries when introducing UI text.
- Keep `src/DevOpsAssistant/DevOpsAssistant/Pages/Help.razor` and its `.resx` files current with instructions for each page whenever features change.

## Commit guidelines

- Use short, imperative commit messages (e.g. "Add new API method"). Group related changes together.
- Follow the existing C# style and project structure.
- Adhere to SOLID and DRY principles to keep the code maintainable.

## Pull request notes

- Reference relevant lines from `README.md` when explaining how to build or run the project.
- Ensure all functionality is covered by tests. Write tests first when possible.
- Update documentation whenever behavior or APIs change so README and other docs stay accurate.

## Running the application

- Use `docker compose up --build` to run the site locally.

## UI tests

- Playwright tests live in `src/DevOpsAssistant/DevOpsAssistant.UiTests`.
- Keep these tests current and add new coverage whenever the UI changes.
- The CD workflow deploys to staging and then runs the Playwright tests against
  that environment.

