param(
    [string]$Database = "site_management",
    [string]$User = "root",
    [string]$HostName = "127.0.0.1",
    [int]$Port = 3306,
    [string]$MySqlPath = "mysql"
)

$ErrorActionPreference = "Stop"

function ConvertTo-SqlLiteral {
    param([object]$Value)
    if ($null -eq $Value) { return "NULL" }
    if ($Value -is [string]) { return "'" + $Value.Replace("'", "''") + "'" }
    if ($Value -is [datetime]) { return "'" + $Value.ToString("yyyy-MM-dd HH:mm:ss") + "'" }
    if ($Value -is [bool]) { return $(if ($Value) { "1" } else { "0" }) }
    return $Value.ToString()
}

function Add-SqlLine {
    param([System.Text.StringBuilder]$Builder, [string]$Line = "")
    [void]$Builder.AppendLine($Line)
}

function Add-InsertStatement {
    param([System.Text.StringBuilder]$Builder, [string]$Table, [string[]]$Columns, [object[][]]$Rows)
    if ($Rows.Count -eq 0) { return }
    Add-SqlLine $Builder ("INSERT INTO $Table (" + ($Columns -join ", ") + ") VALUES")
    for ($index = 0; $index -lt $Rows.Count; $index++) {
        $values = ($Rows[$index] | ForEach-Object { ConvertTo-SqlLiteral $_ }) -join ", "
        Add-SqlLine $Builder ("    ($values)" + $(if ($index -lt $Rows.Count - 1) { "," } else { ";" }))
    }
    Add-SqlLine $Builder
}

$emails = @(
    "cholocandelaria@proton.me",
    "johnthetester1@proton.me",
    "bu.braket@proton.me",
    "jbbc2023-4132-17458@bicol-u.edu.ph",
    "cholocandelaria123@gmail.com"
)

$owners = 0..($emails.Count - 1) | ForEach-Object {
    [pscustomobject]@{
        Name = "Mock Owner $($_ + 1)"
        Email = $emails[$_]
    }
}

$siteNames = @(
    "North Tower", "South Tower", "East Annex", "West Annex", "Central Block",
    "Harbor Tower", "Skyline Residences", "Garden Suites", "Lakeside Flats", "Sunrise Point"
)

$roomCount = $siteNames.Count * 2 * 3
$tenantCount = $roomCount

$ticketTitles = @(
    "Leaking pipe", "Elevator inspection", "Lighting repair", "Security keypad",
    "Parking gate service", "Common area repaint", "Water pressure check", "Lobby AC service",
    "Generator test", "Hallway patching"
)

$baseDate = Get-Date "2025-01-01T08:00:00"
$builder = [System.Text.StringBuilder]::new()

Add-SqlLine $builder "-- Reset seeded data before inserting mock records."
Add-SqlLine $builder "SET FOREIGN_KEY_CHECKS = 0;"
foreach ($table in @("activity_log", "maintenance_ticket_status_history", "maintenance_ticket", "room_occupancy_transaction", "document", "room", "riser", "tenant", "site", "site_owner_password_reset", "site_owner")) {
    Add-SqlLine $builder "TRUNCATE TABLE $table;"
}
Add-SqlLine $builder "SET FOREIGN_KEY_CHECKS = 1;"
Add-SqlLine $builder

$ownerRows = foreach ($owner in $owners) {
    ,@($owner.Name, $owner.Email, "password12345", 1)
}
Add-InsertStatement $builder "site_owner" @("owner_name", "owner_email", "password", "is_active") $ownerRows

$tenantRows = foreach ($tenantIndex in 1..$tenantCount) {
    $email = $emails[($tenantIndex - 1) % $emails.Count]
    ,@(
        "Mock Tenant $tenantIndex",
        $email,
        "Unit $tenantIndex, Mock Residences",
        ("0917-555-{0:04}" -f (2000 + $tenantIndex))
    )
}
Add-InsertStatement $builder "tenant" @("tenant_name", "tenant_email", "tenant_address", "tenant_contact_number") $tenantRows

$siteRows = for ($siteIndex = 0; $siteIndex -lt $siteNames.Count; $siteIndex++) {
    ,@($siteNames[$siteIndex], (($siteIndex % $owners.Count) + 1))
}
Add-InsertStatement $builder "site" @("site_name", "owner_id") $siteRows

$riserRows = foreach ($siteIndex in 0..($siteNames.Count - 1)) {
    $siteId = $siteIndex + 1
    foreach ($suffix in @("A", "B")) {
        ,@("$($siteNames[$siteIndex]) Riser $suffix", $siteId)
    }
}
Add-InsertStatement $builder "riser" @("riser_name", "site_id") $riserRows

$roomTenantIds = 1..$roomCount
$roomTenantIds[0] = 2
$roomTenantIds[1] = 3
$roomTenantIds[2] = 1

$roomRows = @()
$roomIndex = 0
foreach ($riserId in 1..($siteNames.Count * 2)) {
    $siteName = $siteNames[[int][math]::Floor(($riserId - 1) / 2)]
    $suffix = if (($riserId % 2) -eq 1) { "A" } else { "B" }
    foreach ($roomNumber in @(101, 102, 103)) {
        $roomRows += ,@("$siteName - Riser $suffix - Room $roomNumber", $riserId, $roomTenantIds[$roomIndex])
        $roomIndex++
    }
}
Add-InsertStatement $builder "room" @("room_name", "riser_id", "tenant_id") $roomRows

