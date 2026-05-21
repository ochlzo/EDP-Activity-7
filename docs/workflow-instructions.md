# Complete Workflow Instructions

This repository has two separate Windows Forms apps that use the same MySQL
database:

- `edp-gui-admin`: admin app for global record management, account status,
  transaction tracking, and report-ready views.
- `edp-gui-app`: site-owner app for owner login, owned site management,
  room review, password recovery, and maintenance requests.

## Local Setup

Run these commands from the repository root:

```powershell
dotnet restore edp-gui-admin\edp-gui-admin.csproj
dotnet restore edp-gui-app\edp-gui-app.csproj
```

Both apps currently use this local database connection in `Program.cs`:

```text
Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;
```

Make sure MySQL is running and the `site_management` database exists before
opening either app.

## Running The Apps

Admin app:

```powershell
dotnet run --project edp-gui-admin\edp-gui-admin.csproj
```

Owner app:

```powershell
dotnet run --project edp-gui-app\edp-gui-app.csproj
```

## Admin App Login

1. Open the admin app.
2. Enter the fixed local admin credentials:
   - Username: `admin`
   - Password: `Admin123456!`
3. Click login.

During login, the admin auth service recreates and seeds the local `admin_user`
table. This is suitable for local development only.

## Admin Record Workflow

Use the admin app to create and maintain the global record hierarchy:

```text
Site Owners > Sites > Risers > Rooms > Tenants > Documents
```

Recommended setup order:

1. Create a site owner.
2. Create one or more sites for that owner.
3. Create risers under each site.
4. Create rooms under each riser.
5. Create tenants.
6. Assign tenants to rooms from the room editor.
7. Create documents for tenants.

The Add and Edit dialogs use dropdowns for parent lookup fields. Users should
select parent records by name instead of typing parent IDs manually.

Tenant creation stays simple: tenant name only.

## Admin Account Status Workflow

1. Open the `Site Owners` tab.
2. Use the in-cell `Status` dropdown.
3. Set the owner to `Active` or `Inactive`.

Inactive owners are blocked from logging into the owner app.

## Admin Search Workflow

Each main admin tab has a search box:

- `Site Owners`: searches owner name and email only.
- `Sites`: searches site and owner names.
- `Risers`: searches riser and site names.
- `Rooms`: searches room, riser, tenant, and occupancy text.
- `Tenants`: searches tenant name.
- `Documents`: searches document and tenant names.

Search filters only affect the current tab.

## Admin Parent Navigation Workflow

Double-click a parent record to move to the next child tab with an automatic
parent filter:

1. Double-click a site owner to show that owner's sites.
2. Double-click a site to show that site's risers.
3. Double-click a riser to show that riser's rooms.
4. Double-click a room to show the assigned tenant.
5. Double-click a tenant to show that tenant's documents.

When a parent filter is active, the grid shows a filter indicator above the
records. Click `Clear filter` to show the full list for that tab again.

Vacant rooms can navigate to an empty tenant result. This is expected.

## Admin Occupancy Workflow

Tenant assignment is tracked as an occupancy transaction:

1. Open the `Rooms` tab.
2. Edit a room.
3. Select a tenant from the tenant dropdown, or choose no tenant to vacate.
4. Save the room.
5. Open the `Occupancy` tab to review the transaction history.

Tracked occupancy actions include:

- `Assigned`
- `Vacated`
- `Transferred` from service support

These records are report-ready and include room, tenant, effective date, notes,
and creator.

## Admin Document Compliance Workflow

Documents include compliance metadata:

- Document name
- Tenant
- Type
- Status
- Issued date
- Expiration date
- Notes

Use the `Documents` tab for CRUD work. Use the `Document Compliance` tab for a
read-only reporting view of document status and expiration data.

## Admin Maintenance Workflow

Maintenance tickets are created from the owner app and reviewed in the admin
app.

1. Open the `Maintenance` tab.
2. Review ticket site, room, title, priority, status, request date, and resolved
   date.
