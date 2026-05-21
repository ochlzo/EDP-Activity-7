namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private async Task ShowProfileViewAsync()
    {
        _flowController.ShowProfile();
        ApplyState();
        await ReloadOwnerProfileAsync();
    }

    private async Task ReloadOwnerProfileAsync()
    {
        var owner = _flowController.CurrentOwner;
        if (owner is null)
        {
            ShowProfileStatus("No owner profile is loaded.", Color.Firebrick);
            return;
        }

        try
        {
            SetBusy(true);
            ShowProfileStatus("Loading profile...", Color.DimGray);

            var profile = await _authService.LoadOwnerProfileAsync(owner.OwnerId);
            _flowController.ShowProfile(profile);
            ApplyState();
        }
        catch (Exception ex)
        {
            ShowProfileStatus($"Could not load profile: {ex.Message}", Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnSaveProfileDetailsClicked(object? sender, EventArgs e)
    {
        var owner = _flowController.CurrentOwner;
        if (owner is null)
        {
            ShowProfileStatus("No owner profile is loaded.", Color.Firebrick);
            return;
        }

        var ownerName = _profileNameTextBox.Text.Trim();
        var contactNumber = _profileContactNumberTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(ownerName))
        {
            ShowProfileStatus("Owner name is required.", Color.Firebrick);
            return;
        }

        try
        {
            SetBusy(true);
            ShowProfileStatus("Saving profile details...", Color.DimGray);

            await _authService.UpdateOwnerDetailsAsync(owner.OwnerId, ownerName, contactNumber);
            await ReloadOwnerProfileAsync();
            ShowProfileStatus("Profile details updated.", Color.DimGray);
        }
        catch (Exception ex)
        {
            ShowProfileStatus($"Could not update profile details: {ex.Message}", Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnChangeProfileEmailClicked(object? sender, EventArgs e)
    {
        var owner = _flowController.CurrentOwner;
        if (owner is null)
        {
            ShowProfileStatus("No owner profile is loaded.", Color.Firebrick);
            return;
        }

        var email = _profileEmailTextBox.Text.Trim();
        var currentPassword = _profileCurrentPasswordTextBox.Text;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(currentPassword))
        {
            ShowProfileStatus("New email and current password are required.", Color.Firebrick);
            return;
        }

        try
        {
            SetBusy(true);
            ShowProfileStatus("Updating email...", Color.DimGray);

            await _authService.UpdateOwnerEmailAsync(owner.OwnerId, currentPassword, email);
            await ReloadOwnerProfileAsync();
            ShowProfileStatus("Email updated.", Color.DimGray);
        }
        catch (Exception ex)
        {
            ShowProfileStatus($"Could not update email: {ex.Message}", Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnChangeProfilePasswordClicked(object? sender, EventArgs e)
    {
        var owner = _flowController.CurrentOwner;
        if (owner is null)
        {
            ShowProfileStatus("No owner profile is loaded.", Color.Firebrick);
            return;
        }

        var currentPassword = _profileCurrentPasswordTextBox.Text;
        var newPassword = _profileNewPasswordTextBox.Text;
        var confirmPassword = _profileConfirmPasswordTextBox.Text;
        if (string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            ShowProfileStatus("Current password, new password, and confirmation are required.", Color.Firebrick);
            return;
        }

        if (newPassword != confirmPassword)
        {
            ShowProfileStatus("Passwords do not match.", Color.Firebrick);
            return;
        }

        try
        {
            SetBusy(true);
            ShowProfileStatus("Updating password...", Color.DimGray);

            await _authService.UpdateOwnerPasswordAsync(owner.OwnerId, currentPassword, newPassword);
            await ReloadOwnerProfileAsync();
            ShowProfileStatus("Password updated.", Color.DimGray);
        }
        catch (Exception ex)
        {
            ShowProfileStatus($"Could not update password: {ex.Message}", Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }
}