$occupancyRows = foreach ($roomId in 1..$roomCount) {
    ,@($roomId, $roomId, "Assigned", $baseDate.AddDays($roomId), "Initial assignment for room $roomId.", "seed-script")
}
$occupancyRows += @(
    ,@(1, 2, "Replaced", $baseDate.AddDays(21), "Replaced tenant 1 with tenant 2 in room 1.", "seed-script")
    ,@(2, 3, "Replaced", $baseDate.AddDays(22), "Replaced tenant 2 with tenant 3 in room 2.", "seed-script")
    ,@(3, 1, "Replaced", $baseDate.AddDays(23), "Replaced tenant 3 with tenant 1 in room 3.", "seed-script")
)
Add-InsertStatement $builder "room_occupancy_transaction" @("room_id", "tenant_id", "transaction_type", "effective_at", "notes", "created_by") $occupancyRows

$documentRows = foreach ($tenantId in 1..$tenantCount) {
    ,@("Lease Agreement for Tenant $tenantId", $tenantId, "Lease", "Submitted", $baseDate.AddDays($tenantId * 2), $baseDate.AddDays($tenantId * 2).AddHours(2), "Lease agreement file for tenant $tenantId.", "")
    ,@("Proof of Billing for Tenant $tenantId", $tenantId, "Compliance", "Pending Submission", $baseDate.AddDays($tenantId * 2 + 1), $null, "Awaiting billing document for tenant $tenantId.", "")
}
Add-InsertStatement $builder "document" @("doc_name", "tenant_id", "doc_type", "doc_status", "issued_at", "submitted_at", "notes", "file_path") $documentRows

$maintenanceRows = foreach ($ticketIndex in 0..($ticketTitles.Count - 1)) {
    $siteId = $ticketIndex + 1
    $riserId = (($siteId - 1) * 2) + 1
    $roomId = (($riserId - 1) * 3) + 1
    $ownerId = (($ticketIndex % $owners.Count) + 1)
    ,@(
        $siteId,
        $riserId,
        $roomId,
        $ownerId,
        $ticketTitles[$ticketIndex],
        "Mock maintenance request for $($ticketTitles[$ticketIndex].ToLower()).",
        $(if (($ticketIndex % 2) -eq 0) { "High" } else { "Normal" }),
        "Open",
        $baseDate.AddDays(30 + $ticketIndex),
        $null
    )
}
Add-InsertStatement $builder "maintenance_ticket" @("site_id", "riser_id", "room_id", "requested_by_owner_id", "title", "description", "priority", "status", "requested_at", "resolved_at") $maintenanceRows

$maintenanceHistoryRows = foreach ($ticketId in 1..$ticketTitles.Count) {
    ,@($ticketId, "Open", "In Progress", "seed-script", $baseDate.AddDays(40 + $ticketId), "Assigned to maintenance queue.")
    ,@($ticketId, "In Progress", "Resolved", "seed-script", $baseDate.AddDays(41 + $ticketId), "Issue resolved in mock data.")
}
Add-InsertStatement $builder "maintenance_ticket_status_history" @("ticket_id", "old_status", "new_status", "changed_by", "changed_at", "notes") $maintenanceHistoryRows

$activityTemplates = @(
    @("Admin", "seed-script", "Created", "Owner", 1, "Seeded owner record."),
    @("Admin", "seed-script", "Created", "Site", 1, "Seeded site record."),
    @("Admin", "seed-script", "Created", "Riser", 1, "Seeded riser record."),
    @("Admin", "seed-script", "Created", "Room", 1, "Seeded room record."),
    @("Admin", "seed-script", "Created", "Tenant", 1, "Seeded tenant record."),
    @("Admin", "seed-script", "Created", "Document", 1, "Seeded document record."),
    @("Admin", "seed-script", "Updated", "Room", 1, "Seeded occupancy transaction."),
    @("Owner", "Mock Owner 1", "Assigned", "Room", 1, "Assigned tenant to room."),
    @("Owner", "Mock Owner 2", "Replaced", "Room", 2, "Replaced room tenant."),
    @("Admin", "seed-script", "Logged", "Maintenance Ticket", 1, "Seeded maintenance record.")
)

$activityRows = foreach ($activity in $activityTemplates) {
    ,@($activity[0], $activity[1], $activity[2], $activity[3], $activity[4], $null, $null, $null, $null, $null, $null, $activity[5], $baseDate.AddDays(60 + [int]$activity[4]))
}
Add-InsertStatement $builder "activity_log" @("actor_type", "actor_name", "action", "entity_type", "entity_id", "owner_id", "site_id", "riser_id", "room_id", "tenant_id", "document_id", "description", "created_at") $activityRows

$tempSqlPath = Join-Path $env:TEMP ("edp-gui-seed-{0}.sql" -f ([Guid]::NewGuid().ToString("N")))
Set-Content -LiteralPath $tempSqlPath -Value $builder.ToString() -Encoding utf8
try {
    $sourcePath = (Resolve-Path -LiteralPath $tempSqlPath).Path.Replace("\", "/")
    Write-Host "This will clear existing non-admin records and seed mock data into '$Database'."
    Write-Host "You will be prompted for the MySQL password for user '$User'."
    & $MySqlPath "--host=$HostName" "--port=$Port" "--user=$User" "--password" $Database "--execute=source $sourcePath"
    if ($LASTEXITCODE -ne 0) { throw "MySQL seed failed with exit code $LASTEXITCODE." }
}
finally {
    if (Test-Path -LiteralPath $tempSqlPath) { Remove-Item -LiteralPath $tempSqlPath -Force }
}

Write-Host "Seeding finished."
