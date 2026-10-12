# Web File Explorer

A local file-browser application built with ASP.NET Core 8 and a vanilla TypeScript frontend. It supports folder navigation, recursive filename search, file downloads, and uploads within a configured filesystem root.

The application separates HTTP handling, use-case logic, and filesystem access. Unit tests isolate dependencies, while integration tests exercise the filesystem and HTTP endpoints. It uses the filesystem directly; no database, frontend framework, or external service is required.

## Features

- Browse immediate child folders and files, ordered folders first and then by case-insensitive relative path.
- Navigate through breadcrumbs, including compact breadcrumbs for deep paths.
- Search filenames and folder names recursively below the current folder, without matching file contents.
- Preserve the current path and committed search in the URL, supporting direct links, refresh, and browser Back/Forward.
- Download files as attachments and upload a file into the currently browsed folder.
- Reject duplicate filenames without overwriting existing files.
- Display file/folder counts and the total size of displayed file rows. Folder rows do not include recursive byte totals.
- Show actionable errors for invalid paths, missing folders, excessive search input, duplicate uploads, and configured limits.
- Preserve the previous view during navigation; upload progress uses the existing button rather than adding a temporary layout row.

## Prerequisites

- **.NET SDK 8.0.425**, selected by `global.json`.
- An accessible, writable data directory. The repository includes fictional sample data.
- A current browser.

Visual Studio 2022 with the ASP.NET and web development workload, or VS Code with C# tooling, can be used alongside the SDK. The command-line workflow does not require an IDE.

TypeScript compilation is integrated into MSBuild through `Microsoft.TypeScript.MSBuild`. No Node.js or npm installation is required for the build. Browsers execute the generated JavaScript, not the TypeScript source.

## Quick start

Run these commands from the repository root, beside `WebFileExplorer.sln`:

```shell
dotnet restore WebFileExplorer.sln
dotnet build WebFileExplorer.sln
dotnet test WebFileExplorer.sln
dotnet run --project src/WebFileExplorer.WebApp --no-launch-profile --urls http://localhost:5120
```

Open **http://localhost:5120**. This HTTP command runs the app locally without requiring a development certificate. For development HTTPS, trust the ASP.NET Core development certificate and use the `WebFileExplorer.WebApp` launch profile:

```shell
dotnet dev-certs https --trust
dotnet run --project src/WebFileExplorer.WebApp --launch-profile WebFileExplorer.WebApp
```

In Visual Studio, open the solution, set the WebApp as the startup project, and select its Project launch profile. Use the address printed by the running application.

## Configuration

Settings are in `src/WebFileExplorer.WebApp/appsettings.json` under `FileBrowser`:

```json
{
  "RootPath": "../../SampleData",
  "MaxUploadBytes": 26214400,
  "MaxScannedItems": 10000
}
```

`RootPath` is resolved relative to the WebApp's **content root**, not the shell's working directory. With the normal source-tree launch, `../../SampleData` points to the repository-root sample directory. Startup fails with a clear message if the configured directory is missing.

Environment variables can override settings without committing machine-specific configuration. For example, in PowerShell:

```powershell
$env:FileBrowser__RootPath = (Resolve-Path "C:\Demo\Files").Path
dotnet run --project src/WebFileExplorer.WebApp --no-launch-profile --urls http://localhost:5120
```

The example directory must exist. In a POSIX shell, an absolute-path override can be supplied before the command:

```shell
FileBrowser__RootPath=/absolute/path/to/files dotnet run --project src/WebFileExplorer.WebApp --no-launch-profile --urls http://localhost:5120
```

Remove environment overrides before returning to the default sample dataset.

### Limits and size semantics

