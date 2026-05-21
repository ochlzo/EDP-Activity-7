# Report-Ready Transactions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add transaction features that improve the owner and admin apps while preparing clean data for a future MS Excel report module.

**Architecture:** Keep owner and admin apps separate. Add focused MySQL transaction tables, small domain records, service methods, and WinForms panels. Excel export is out of scope; the final task only adds flat report-row queries that a later Excel module can consume.

**Tech Stack:** C#/.NET `net10.0-windows`, Windows Forms, MySqlConnector, MSTest, local MySQL.

---

## Scope

In scope: occupancy lifecycle, maintenance tickets, document compliance metadata, activity logging, and report-ready query rows.

Out of scope: Excel export, document binary upload/download, shared library extraction, and role-based permissions.

## Feature Decisions

- Track **room occupancy transactions** when tenants are assigned, vacated, or transferred.
- Add **maintenance tickets** created by site owners and managed by admins.
- Extend **documents** with compliance metadata: type, status, issued date, submitted date, and notes.
- Add an **activity log** for admin and owner actions that matter in reports.
- Add **flat report-row queries** without generating spreadsheets.

## Files

Create:
- `edp-gui-admin/Domain/AdminTransactions.cs`
- `edp-gui-admin/Features/Transactions/AdminTransactionService.cs`
- `edp-gui-admin/Features/Transactions/AdminTransactionService.Schema.cs`
- `edp-gui-admin/UI/Transactions/AdminAppWindow.TransactionTabs.cs`
- `edp-gui-admin/UI/Transactions/AdminAppWindow.TransactionActions.cs`
- `edp-gui-admin.Tests/AdminTransactionServiceTests.cs`
- `edp-gui-app/Domain/OwnerSites/OwnerMaintenanceRequest.cs`
- `edp-gui-app/Features/OwnerSites/OwnerMaintenanceService.cs`
- `edp-gui-app/UI/OwnerSites/MainAppWindow.MaintenancePanels.cs`
- `edp-gui-app/UI/OwnerSites/MainAppWindow.MaintenanceActions.cs`
- `edp-gui-app/edp-gui-app.Tests/OwnerMaintenanceServiceTests.cs`

Modify:
- `edp-gui-admin/Domain/AdminRecords.cs`
- `edp-gui-admin/Features/Records/AdminRecordService.ReadOnly.cs`
- `edp-gui-admin/Features/Records/AdminRecordService.Rooms.cs`
- `edp-gui-admin/UI/Shell/AdminAppWindow.cs`
- `edp-gui-admin/UI/Records/AdminAppWindow.RecordEditors.cs`
- `edp-gui-app/Features/Auth/LoginFlowController.cs`
- `edp-gui-app/Features/Auth/LoginViewState.cs`
- `edp-gui-app/UI/Shell/MainAppWindow.cs`
- `edp-gui-app/docs/reference/columns-and-tables.md`

## Schema Plan

Add columns to `document`:

```sql
doc_type VARCHAR(80) NOT NULL DEFAULT 'General',
doc_status VARCHAR(40) NOT NULL DEFAULT 'Active',
issued_at DATE NULL,
submitted_at DATE NULL,
notes TEXT NULL,
created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
updated_at DATETIME NULL
```

Create tables:

```sql
room_occupancy_transaction(
  occupancy_transaction_id PK,
  room_id FK,
  tenant_id FK NULL,
  transaction_type,
  effective_at,
  notes,
  created_by,
  created_at
)

maintenance_ticket(
  ticket_id PK,
  site_id FK,
  riser_id FK NULL,
  room_id FK NULL,
  requested_by_owner_id FK,
  title,
  description,
  priority,
  status,
  requested_at,
  resolved_at
)

maintenance_ticket_status_history(
  history_id PK,
  ticket_id FK,
  old_status,
  new_status,
  changed_by,
  changed_at,
  notes
)

activity_log(
  activity_id PK,
  actor_type,
  actor_name,
  action,
  entity_type,
  entity_id,
  owner_id,
  site_id,
  riser_id,
  room_id,
  tenant_id,
  document_id,
  description,
  created_at
)
```

### Task 1: Add Schema and Domain Records

**Files:**
- Create: `edp-gui-admin/Domain/AdminTransactions.cs`
- Create: `edp-gui-admin/Features/Transactions/AdminTransactionService.cs`
- Create: `edp-gui-admin/Features/Transactions/AdminTransactionService.Schema.cs`
- Test: `edp-gui-admin.Tests/AdminTransactionServiceTests.cs`