3. Status changes are stored in `maintenance_ticket_status_history` by the
   transaction service.

The current admin UI exposes the maintenance list as a reporting view. Status
update service support exists for future admin controls.

## Admin Activity Log Workflow

The admin app logs report-ready audit rows when records are created, updated,
deleted, or when maintenance status changes.

Open the `Activity Log` tab to review:

- Actor type
- Actor name
- Action
- Entity type
- Entity ID
- Description
- Created date

## Owner App Sign Up And Login Workflow

1. Open the owner app.
2. Sign up with owner name, email, and password, or log in with an existing
   active owner account.
3. After login, the owner workspace opens.

If the admin has set the owner account to `Inactive`, login is blocked.

## Owner Password Recovery Workflow

1. Click the forgot-password flow from the owner app login screen.
2. Enter the owner email address.
3. Receive a reset code through the configured Gmail sender.
4. Enter the reset code and new password.

Password reset codes expire after 15 minutes and are stored as hashes only.
See `edp-gui-app/docs/reference/password-recovery.md` for environment setup.

## Owner Site Workflow

After owner login:

1. Review assigned sites in the owner workspace.
2. Search by site name or site ID.
3. Click a site name to open site details.
4. Add, edit, or delete owned sites from the site list.

Site operations are scoped to the logged-in owner.

## Owner Riser Workflow

From site details:

1. Review risers for the selected site.
2. Add a riser when needed.
3. Edit or delete a riser from the grid.
4. Click a riser to open room details.

Riser operations are scoped to the selected site and owner.

## Owner Room Workflow

From riser details:

1. Review rooms for the selected riser.
2. Sort rooms by room name ascending or descending.
3. Add a room when needed.
4. Update or delete a room from the grid.
5. Review occupancy status from the room list.

Owner room management currently edits room names only. Tenant assignment is
handled in the admin app.

## Owner Maintenance Request Workflow

From the owner workspace:

1. Click `Maintenance`.
2. Review existing maintenance requests.
3. Click `Add Request`.
4. Select the site from the dropdown.
5. Enter title, description, and priority.
6. Click `Create`.

The new ticket is stored as `Open` and becomes visible in the admin
`Maintenance` tab.

## Report-Ready Data Workflow

The admin app includes a `Reports` tab for report preview and Excel export.
Use it after logging in as the seeded admin user.

1. Open the `Reports` tab.
2. Select one of the primary transaction reports:
   - `Monthly Tenant Accommodations`
   - `Maintenance`
   - `Document Compliance`
3. Type the signatory name in the `Signatory` field.
4. Review the generated rows in the DataGrid preview.
5. Click `Export to Excel` and choose the output `.xlsx` path.

Each export uses the `Co-Siter` text header, includes the typed signatory,
stores the report rows on Sheet 1, and stores graph data plus a chart on Sheet
2.

`Monthly Tenant Accommodations` is sourced from current-year
`room_occupancy_transaction.created_at` rows whose transaction type is
`Assigned` or `Transferred`. Its Sheet 2 chart is a line graph showing monthly
year-to-date accommodation counts.

`Maintenance` uses Sheet 2 for a line graph of monthly year-to-date maintenance
ticket counts and adds a `Status Graph` sheet with a bar graph comparing open
and resolved maintenance tickets.

The Excel module uses the transaction/reporting services instead of reading UI
grids directly.

Primary export entry point:

```text
AdminTransactionService.LoadReportRowsAsync()
```

Returned export shape:

```text
Category
ParentName
ItemName
Status
EventDate
Notes
```

Current report categories:

- `Monthly Tenant Accommodations`
- `Maintenance`
- `Document Compliance`
- `Activity Log`

## Verification Workflow

Run admin tests after admin app changes:

```powershell
dotnet test edp-gui-admin.Tests\edp-gui-admin.Tests.csproj
```

Run owner tests after owner app changes:

```powershell
dotnet test edp-gui-app\edp-gui-app.Tests\edp-gui-app.Tests.csproj
```

Run both before handing off changes that affect shared database behavior.