- The file-content ceiling is **26,214,400 bytes**, or **25 MiB**. The UI uses decimal units consistently, so this limit appears as **26.2 MB**.
- The host allows an additional **1 MiB** for multipart framing. This is a deliberate allowance, not a guaranteed size for every possible multipart request. A known-length oversized request can be rejected before model binding; storage also verifies the exact file-content limit and cleans up an incomplete upload.
- `IFormFile` uses buffered model binding. Reading the uploaded file in chunks afterward does not make the whole HTTP pipeline unbuffered streaming.
- The scan ceiling is a defensive default of **10,000 inspected items**, not a benchmark-derived threshold. Physical enumeration counts entries before filtering links; Browse checks combined immediate children; Search enforces an aggregate ceiling across the directories it visits, including nonmatching rows.
- Search accepts at most **200 characters**. Blank search returns the immediate browse view.
- Totals represent the displayed file rows. A normal browse does not traverse descendant folders solely to calculate a size; recursive search totals only its matching file rows.

## Architecture and methodologies

### Layered dependency boundaries

Production code is divided into five assemblies:

- **SharedKernel**: the result and error types used across the application.
- **Domain**: file/folder models, typed relative-path identities, and path-format invariants. It does not depend on HTTP, ASP.NET Core, or concrete storage implementations.
- **Application**: commands, queries, handlers, response projections, configuration, and interfaces describing required services and repositories.
- **Infrastructure**: physical-filesystem access, containment/link checks, repositories that hydrate Domain models, and services that project them into Application responses.
- **WebApp**: composition root, HTTP controllers, middleware, static frontend, and transport-specific error mapping.

The structure follows Clean Architecture: Domain and Application do not depend on WebApp or Infrastructure. Domain concepts are used where they clarify identity and invariants; the application does not introduce aggregates, event sourcing, or database abstractions that the problem does not need.

### Use-case organization and CQRS

Commands and queries are organized by feature under `Application/FileSystemManagement`. Upload is a command; Browse, Search, Download, and child-item retrieval are queries.

Handlers are registered directly through dependency injection, without a mediator framework. Browse composes child-folder and child-file queries. Search reuses Browse for each visited directory while owning recursive traversal, matching, aggregate limits, and final result ordering. This separates immediate listing from recursive work without duplicating the read pipeline.

CQRS here means separating read and write use cases. It does not imply separate databases, distributed messaging, or eventual consistency.

### Validation and error handling

Path syntax is validated close to the Domain identities it protects. Infrastructure separately enforces the configured filesystem boundary and rejects symbolic links/reparse points. Expected use-case failures use typed `Result`/`Error` values rather than HTTP-specific types.

Controllers map expected failures to HTTP statuses: validation to 400, missing items to 404, conflicts to 409, and limits to 413. The frontend uses stable error codes for special recovery behavior and readable descriptions for ordinary banners. Unexpected faults remain exceptions rather than being mislabeled as user validation or duplicate-file errors.

### Browser state and progressive feedback

The frontend is a single organized TypeScript entry file, with strict compiler checks, DOM rendering, URL helpers, request lifecycle handling, and event handlers. API paths are slash-separated relative identities; URL parameters are encoded rather than constructed as OS paths.

The URL is the committed navigation/search state. Listing requests are abortable to avoid applying stale responses. The existing view stays visible while replacement data arrives, with stale controls made inert. Invalid views disable unsafe actions; a missing folder can recover to Root. Upload progress changes the button label and prevents duplicate submits, while errors remain visible in the status banner.

### Testing strategy

Tests mirror production responsibilities and use Arrange/Act/Assert with descriptive behavior-based names.

- **SharedKernel and Domain unit tests** exercise pure invariants and result contracts.
- **Application unit tests** isolate immediate dependencies with Moq or configurable handwritten fakes. Search substitutes Browse rather than executing the real Browse chain.
- **Infrastructure unit tests** verify projections against repository mocks and include an explicitly grouped assembly-dependency check.
- **Infrastructure integration tests** use disposable temporary directories, real repositories, and selected real-DI flows.
- **WebApp integration tests** start the application with `WebApplicationFactory`, isolated configuration/data roots, and real HTTP request/response processing in the test host. They cover endpoint contracts, middleware, and startup rejection.

