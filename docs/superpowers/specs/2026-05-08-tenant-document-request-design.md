# Tenant Document Request Design

## Goal

Add an owner-app workflow that lets site owners request tenant documents by email. The owner selects documents from a fixed in-app list, the app sends a Gmail OAuth email containing a same-machine local form URL, and uploaded files are saved to a local folder.

## Scope

- App: `edp-gui-app`
- Entry point: `Riser Details`
- Recipient: tenant assigned to an occupied room
- Delivery: existing Gmail OAuth setup
- Form hosting: local HTTP server for same-machine testing only
- Upload storage: local filesystem folder owned by the app

Out of scope:

- Public internet access for tenants
- LAN/mobile-device access
- Production-grade authentication for the upload link
- Admin-app changes unless needed to keep shared schema assumptions valid

## User Flow

1. Site owner opens `Riser Details`.
2. Tenant cells for occupied rooms behave like links: hover underline and click opens tenant details.
3. Tenant details shows name, email, address, contact number, and document attachments.
4. Owner clicks `Send Document Request`.
5. Popup shows a fixed checklist of requestable document names.
6. Owner submits; app creates a request token, starts or reuses the local upload server, and sends an email to the tenant.
7. Email includes a `http://localhost:<port>/request/<token>` URL.
8. Tenant opens the URL on the same machine, uploads only the requested document files, and submits.
9. Files are saved locally under a request-specific folder and associated with the tenant.

## Data Model

Extend tenant data in owner app service reads/writes:

- `tenant_name`
- `tenant_email`
- `tenant_address`
- `tenant_contact_number`

Existing databases may only have `tenant_name`, so owner app startup or service operations should ensure these nullable columns exist before using them.

Use existing `document` rows for uploaded-document visibility where practical. Uploaded-file paths may require a new nullable `file_path` column on `document`; if added, it should be schema-ensured like existing document compliance columns.

## Components

- `OwnedTenant`: tenant details record for the owner app.
- `OwnedDocumentAttachment`: document metadata and local file path for Tenant Details.
- Tenant service methods in `SiteOwnerAuthService`:
  - load tenant details with ownership validation
  - create tenant in room with four fields
  - load tenant document attachments
  - save uploaded document metadata
- Document request definitions:
  - fixed list in one code location, for example `DocumentRequestCatalog`
  - initial names: Valid Government ID, Proof of Billing, Lease Agreement, Business Permit, Authorization Letter, Tax Identification Document
- Gmail document request sender:
  - extend email abstraction or add a new sender method
  - reuse Gmail OAuth credentials already loaded from environment
- Local upload server:
  - listens on localhost only
  - serves one HTML/CSS/JS page for request tokens
  - accepts multipart uploads
  - writes files to a local folder

## UI Design

`Add Tenant` becomes a four-field dialog:

- Name
- Email
- Address
- Contact Number

`Riser Details` keeps the existing room grid. Tenant cell behavior changes only when the row has a tenant:

- hover cursor and underline styling
- click opens Tenant Details

Tenant Details can be a focused dialog to minimize navigation churn and file count. It shows profile values, document attachments, and a `Send Document Request` action.

## Local Upload Server

The server is intended for same-machine testing. It should bind to localhost and use a fixed configurable port or select an available port.

Request state can be held in memory for the running app. That is acceptable for the current same-machine test scope; if the app restarts, old request links can expire.

Uploaded files should be saved with sanitized filenames under:

`DocumentRequests/<tenant-id>/<request-token>/`

The upload handler should prevent path traversal and reject missing or unknown request tokens.

## Error Handling

- Add Tenant validates required name and email; address/contact can be required if the UI labels imply complete tenant details.
- Send request requires at least one checked document.
- Gmail not configured shows a clear owner-facing error.
- Upload server failures show a clear owner-facing error before sending email.
- Upload page shows a simple success or failure message after submission.

## Testing

Add focused MSTest coverage for:

- tenant creation stores name, email, address, and contact number
- loading tenant details enforces site-owner ownership
- document request catalog returns expected fixed names
- request form generation includes only selected documents
- upload path sanitization prevents unsafe paths

Manual verification:

- build owner app
- run owner tests
- create tenant from Riser Details
- click tenant cell and open details
- send request email through Gmail OAuth
- open localhost link and upload files
- confirm files appear in local storage and tenant details
