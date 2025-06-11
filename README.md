# DevOpsAssistant

DevOpsAssistant is a Blazor WebAssembly application used for viewing and updating Azure DevOps work items. Docker assets are included so the site can be run either directly with the .NET SDK or inside a container.

## Running the site

### Docker Compose (recommended)

The repository contains a `Dockerfile` and `docker-compose.yml` for running the application behind Nginx. Build and start the container with:

```bash
docker compose up --build
```

Once the container is running, browse to <http://localhost:5678>.

### Using the `dotnet` CLI

If you prefer to run the site without Docker you can use the .NET SDK. A helper script `dotnet-install.sh` is provided for installing the SDK on Linux. After installing, execute:

```bash
dotnet run --project src/DevOpsAssistant/DevOpsAssistant/DevOpsAssistant.csproj
```

The development server listens on <http://localhost:5000>.

## Continuous Integration

Pull requests trigger a GitHub Actions workflow which restores the solution and
runs all tests. The workflow ensures builds remain healthy before merging.

When changes are merged into the `main` branch, a separate workflow deploys the
Blazor application to Azure Static Web Apps. Deployments do not run during pull
requests.

## Obtaining a PAT token

The application communicates with Azure DevOps using a Personal Access Token (PAT). To create one:

1. Sign in to <https://dev.azure.com/>.
2. Open your user menu and choose **Personal access tokens**.
3. Select **New Token**, provide a name and a short expiration and grant
   the **Work Items (Read & write)** and **Wiki (Read)** scopes.
4. Create the token and copy the value.

Run the site and click the settings icon in the top right corner. Enter your organization, project and PAT token, then save. These values are stored in your browser's local storage.
When you're done, click the **Sign Out** button in the top bar to clear the saved settings.
