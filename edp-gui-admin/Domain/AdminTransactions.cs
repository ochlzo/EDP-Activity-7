namespace edp_gui_admin;

public sealed record AdminOccupancyTransaction(
    int TransactionId,
    int RoomId,
    string RoomName,
    int? TenantId,
    string TenantName,
    string TransactionType,
    DateTime Date,
    string Notes);

public sealed record AdminOccupancyHistoryRow(
    int HistoryId,
    string TenantName,
    int? TenantId,
    string RoomName,
    int RoomId,
    DateTime DateOccupied,
    string Notes);

public sealed record AdminMaintenanceTicket(
    int TicketId,
    string SiteName,
    string Title,
    string Description,
    string Priority,
    string Status,
    DateTime RequestedAt,
    DateTime? ResolvedAt,
    string Notes);

public sealed record AdminMaintenanceHistory(
    int HistoryId,
    int TicketId,
    string OldStatus,
    string NewStatus,
    string ChangedBy,
    DateTime ChangedAt,
    string Notes);

public sealed record AdminActivityLog(
    int ActivityId,
    string ActorType,
    string ActorName,
    string Action,
    string EntityType,
    int EntityId,
    string Description,
    DateTime CreatedAt);

public sealed record AdminReportRow(
    string Category,
    string ParentName,
    string ItemName,
    string Status,
    DateTime? Date,
    string Notes);
