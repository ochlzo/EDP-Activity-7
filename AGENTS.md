# Repository Guidelines

## Project Structure & Module Organization

This repository contains .NET Windows Forms CRUD apps. The owner-facing app is in `edp-gui-app/edp-gui-app.csproj`, and the separate admin app is in `edp-gui-admin/edp-gui-admin.csproj`. Both target `net10.0-windows`.

- `edp-gui-app/App/` contains the application entry point (`Program.cs`).
- `edp-gui-app/UI/` contains WinForms UI code, split by feature area and partial `MainAppWindow` files.
- `edp-gui-app/Features/` contains application logic for authentication and owner-site workflows.
- `edp-gui-app/Domain/` contains domain models such as site owners.
- `edp-gui-app/edp-gui-app.Tests/` contains MSTest unit tests.
- `edp-gui-app/docs/` contains reference notes.
- `edp-gui-admin/App/` contains the admin application entry point.
- `edp-gui-admin/UI/` contains WinForms admin UI code, split by shell, auth, and record-management partials.
- `edp-gui-admin/Features/` contains independent admin authentication and record-management services.
- `edp-gui-admin/Domain/` contains admin-facing record models.
- `edp-gui-admin.Tests/` contains MSTest coverage for the admin app.

Keep files small and focused. Prefer adding feature-specific partials or helper classes over expanding large UI files. The owner app and admin app are intentionally separate executables with independent app/service/UI code; do not introduce shared code unless the task explicitly asks for it.

## Build, Test, and Development Commands

Run commands from the repository root:

```powershell
dotnet restore edp-gui-app\edp-gui-app.csproj
dotnet build edp-gui-app\edp-gui-app.csproj
dotnet run --project edp-gui-app\edp-gui-app.csproj
dotnet test edp-gui-app\edp-gui-app.Tests\edp-gui-app.Tests.csproj

dotnet restore edp-gui-admin\edp-gui-admin.csproj
dotnet build edp-gui-admin\edp-gui-admin.csproj
dotnet run --project edp-gui-admin\edp-gui-admin.csproj
dotnet test edp-gui-admin.Tests\edp-gui-admin.Tests.csproj
```

`restore` downloads NuGet packages, `build` compiles the WinForms app, `run` starts the local desktop app, and `test` runs the MSTest suite. Use the owner app commands for site-owner workflows and the admin app commands for global record management.

## Coding Style & Naming Conventions

Use C# with nullable reference types and implicit usings enabled. Follow the existing file-scoped namespace style (`namespace edp_gui_app;` for the owner app and `namespace edp_gui_admin;` for the admin app) and four-space indentation. Use PascalCase for classes, methods, records, and properties; use camelCase for locals and parameters. Keep test method names descriptive, for example `Apply_FiltersBySiteName`.

The owner UI uses partial `MainAppWindow` files grouped by workflow (`Auth`, `OwnerSites`, `Shell`). The admin UI uses partial `AdminAppWindow` files grouped by shell, auth, and records. Match the relevant app's existing pattern when adding UI behavior.

## Admin App Record Dialogs

In the admin app, do not ask users to manually type parent-table IDs in record create/edit dialogs. Use combo boxes for parent lookups when creating or editing child records, show human-readable names in lookup controls, and pass the selected record ID to the service layer. Keep tenant creation as a simple name-only form because tenants currently have no parent table. For rooms, tenant selection should be optional and should allow leaving the room vacant.

## Testing Guidelines

Tests use MSTest with `[TestClass]` and `[TestMethod]`. Add owner app tests beside the existing test files in `edp-gui-app/edp-gui-app.Tests/`. Add admin app tests in `edp-gui-admin.Tests/`. Name test classes after the unit under test, such as `OwnedRoomSortTests`. Prefer focused tests for business logic and controller behavior before changing UI-heavy code.

Run `dotnet test edp-gui-app\edp-gui-app.Tests\edp-gui-app.Tests.csproj` before submitting changes.
Run `dotnet test edp-gui-admin.Tests\edp-gui-admin.Tests.csproj` when changing the admin app.

Local MySQL integration tests should include `SslMode=None` in test fixture connection strings unless the local database is explicitly configured for SSL.

## Commit & Pull Request Guidelines

Recent commits use short imperative or conventional-style messages such as `feat: added risers and room panels`, `chore: cleaned up file tree`, and `Add project title to README`. Prefer `feat:`, `fix:`, `chore:`, or a concise imperative summary.

Pull requests should include a short description, testing performed, linked issue or task when available, and screenshots for visible UI changes.

## Security & Configuration Tips

`Program.cs` currently contains local MySQL connection strings. Do not commit real production credentials. Prefer local-only settings or environment-based configuration when introducing new database or secret values.
