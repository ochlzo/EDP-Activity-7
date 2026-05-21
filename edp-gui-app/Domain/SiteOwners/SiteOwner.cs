namespace edp_gui_app;

public sealed record SiteOwner(int OwnerId, string OwnerName, string OwnerEmail, string ContactNumber = "");
