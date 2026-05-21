namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private void ResetProfileState()
    {
        _profileNameTextBox.Clear();
        _profileEmailTextBox.Clear();
        _profileContactNumberTextBox.Clear();
        _profileCurrentPasswordTextBox.Clear();
        _profileNewPasswordTextBox.Clear();
        _profileConfirmPasswordTextBox.Clear();
        ShowProfileStatus(string.Empty, Color.DimGray);
    }

    private void UpdateProfilePanel()
    {
        var owner = _flowController.CurrentOwner;
        if (owner is null)
        {
            ShowProfileStatus("No owner profile is loaded.", Color.Firebrick);
            return;
        }

        _profileNameTextBox.Text = owner.OwnerName;
        _profileEmailTextBox.Text = owner.OwnerEmail;
        _profileContactNumberTextBox.Text = owner.ContactNumber;
        _profileCurrentPasswordTextBox.Clear();
        _profileNewPasswordTextBox.Clear();
        _profileConfirmPasswordTextBox.Clear();
        ShowProfileStatus(string.Empty, Color.DimGray);
    }

    private void ShowProfileStatus(string text, Color color)
    {
        _profileStatusLabel.Text = text;
        _profileStatusLabel.ForeColor = color;
    }
}
