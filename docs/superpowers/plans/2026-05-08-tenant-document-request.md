# Tenant Document Request Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let site owners send tenant document request emails through Gmail OAuth and collect requested uploads through a same-machine localhost form.

**Architecture:** Extend owner tenant data access, add a fixed document request catalog, add a localhost-only upload server, and add Tenant Details UI from the existing Riser Details room grid. Keep state in memory for request links and persist uploaded files plus document rows locally.

**Tech Stack:** C# WinForms on `net10.0-windows`, MySqlConnector, Gmail REST API via existing OAuth credentials, `HttpListener` for localhost upload handling, MSTest.

---

## File Structure

- Create `edp-gui-app/Features/OwnerSites/OwnedTenant.cs`: owner-facing tenant profile.
- Create `edp-gui-app/Features/OwnerSites/OwnedDocumentAttachment.cs`: uploaded document metadata for Tenant Details.
- Create `edp-gui-app/Features/OwnerSites/DocumentRequestCatalog.cs`: fixed requestable document names.
- Create `edp-gui-app/Features/OwnerSites/DocumentRequestModels.cs`: request token and selected document state.
- Create `edp-gui-app/Features/OwnerSites/DocumentUploadStorage.cs`: safe local upload paths and filename sanitization.
- Create `edp-gui-app/Features/OwnerSites/LocalDocumentRequestServer.cs`: localhost form server and upload endpoint.
- Create `edp-gui-app/Features/Auth/DocumentRequestEmailMessage.cs`: email payload for Gmail document request emails.
- Modify `edp-gui-app/Features/Auth/IEmailSender.cs`: add document request email method.
- Modify `edp-gui-app/Features/Auth/GmailPasswordResetEmailSender.cs`: send document request emails through existing OAuth setup.
- Modify `edp-gui-app/Features/Auth/SiteOwnerAuthService.Rooms.cs`: load/create tenants with details and document attachments.
- Modify `edp-gui-app/Features/OwnerSites/OwnedRoom.cs`: include tenant email if useful for grid/details.
- Modify `edp-gui-app/UI/Shell/MainAppWindow.cs`: add local request server field.
- Modify `edp-gui-app/UI/OwnerSites/MainAppWindow.OwnerSitesDialogs.cs`: replace Add Tenant name-only dialog with four-field dialog and add request checklist dialog.
- Modify `edp-gui-app/UI/OwnerSites/MainAppWindow.RiserDetailsPanels.cs`: add tenant-cell hover/click events.
- Modify `edp-gui-app/UI/OwnerSites/MainAppWindow.RiserDetailsActions.cs`: open Tenant Details and send request.
- Test `edp-gui-app/edp-gui-app.Tests/SiteOwnerRoomTenantServiceTests.cs`: tenant details persistence.
- Test `edp-gui-app/edp-gui-app.Tests/DocumentRequestCatalogTests.cs`: fixed list.
- Test `edp-gui-app/edp-gui-app.Tests/DocumentUploadStorageTests.cs`: safe paths.

## Task 1: Models and Fixed Catalog

**Files:**
- Create: `edp-gui-app/Features/OwnerSites/OwnedTenant.cs`
- Create: `edp-gui-app/Features/OwnerSites/OwnedDocumentAttachment.cs`
- Create: `edp-gui-app/Features/OwnerSites/DocumentRequestCatalog.cs`
- Test: `edp-gui-app/edp-gui-app.Tests/DocumentRequestCatalogTests.cs`

- [ ] Write tests asserting the fixed document names include real labels and no blanks.
- [ ] Implement simple records and static catalog.
- [ ] Run `dotnet test edp-gui-app\edp-gui-app.Tests\edp-gui-app.Tests.csproj --filter DocumentRequestCatalogTests`.

## Task 2: Upload Storage

**Files:**
- Create: `edp-gui-app/Features/OwnerSites/DocumentUploadStorage.cs`
- Test: `edp-gui-app/edp-gui-app.Tests/DocumentUploadStorageTests.cs`

