column name: riser
attributes:
riser_id PK
riser_name
site_id FK to site

column name: site
attributes:
site_id PK
site_name
owner_id FK to site_owner

column name: site_owner
attributes:
owner_id PK
owner_name
owner_email
contact_number
password

column name: site_owner_password_reset
attributes:
reset_id PK
owner_id FK to site_owner
token_hash
expires_at
used_at
created_at

column name: room
attributes:
room_id PK
room_name
riser_id FK to riser
tenant_id FK to tenant

column name: tenant
attributes:
tenant_id PK
tenant_name
tenant_email
tenant_address
tenant_contact_number

column name: document
attributes:
document_id PK
doc_name
tenant_id FK to tenant
doc_type
doc_status
issued_at
submitted_at
notes
file_path
created_at
updated_at

column name: room_occupancy_transaction
attributes:
occupancy_transaction_id PK
room_id FK to room
tenant_id FK to tenant nullable
transaction_type
Date
notes
created_by
created_at

column name: maintenance_ticket
attributes:
ticket_id PK
site_id FK to site
riser_id FK to riser nullable
room_id FK to room nullable
requested_by_owner_id FK to site_owner
title
description
priority
status
requested_at
resolved_at
notes

column name: maintenance_ticket_status_history
attributes:
history_id PK
ticket_id FK to maintenance_ticket
old_status
new_status
changed_by
changed_at
notes

column name: activity_log
attributes:
activity_id PK
actor_type
actor_name
action
entity_type
entity_id
owner_id nullable
site_id nullable
riser_id nullable
room_id nullable
tenant_id nullable
document_id nullable
description
created_at

report-ready query:
AdminTransactionService.LoadReportRowsAsync returns a flat export shape:
category, parent_name, item_name, status, Date, notes
