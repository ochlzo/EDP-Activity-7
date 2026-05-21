namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private Form BuildEditTenantDialog(
        int ownerId,
        int siteId,
        OwnedTenant tenant)
    {
        var fields = new Dictionary<string, TextBox>
        {
            ["Name"] = new() { Text = tenant.Name },
            ["Email"] = new() { Text = tenant.Email },
            ["Address"] = new() { Text = tenant.Address },
            ["Contact Number"] = new() { Text = tenant.ContactNumber }
        };

        return BuildTenantFormDialog(
            "Edit Tenant",
            "Update",
            fields,
            async values => await _authService.UpdateTenantDetailsAsync(
                tenant.TenantId,
                siteId,
                ownerId,
                values["Name"],
                values["Email"],
                values["Address"],
                values["Contact Number"]));
    }

    private Form BuildReplaceTenantDialog(
        int ownerId,
        int siteId,
        OwnedRoom room)
    {
        var fields = new Dictionary<string, TextBox>
        {
            ["Name"] = new(),
            ["Email"] = new(),
            ["Address"] = new(),
            ["Contact Number"] = new()
        };

        return BuildTenantFormDialog(
            "Replace Tenant",
            "Replace",
            fields,
            async values => await _authService.ReplaceTenantInRoomAsync(
                room.RoomId,
                siteId,
                ownerId,
                values["Name"],
                values["Email"],
                values["Address"],
                values["Contact Number"]));
    }
}
