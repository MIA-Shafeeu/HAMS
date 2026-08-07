using HAMS.Mobile.Models;
using HAMS.Mobile.Services;

namespace HAMS.Mobile.Pages;

public partial class TimetablePage : ContentPage
{
    private readonly AuthService _authService;
    private readonly MobileApiService _api;
    private Guid _schoolId;
    private Guid _academicYearId;

    public TimetablePage(AuthService authService, MobileApiService api)
    {
        InitializeComponent();
        _authService = authService;
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await LoadAsync();
        TimetableRefreshView.IsRefreshing = false;
    }

    private async Task LoadAsync()
    {
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;
        try
        {
            _schoolId = await _api.GetSchoolIdAsync();
            var years = await _api.GetAcademicYearsAsync(_schoolId);
            if (years.Count == 0)
            {
                TimetableList.ItemsSource = Array.Empty<StaffTimetableEntry>();
                return;
            }

            // Most-recently-created academic year — the same "no admin picker yet" simplification
            // the WASM portal's own homework tab makes for its academic-year default.
            _academicYearId = years[^1].Id;

            var entries = await _api.GetMyTimetableAsync(_schoolId, _academicYearId);
            TimetableList.ItemsSource = entries;
        }
        catch (HttpRequestException)
        {
            await DisplayAlertAsync("Connection Error", "Could not reach the server. Check your connection and try again.", "OK");
        }
        finally
        {
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    private async void OnEntryTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not StaffTimetableEntry entry)
        {
            return;
        }

        await Shell.Current.GoToAsync(
            $"{nameof(AttendancePage)}?classId={entry.ClassId}&schoolId={_schoolId}&academicYearId={_academicYearId}&className={Uri.EscapeDataString(entry.ClassName)}");
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}
