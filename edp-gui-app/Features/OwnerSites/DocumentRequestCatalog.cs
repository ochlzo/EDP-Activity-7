namespace edp_gui_app;

public static class DocumentRequestCatalog
{
    public static IReadOnlyList<string> RequestableDocuments { get; } =
    [
        "Valid Government ID",
        "Proof of Billing",
        "Lease Agreement",
        "Business Permit",
        "Authorization Letter",
        "Tax Identification Document"
    ];
}