Integration tests use isolated fixtures and cleanup rather than the repository's demo files. Supported async test operations use xUnit's test cancellation token. HTTP-host tests complement unit checks; browser/manual checks remain necessary for TypeScript behavior, layout, native file selection, and real Kestrel behavior.

Run the complete suite with:

```shell
dotnet test WebFileExplorer.sln
```

The test projects include Coverlet. To collect combined C# coverage:

```shell
dotnet test WebFileExplorer.sln --collect:"XPlat Code Coverage" --results-directory TestResults
```

This produces a report per test project. It does not measure TypeScript coverage; combined coverage requires merging reports without double-counting shared source lines. `codecoverage.runsettings` configures the separate Microsoft Code Coverage collector and is not used by the command above.

## API overview

- `GET /api/files?path=relative/folder`: immediate listing; omitted/empty path represents Root.
- `GET /api/files?path=relative/folder&q=bacon`: recursive name search below that folder.
- `GET /api/files/download?path=relative/file.txt`: attachment download.
- `POST /api/files/upload?path=relative/folder`: multipart form with a `file` field; omitted upload path targets Root.

Listing responses include row identities, relative paths, directory indicators, file sizes, counts, and the displayed-file total. Successful uploads return 201 with a relative download location. The UI intentionally hides uploads during search so the target folder remains unambiguous.

## Sample dataset

`SampleData` contains original, fictional, work-safe brunch-themed text and structured data. It has no real personal documents or third-party lorem-ipsum content.

- `00-Welcome.txt`: dataset introduction.
- `01-Recipes`: recipe notes, nested brunch folders, spaces/ampersands in names, a Unicode cafe filename, and a deeper shopping list.
- `02-Formats`: CSV, JSON, a small original SVG, and a zero-byte text file. These are downloadable files, not in-browser previews.
- `03-LotsOfFiles`: 40 small numbered notes to demonstrate scrolling and stable controls.
- `05-FileSizes`: 64-byte, 250,000-byte, and 1,000,000-byte files to demonstrate decimal formatting.
- `06-LongNames`: a long filename to demonstrate wrapping and narrow-layout behavior.
- `07-Uploads`: an upload destination with a short explanatory note.

The `.gitignore` explicitly allows the known sample fixtures and ignores other files uploaded under `SampleData`, including nested folders. The source fixtures are still writable: do not edit them with private content and assume Git will hide those changes. Inspect staged changes before publishing.

## Publishing and limitations

The app's data directory is outside `wwwroot`. `dotnet publish` does not include the repository-root sample dataset.

Before running a published copy, configure `FileBrowser:RootPath` in the published application's `appsettings.json`, or override it with the `FileBrowser__RootPath` environment variable. The directory must already exist and be accessible to the application, with write permission for uploads.

The default `../../SampleData` path is intended for running from the source tree. For a published deployment, use an absolute path or a relative path appropriate to the application's content root. Relative paths are resolved against that content root, not the shell's working directory.

```shell
dotnet publish src/WebFileExplorer.WebApp -c Release -o artifacts/publish
```

This is a **local demonstration**, not an authenticated public file service. Do not expose a writable instance to the internet without additional authentication, authorization, deployment controls, and threat review.

Other deliberate limitations:

- Search matches names, not content; folders have no recursively calculated size.
- No delete, rename, preview, overwrite, or multi-file upload workflow is implemented.
- Enumeration uses best-effort cancellation at use-case boundaries; a synchronous repository enumeration already in progress is not fully cancellable.
- Filesystem changes during a scan can affect results. Permission or unexpected I/O failures are not guaranteed to become a typed business error.
- Link checks are a useful containment safeguard, not a race-proof sandbox against a hostile local process modifying the same filesystem.
- Scan and upload defaults protect a small demo from unbounded work; they are not performance guarantees.

The repository intentionally excludes local IDE state, build outputs, generated JavaScript, test results, credentials, and new uploads. Restore and build before running a fresh clone so the frontend assets are generated.
