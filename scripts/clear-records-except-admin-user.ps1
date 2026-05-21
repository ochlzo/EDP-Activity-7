param(
    [string]$Database = "site_management",
    [string]$User = "root",
    [string]$HostName = "127.0.0.1",
    [int]$Port = 3306,
    [string]$MySqlPath = "mysql"
)

$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlPath = Join-Path $scriptDirectory "clear-records-except-admin-user.sql"

if (-not (Test-Path -LiteralPath $sqlPath)) {
    throw "SQL cleanup script not found: $sqlPath"
}

$sourcePath = (Resolve-Path -LiteralPath $sqlPath).Path.Replace("\", "/")

Write-Host "This will delete all records from '$Database' except rows in admin_user."
Write-Host "You will be prompted for the MySQL password for user '$User'."

& $MySqlPath `
    "--host=$HostName" `
    "--port=$Port" `
    "--user=$User" `
    "--password" `
    $Database `
    "--execute=source $sourcePath"

if ($LASTEXITCODE -ne 0) {
    throw "MySQL cleanup failed with exit code $LASTEXITCODE."
}

Write-Host "Cleanup finished. admin_user was preserved."