- [ ] Write failing test `EnsureSchemaAsync_CreatesReportReadyTransactionTables`.
- [ ] Run: `dotnet test edp-gui-admin.Tests\edp-gui-admin.Tests.csproj --filter EnsureSchemaAsync_CreatesReportReadyTransactionTables`
- [ ] Implement `AdminTransactionService.EnsureSchemaAsync`.
- [ ] Add records: `AdminOccupancyTransaction`, `AdminMaintenanceTicket`, `AdminActivityLog`, `AdminReportRow`.
- [ ] Re-run the test and commit: `git commit -m "feat: add report-ready transaction schema"`

### Task 2: Track Occupancy Transactions

**Files:**
- Modify: `edp-gui-admin/Features/Transactions/AdminTransactionService.cs`
- Modify: `edp-gui-admin/Features/Records/AdminRecordService.Rooms.cs`
- Test: `edp-gui-admin.Tests/AdminTransactionServiceTests.cs`

- [ ] Write failing test `AssignTenantToRoomAsync_UpdatesRoomAndWritesTransaction`.
- [ ] Implement `AssignTenantToRoomAsync`, `VacateRoomAsync`, and `TransferTenantAsync`.
- [ ] Ensure each method updates `room.tenant_id` and inserts `room_occupancy_transaction` in one DB transaction.
- [ ] Route admin room tenant changes through these transaction methods.
- [ ] Run: `dotnet test edp-gui-admin.Tests\edp-gui-admin.Tests.csproj`
- [ ] Commit: `git commit -m "feat: track room occupancy transactions"`

### Task 3: Add Maintenance Ticket Workflow

**Files:**
- Create: `edp-gui-app/Domain/OwnerSites/OwnerMaintenanceRequest.cs`
- Create: `edp-gui-app/Features/OwnerSites/OwnerMaintenanceService.cs`
- Modify: `edp-gui-admin/Features/Transactions/AdminTransactionService.cs`
- Test: `edp-gui-app/edp-gui-app.Tests/OwnerMaintenanceServiceTests.cs`
- Test: `edp-gui-admin.Tests/AdminTransactionServiceTests.cs`

- [ ] Write failing owner test `CreateMaintenanceTicketAsync_CreatesOpenTicketForOwnedSite`.
- [ ] Implement owner service that validates `site.owner_id = ownerId`.
- [ ] Write failing admin test `UpdateMaintenanceTicketStatusAsync_WritesHistory`.
- [ ] Implement statuses: `Open`, `In Progress`, `Resolved`, `Cancelled`.
- [ ] Set `resolved_at` only for `Resolved`.
- [ ] Run owner and admin test suites.
- [ ] Commit: `git commit -m "feat: add maintenance ticket workflow"`

### Task 4: Add Document Compliance Metadata

**Files:**
- Modify: `edp-gui-admin/Domain/AdminRecords.cs`
- Modify: `edp-gui-admin/Features/Records/AdminRecordService.ReadOnly.cs`
- Modify: `edp-gui-admin/UI/Records/AdminAppWindow.RecordEditors.cs`
- Test: `edp-gui-admin.Tests/AdminRecordServiceTests.cs`

- [ ] Write failing test `CreateDocumentAsync_SavesComplianceFields`.
- [ ] Extend `AdminDocument` with `DocumentType`, `DocumentStatus`, `IssuedAt`, `SubmittedAt`, `Notes`.
- [ ] Update document load/create/update SQL.
- [ ] Update document add/edit dialog with type, status, and notes.
- [ ] Auto-fill issued and submitted dates in service code.
- [ ] Run: `dotnet test edp-gui-admin.Tests\edp-gui-admin.Tests.csproj`
- [ ] Commit: `git commit -m "feat: add document compliance metadata"`

Test skeleton:

```csharp
var doc = await service.CreateDocumentAsync("Lease", tenantId, "Lease", "Active", "Signed");
Assert.AreEqual("Lease", doc.DocumentType);
Assert.AreEqual(today, doc.IssuedAt?.Date);
Assert.IsNull(doc.SubmittedAt);
```

### Task 5: Add Activity Log

**Files:**
- Modify: `edp-gui-admin/Features/Transactions/AdminTransactionService.cs`
- Modify: `edp-gui-admin/Features/Records/AdminRecordService.Owners.cs`
- Modify: `edp-gui-admin/Features/Records/AdminRecordService.Sites.cs`
- Modify: `edp-gui-admin/Features/Records/AdminRecordService.Risers.cs`
- Modify: `edp-gui-admin/Features/Records/AdminRecordService.Rooms.cs`
- Modify: `edp-gui-admin/Features/Records/AdminRecordService.ReadOnly.cs`
- Test: `edp-gui-admin.Tests/AdminTransactionServiceTests.cs`

