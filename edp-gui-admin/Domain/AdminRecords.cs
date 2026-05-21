namespace edp_gui_admin;

public sealed class AdminOwner
{
    public AdminOwner(int ownerId, string ownerName, string ownerEmail, bool isActive)
    {
        OwnerId = ownerId;
        OwnerName = ownerName;
        OwnerEmail = ownerEmail;
        IsActive = isActive;
    }

    public int OwnerId { get; }

    public string OwnerName { get; }

    public string OwnerEmail { get; }

    public bool IsActive { get; private set; }

    public string Status
    {
        get => IsActive ? "Active" : "Inactive";
        set => IsActive = string.Equals(value, "Active", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record AdminSite(int SiteId, string SiteName, int OwnerId, string OwnerName);

public sealed record AdminRiser(int RiserId, string RiserName, int SiteId, string SiteName);

public sealed record AdminRoom(
    int RoomId,
    string RoomName,
    int RiserId,
    string RiserName,
    int? TenantId,
    string TenantName)
{
    public string Occupancy => TenantId is null ? "Vacant" : "Occupied";
}

public sealed record AdminTenant(
    int TenantId,
    string TenantName,
    string TenantEmail = "",
    string TenantAddress = "",
    string TenantContactNumber = "");

public sealed record AdminDocument(
    int DocumentId,
    string DocumentName,
    int TenantId,
    string TenantName,
    string DocumentType = "General",
    string DocumentStatus = "Active",
    DateTime? IssuedAt = null,
    DateTime? SubmittedAt = null,
    string Notes = "");
