namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private static IReadOnlyList<DialogOption> BuildOwnerOptions(IEnumerable<AdminOwner> owners) =>
        owners.Select(owner => new DialogOption(owner.OwnerId.ToString(), $"{owner.OwnerName} ({owner.OwnerEmail})")).ToArray();

    private static IReadOnlyList<DialogOption> BuildSiteOptions(IEnumerable<AdminSite> sites) =>
        sites.Select(site => new DialogOption(site.SiteId.ToString(), $"{site.SiteName} ({site.OwnerName})")).ToArray();

    private static IReadOnlyList<DialogOption> BuildRiserOptions(IEnumerable<AdminRiser> risers) =>
        risers.Select(riser => new DialogOption(riser.RiserId.ToString(), $"{riser.RiserName} ({riser.SiteName})")).ToArray();

    private static IReadOnlyList<DialogOption> BuildTenantOptions(
        IEnumerable<AdminTenant> tenants,
        bool includeVacant = false)
    {
        var options = tenants
            .Select(tenant => new DialogOption(tenant.TenantId.ToString(), tenant.TenantName))
            .ToList();

        if (includeVacant)
        {
            options.Insert(0, new DialogOption(string.Empty, "Vacant"));
        }

        return options;
    }

    private static void EnsureLookupOptions(IReadOnlyList<DialogOption> options, string missingMessage)
    {
        if (options.Count == 0)
        {
            throw new InvalidOperationException(missingMessage);
        }
    }
}