- [ ] Write failing test `LogActivityAsync_CreatesReportReadyAuditRow`.
- [ ] Implement `LogActivityAsync` and `LoadActivityLogsAsync`.
- [ ] Write one activity row after successful create/update/delete/status changes.
- [ ] Use actor type `Admin` and actor name `admin` for existing admin actions.
- [ ] Run: `dotnet test edp-gui-admin.Tests\edp-gui-admin.Tests.csproj`
- [ ] Commit: `git commit -m "feat: add admin activity log"`

Test skeleton:

```csharp
await service.LogActivityAsync("Admin", "admin", "Updated", "Room", 12, "Changed room name");
Assert.AreEqual("Updated", logs.Single().Action);
```

### Task 6: Add Admin Transaction Views

**Files:**
- Create: `edp-gui-admin/UI/Transactions/AdminAppWindow.TransactionTabs.cs`
- Create: `edp-gui-admin/UI/Transactions/AdminAppWindow.TransactionActions.cs`
- Modify: `edp-gui-admin/UI/Shell/AdminAppWindow.cs`

- [ ] Add tabs for `Occupancy`, `Maintenance`, `Document Compliance`, and `Activity Log`.
- [ ] Add binding sources and refresh methods for each tab.
- [ ] Add maintenance status action buttons.
- [ ] Add room assign/vacate actions or route existing room edit flow through occupancy methods.
- [ ] Run: `dotnet build edp-gui-admin\edp-gui-admin.csproj`
- [ ] Commit: `git commit -m "feat: add admin transaction views"`

### Task 7: Add Owner Maintenance UI

**Files:**
- Create: `edp-gui-app/UI/OwnerSites/MainAppWindow.MaintenancePanels.cs`
- Create: `edp-gui-app/UI/OwnerSites/MainAppWindow.MaintenanceActions.cs`
- Modify: `edp-gui-app/UI/Shell/MainAppWindow.cs`
- Modify: `edp-gui-app/Features/Auth/LoginViewState.cs`
- Modify: `edp-gui-app/Features/Auth/LoginFlowController.cs`
- Test: `edp-gui-app/edp-gui-app.Tests/LoginFlowControllerTests.cs`

- [ ] Add failing navigation test for a maintenance view.
- [ ] Add owner maintenance panel listing owner tickets.
- [ ] Add create-ticket dialog with site, optional riser/room, title, description, and priority.
- [ ] Wire actions to `OwnerMaintenanceService`.
- [ ] Run: `dotnet test edp-gui-app\edp-gui-app.Tests\edp-gui-app.Tests.csproj`
- [ ] Commit: `git commit -m "feat: add owner maintenance requests"`

### Task 8: Add Report-Ready Query Rows

**Files:**
- Modify: `edp-gui-admin/Features/Transactions/AdminTransactionService.cs`
- Test: `edp-gui-admin.Tests/AdminTransactionServiceTests.cs`

- [ ] Write failing test `LoadReportRowsAsync_ReturnsSpreadsheetReadyRows`.
- [ ] Implement `LoadReportRowsAsync()` for occupancy, maintenance, documents, and activity logs.
- [ ] Add overload `LoadReportRowsAsync(DateTime? from, DateTime? to)`.
- [ ] Do not generate Excel files.
- [ ] Run: `dotnet test edp-gui-admin.Tests\edp-gui-admin.Tests.csproj`
- [ ] Commit: `git commit -m "feat: add report-ready row queries"`

Test skeleton:

```csharp
var rows = await service.LoadReportRowsAsync();
Assert.IsTrue(rows.Any(row => row.Category == "Occupancy"));
Assert.IsTrue(rows.All(row => !string.IsNullOrWhiteSpace(row.ItemName)));
```

### Task 9: Documentation and Final Verification

**Files:**
- Modify: `edp-gui-app/docs/reference/columns-and-tables.md`

- [ ] Document new tables and document columns.
- [ ] Note that Excel export must consume `LoadReportRowsAsync` later.
- [ ] Run:
  - `dotnet build edp-gui-admin\edp-gui-admin.csproj`
  - `dotnet test edp-gui-admin.Tests\edp-gui-admin.Tests.csproj`
  - `dotnet build edp-gui-app\edp-gui-app.csproj`
  - `dotnet test edp-gui-app\edp-gui-app.Tests\edp-gui-app.Tests.csproj`
- [ ] Commit: `git commit -m "docs: document report-ready transaction schema"`

## Notes

- Keep each `.cs` file under 300 lines.
- Use `SslMode=None` in local MySQL test connection strings.
- Prefer append-only transaction rows; use current-state columns only for UI convenience.
- Future Excel integration should query `AdminReportRow` results instead of duplicating SQL.
