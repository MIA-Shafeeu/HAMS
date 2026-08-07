using HAMS.Mobile.Services;

namespace HAMS.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;

    public LoginPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Already holding a refresh token from a prior session — skip straight past sign-in.
        // BearerTokenHandler silently refreshes the access token on the first real API call if needed.
        if (await _authService.IsLoggedInAsync())
        {
            await Shell.Current.GoToAsync($"//{nameof(TimetablePage)}");
        }
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Enter your username and password.");
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _authService.LoginAsync(username, password);

            if (result.Succeeded)
            {
                await Shell.Current.GoToAsync($"//{nameof(TimetablePage)}");
                return;
            }

            if (result.MfaRequired && result.MfaToken is not null)
            {
                await Shell.Current.GoToAsync($"{nameof(MfaPage)}?mfaToken={Uri.EscapeDataString(result.MfaToken)}");
                return;
            }

            ShowError(result.Error ?? "Sign-in failed.");
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
        SignInButton.IsEnabled = !busy;
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
