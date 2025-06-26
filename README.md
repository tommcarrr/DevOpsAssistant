# DevOpsAssistant

DevOpsAssistant is a Blazor WebAssembly application used for viewing and updating Azure DevOps work items. Docker assets are included so the site can be run either directly with the .NET SDK or inside a container.

## Running the site

### Docker Compose (recommended)

The repository contains a `Dockerfile` and `docker-compose.yml` for running the application behind Nginx. Build and start the container with:

```bash
docker compose up --build
```

Once the container is running, browse to <http://localhost:5678>.

To change the version displayed in the site's footer, edit the `version.txt`
file in the repository root. The file is mounted into the container so you can
update it without rebuilding.

### Using the `dotnet` CLI

If you prefer to run the site without Docker you can use the .NET SDK. A helper script `dotnet-install.sh` is provided for installing the SDK on Linux. After installing, execute:

```bash
dotnet run --project src/DevOpsAssistant/DevOpsAssistant/DevOpsAssistant.csproj
```

The development server listens on <http://localhost:5000>.

## Continuous Integration

Pull requests trigger a GitHub Actions workflow which restores the solution and
runs all tests. The workflow ensures builds remain healthy before merging. Test
execution collects code coverage data and publishes an HTML report as a build
artifact.

When changes are merged into the `main` branch, the deployment workflow first
publishes the site to a staging environment. Playwright UI tests verify that
each page loads correctly before the same build is promoted to production.
The production deployment runs in the `production` environment so GitHub can
require approval from designated reviewers before the job executes.
During pull requests the CI workflow performs a production build before running
the unit tests.

### Azure Static Web Apps

Production hosting uses Azure Static Web Apps. The configuration file
`staticwebapp.config.json` rewrites unknown URLs to `index.html` so deep links
(for example `/projects/myProject/requirements-planner`) work correctly.


## Obtaining a PAT token

The application communicates with Azure DevOps using a Personal Access Token (PAT). To create one:

1. Sign in to <https://dev.azure.com/>.
2. Open your user menu and choose **Personal access tokens**.
3. Select **New Token**, provide a name and a short expiration and grant
   the **Work Items (Read & write)**, **Wiki (Read)** and **Code (Read)** scopes.
4. Create the token and copy the value.

Run the site and you'll see a project selection screen if any projects are configured or be taken to the **New Project** page otherwise. Use the settings page from the project menu to configure each project. A comma separated list of default states can also be provided and each AI feature has a field for a custom prompt. These values are stored in your browser's local storage.
When you're done, click the **Sign Out** button in the top bar. A warning dialog confirms that all saved settings will be removed before returning to the project selection screen.

## Faking the DevOps API for tests

UI tests normally require a valid PAT token. When deploying to staging the site
reads optional overrides from `staging-config.json`. This file can specify
`devOpsApiBaseUrl` to redirect all API calls to a stub service and
`staticApiPath` for mock wiki responses. The file is copied only when the
`IncludeMockData` MSBuild property is set to `true`, so production deployments do
not include these settings or the mock JSON files.

The UI tests use Playwright to exercise the deployed staging site. Set
`STAGING_URL` to the site's base URL when running the tests locally so they know
where to connect.

## Accessibility

## Doom Theme

Type `iddqd` anywhere on the page to toggle a special Doom-inspired theme. Typing it again reverts to the default look. The choice is not saved between sessions.
