namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private async Task AddOwnerAsync()
    {
        var values = ShowRecordDialog("Add Site Owner",
            ("name", "Name", string.Empty),
            ("email", "Email", string.Empty),
            ("password", "Password", string.Empty));
        if (values is not null)
        {
            await _recordService.CreateOwnerAsync(values["name"], values["email"], values["password"]);
        }
    }

    private async Task EditOwnerAsync(AdminOwner owner)
    {
        var values = ShowRecordDialog("Edit Site Owner",
            ("name", "Name", owner.OwnerName),
            ("email", "Email", owner.OwnerEmail),
            ("password", "Password", string.Empty));
        if (values is not null)
        {
            await _recordService.UpdateOwnerAsync(owner.OwnerId, values["name"], values["email"], values["password"]);
        }
    }

    private async Task AddSiteAsync()
    {
        var ownerOptions = BuildOwnerOptions(await _recordService.LoadOwnersAsync());
        EnsureLookupOptions(ownerOptions, "Create a site owner before adding a site.");

        var values = ShowRecordDialog("Add Site",
            DialogField.Text("siteName", "Site Name", string.Empty),
            DialogField.Combo("ownerId", "Owner", ownerOptions[0].Value, ownerOptions));
        if (values is not null)
        {
            await _recordService.CreateSiteAsync(values["siteName"], ParseRequiredInt(values, "ownerId"));
        }
    }

    private async Task EditSiteAsync(AdminSite site)
    {
        var ownerOptions = BuildOwnerOptions(await _recordService.LoadOwnersAsync());
        EnsureLookupOptions(ownerOptions, "Create a site owner before editing a site.");

        var values = ShowRecordDialog("Edit Site",
            DialogField.Text("siteName", "Site Name", site.SiteName),
            DialogField.Combo("ownerId", "Owner", site.OwnerId.ToString(), ownerOptions));
        if (values is not null)
        {
            await _recordService.UpdateSiteAsync(
                site.SiteId,
                values["siteName"],
                ParseRequiredInt(values, "ownerId"));
        }
    }

    private async Task AddRiserAsync()
    {
        var siteOptions = BuildSiteOptions(await _recordService.LoadSitesAsync());
        EnsureLookupOptions(siteOptions, "Create a site before adding a riser.");

        var values = ShowRecordDialog("Add Riser",
            DialogField.Text("riserName", "Riser Name", string.Empty),
            DialogField.Combo("siteId", "Site", siteOptions[0].Value, siteOptions));
        if (values is not null)
        {
            await _recordService.CreateRiserAsync(values["riserName"], ParseRequiredInt(values, "siteId"));
        }
    }

    private async Task EditRiserAsync(AdminRiser riser)
    {
        var siteOptions = BuildSiteOptions(await _recordService.LoadSitesAsync());
        EnsureLookupOptions(siteOptions, "Create a site before editing a riser.");

        var values = ShowRecordDialog("Edit Riser",
            DialogField.Text("riserName", "Riser Name", riser.RiserName),
            DialogField.Combo("siteId", "Site", riser.SiteId.ToString(), siteOptions));
        if (values is not null)
        {
            await _recordService.UpdateRiserAsync(
                riser.RiserId,
                values["riserName"],
                ParseRequiredInt(values, "siteId"));
        }
    }

    private async Task AddRoomAsync()
    {
        var riserOptions = BuildRiserOptions(await _recordService.LoadRisersAsync());
        EnsureLookupOptions(riserOptions, "Create a riser before adding a room.");
        var tenantOptions = BuildTenantOptions(await _recordService.LoadTenantsAsync(), true);

        var values = ShowRecordDialog("Add Room",
            DialogField.Text("roomName", "Room Name", string.Empty),
            DialogField.Combo("riserId", "Riser", riserOptions[0].Value, riserOptions),
            DialogField.Combo("tenantId", "Tenant", string.Empty, tenantOptions));
        if (values is not null)
        {
            await _recordService.CreateRoomAsync(
                values["roomName"],
                ParseRequiredInt(values, "riserId"),
                ParseOptionalInt(values, "tenantId"));
        }
    }

    private async Task EditRoomAsync(AdminRoom room)
    {
        var riserOptions = BuildRiserOptions(await _recordService.LoadRisersAsync());
        EnsureLookupOptions(riserOptions, "Create a riser before editing a room.");
        var tenantOptions = BuildTenantOptions(await _recordService.LoadTenantsAsync(), true);

        var values = ShowRecordDialog("Edit Room",
            DialogField.Text("roomName", "Room Name", room.RoomName),
            DialogField.Combo("riserId", "Riser", room.RiserId.ToString(), riserOptions),
            DialogField.Combo("tenantId", "Tenant", room.TenantId?.ToString() ?? string.Empty, tenantOptions));
        if (values is not null)
        {
            var tenantId = ParseOptionalInt(values, "tenantId");
            await _recordService.UpdateRoomAsync(
                room.RoomId,
                values["roomName"],
                ParseRequiredInt(values, "riserId"),
                room.TenantId);

            if (tenantId != room.TenantId)
            {
                if (tenantId is null)
                {
                    await _transactionService.VacateRoomAsync(room.RoomId, "admin", "Vacated from admin room edit.");
                }
                else
                {
                    await _transactionService.AssignTenantToRoomAsync(
                        room.RoomId,
                        tenantId.Value,
                        "admin",
                        "Assigned from admin room edit.");
                }
            }
        }
    }

    private async Task AddTenantAsync()
    {
        var values = ShowRecordDialog("Add Tenant",
            ("tenantName", "Tenant Name", string.Empty),
            ("tenantEmail", "Email", string.Empty),
            ("tenantAddress", "Address", string.Empty),
            ("tenantContactNumber", "Contact Number", string.Empty));
        if (values is not null)
        {
            await _recordService.CreateTenantAsync(
                values["tenantName"],
                values["tenantEmail"],
                values["tenantAddress"],
                values["tenantContactNumber"]);
        }
    }

    private async Task EditTenantAsync(AdminTenant tenant)
    {
        var values = ShowRecordDialog("Edit Tenant",
            ("tenantName", "Tenant Name", tenant.TenantName),
            ("tenantEmail", "Email", tenant.TenantEmail),
            ("tenantAddress", "Address", tenant.TenantAddress),
            ("tenantContactNumber", "Contact Number", tenant.TenantContactNumber));
        if (values is not null)
        {
            await _recordService.UpdateTenantAsync(
                tenant.TenantId,
                values["tenantName"],
                values["tenantEmail"],
                values["tenantAddress"],
                values["tenantContactNumber"]);
        }
    }

    private async Task AddDocumentAsync()
    {
        var tenantOptions = BuildTenantOptions(await _recordService.LoadTenantsAsync());
        EnsureLookupOptions(tenantOptions, "Create a tenant before adding a document.");

        var values = ShowRecordDialog("Add Document",
            DialogField.Text("documentName", "Document Name", string.Empty),
            DialogField.Combo("tenantId", "Tenant", tenantOptions[0].Value, tenantOptions),
            DialogField.Text("documentType", "Type", "General"),
            DialogField.Text("documentStatus", "Status", "Active"),
            DialogField.Text("notes", "Notes", string.Empty));
        if (values is not null)
        {
            await _recordService.CreateDocumentAsync(
                values["documentName"],
                ParseRequiredInt(values, "tenantId"),
                values["documentType"],
                values["documentStatus"],
                values["notes"]);
        }
    }

    private async Task EditDocumentAsync(AdminDocument document)
    {
        var tenantOptions = BuildTenantOptions(await _recordService.LoadTenantsAsync());
        EnsureLookupOptions(tenantOptions, "Create a tenant before editing a document.");

        var values = ShowRecordDialog("Edit Document",
            DialogField.Text("documentName", "Document Name", document.DocumentName),
            DialogField.Combo("tenantId", "Tenant", document.TenantId.ToString(), tenantOptions),
            DialogField.Text("documentType", "Type", document.DocumentType),
            DialogField.Text("documentStatus", "Status", document.DocumentStatus),
            DialogField.Text("notes", "Notes", document.Notes));
        if (values is not null)
        {
            await _recordService.UpdateDocumentAsync(
                document.DocumentId,
                values["documentName"],
                ParseRequiredInt(values, "tenantId"),
                values["documentType"],
                values["documentStatus"],
                values["notes"]);
        }
    }
}