- [ ] Write tests for unsafe filename sanitization and tenant/token folder generation.
- [ ] Implement storage using `AppContext.BaseDirectory/DocumentRequests`.
- [ ] Ensure sanitized filenames cannot contain path separators.
- [ ] Run filtered storage tests.

## Task 3: Tenant Persistence

**Files:**
- Modify: `edp-gui-app/Features/Auth/SiteOwnerAuthService.Rooms.cs`
- Modify: `edp-gui-app/Features/OwnerSites/OwnedRoom.cs`
- Test: `edp-gui-app/edp-gui-app.Tests/SiteOwnerRoomTenantServiceTests.cs`

- [ ] Add failing test for `CreateTenantInRoomAsync` storing name, email, address, and contact number.
- [ ] Add failing test for loading tenant details only through owned site/room context.
- [ ] Add schema ensure helpers for nullable `tenant_email`, `tenant_address`, `tenant_contact_number`, and document `file_path`.
- [ ] Update room queries and tenant creation.
- [ ] Add `LoadTenantDetailsAsync`, `LoadTenantDocumentsAsync`, and `CreateTenantDocumentAsync`.
- [ ] Run tenant service tests.

## Task 4: Gmail Document Request Email

**Files:**
- Create: `edp-gui-app/Features/Auth/DocumentRequestEmailMessage.cs`
- Modify: `edp-gui-app/Features/Auth/IEmailSender.cs`
- Modify: `edp-gui-app/Features/Auth/GmailPasswordResetEmailSender.cs`
- Modify tests using `CapturingEmailSender`

- [ ] Add email message record with recipient, tenant name, request URL, and requested document names.
- [ ] Add `SendDocumentRequestAsync` to `IEmailSender`.
- [ ] Implement plain-text Gmail MIME body using existing OAuth token flow.
- [ ] Update test doubles to capture or no-op the new method.
- [ ] Run password recovery tests to ensure no regression.

## Task 5: Local Upload Server

**Files:**
- Create: `edp-gui-app/Features/OwnerSites/DocumentRequestModels.cs`
- Create: `edp-gui-app/Features/OwnerSites/LocalDocumentRequestServer.cs`

- [ ] Implement request registration with generated token.
- [ ] Bind `HttpListener` to `http://localhost:<port>/`.
- [ ] Serve an HTML page that renders upload fields from request state.
- [ ] Accept multipart `POST /upload/<token>`.
- [ ] Save files through `DocumentUploadStorage`.
- [ ] Call service callback to create tenant document rows after each upload.
- [ ] Keep HTML/CSS/JS generation small and static.

## Task 6: Owner UI

**Files:**
- Modify: `edp-gui-app/UI/Shell/MainAppWindow.cs`
- Modify: `edp-gui-app/UI/OwnerSites/MainAppWindow.OwnerSitesDialogs.cs`
- Modify: `edp-gui-app/UI/OwnerSites/MainAppWindow.RiserDetailsPanels.cs`
- Modify: `edp-gui-app/UI/OwnerSites/MainAppWindow.RiserDetailsActions.cs`

- [ ] Add request server field initialized with auth service and upload storage.
- [ ] Replace Add Tenant name-only dialog with name, email, address, and contact number fields.
- [ ] Add tenant-cell mouse move/leave/click behavior for occupied tenants.
- [ ] Add Tenant Details dialog with profile labels, document list, and Send Document Request button.
- [ ] Add checkbox dialog for document request catalog.
- [ ] On send, register request, start server, send Gmail email, and show status.

## Task 7: Verification

**Files:**
- Build/test commands only.

- [ ] Run `dotnet test edp-gui-app\edp-gui-app.Tests\edp-gui-app.Tests.csproj`.
- [ ] Run `dotnet build edp-gui-app\edp-gui-app.csproj`.
- [ ] If tests require local MySQL and fail because unavailable, record that clearly.
- [ ] Manually inspect file lengths and split any file over 300 lines if introduced by this work.
