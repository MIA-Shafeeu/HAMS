using HAMS.Mobile.Services;

namespace HAMS.Mobile.Pages;

[QueryProperty(nameof(MfaToken), "mfaToken")]
public partial class MfaPage : ContentPage
{
    private readonly AuthService _authService;

    public string MfaToken { get; set; } = "";

    public MfaPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnVerifyClicked(object? sender, EventArgs e)
    {
        var code = CodeEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            ShowError("Enter the 6-digit code.");
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _authService.VerifyMfaAsync(MfaToken, code);

            if (result.Succeeded)
            {
                await Shell.Current.GoToAsync($"//{nameof(TimetablePage)}");
                return;
            }

            ShowError(result.Error ?? "Invalid authentication code.");
        }
        catch (HttpRequestException)
        {
            ShowError("Could not reach the server. Check your connection and try again.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        VerifyButton.IsEnabled = !busy;
        BusyIndicator.IsRunning = busy;
        BusyIndicator.IsVisible = busy;
        if (busy)
        {
            ErrorLabel.IsVisible = false;
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
