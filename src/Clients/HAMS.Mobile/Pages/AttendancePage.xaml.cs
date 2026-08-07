using HAMS.Mobile.Models;
using HAMS.Mobile.Services;

namespace HAMS.Mobile.Pages;

/// <summary>Marks daily attendance for one class — reached only by tapping a timetable entry (build plan Phase 14), never via a standalone class picker, so classId/schoolId/academicYearId always arrive as navigation query parameters.</summary>
[QueryProperty(nameof(TeachingClassId), "classId")]
[QueryProperty(nameof(SchoolId), "schoolId")]
[QueryProperty(nameof(AcademicYearId), "academicYearId")]
[QueryProperty(nameof(ClassName), "className")]
public partial class AttendancePage : ContentPage
{
    private const string PresentStatusCode = "PRESENT";

    private readonly MobileApiService _api;
    private readonly List<(ClassRosterEntry Student, Picker StatusPicker, Entry NotesEntry)> _rows = [];
    private List<AttendanceStatusOption> _statusOptions = [];

    public string TeachingClassId { get; set; } = "";
    public string SchoolId { get; set; } = "";
    public string AcademicYearId { get; set; } = "";
    public string ClassName { get; set; } = "";

    public AttendancePage(MobileApiService api)
    {
        InitializeComponent();
        _api = api;
        AttendanceDatePicker.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ClassNameLabel.Text = Uri.UnescapeDataString(ClassName);
        await LoadAsync();
    }

    private async void OnDateSelected(object? sender, DateChangedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;
        RosterScrollView.IsVisible = false;
        try
        {
            if (_statusOptions.Count == 0)
            {
                _statusOptions = (await _api.GetActiveAttendanceStatusesAsync()).ToList();
            }

            var classId = Guid.Parse(TeachingClassId);
            var asOf = DateOnly.FromDateTime(AttendanceDatePicker.Date.GetValueOrDefault(DateTime.Today));
            var roster = await _api.GetClassRosterAsync(classId, asOf);

            BuildRosterRows(roster);
        }
        catch (HttpRequestException)
        {
            await DisplayAlertAsync("Connection Error", "Could not reach the server. Check your connection and try again.", "OK");
        }
        finally
        {
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
            RosterScrollView.IsVisible = true;
        }
    }

    private void BuildRosterRows(IReadOnlyList<ClassRosterEntry> roster)
    {
        RosterStack.Children.Clear();
        _rows.Clear();

        var statusNames = _statusOptions.Select(s => s.Name).ToList();
        var defaultIndex = _statusOptions.FindIndex(s => s.Code == PresentStatusCode);

        foreach (var student in roster)
        {
            var nameLabel = new Label { Text = student.NameEn, VerticalOptions = LayoutOptions.Center };
            var statusPicker = new Picker { ItemsSource = statusNames, SelectedIndex = defaultIndex >= 0 ? defaultIndex : 0, WidthRequest = 130 };
            var notesEntry = new Entry { Placeholder = "Notes (optional)" };

            var grid = new Grid { Padding = new Thickness(0, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(nameLabel, 0);
            Grid.SetRow(nameLabel, 0);
            Grid.SetColumn(statusPicker, 1);
            Grid.SetRow(statusPicker, 0);
            Grid.SetColumn(notesEntry, 0);
            Grid.SetColumnSpan(notesEntry, 2);
            Grid.SetRow(notesEntry, 1);

            grid.Children.Add(nameLabel);
            grid.Children.Add(statusPicker);
            grid.Children.Add(notesEntry);

            RosterStack.Children.Add(grid);
            _rows.Add((student, statusPicker, notesEntry));
        }
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        if (_rows.Count == 0)
        {
            return;
        }

        SubmitButton.IsEnabled = false;
        try
        {
            var schoolId = Guid.Parse(SchoolId);
            var academicYearId = Guid.Parse(AcademicYearId);
            var date = DateOnly.FromDateTime(AttendanceDatePicker.Date.GetValueOrDefault(DateTime.Today));
            var failures = new List<string>();

            foreach (var (student, statusPicker, notesEntry) in _rows)
            {
                if (statusPicker.SelectedIndex < 0)
                {
                    continue;
                }

                var selectedStatus = _statusOptions[statusPicker.SelectedIndex];
                var request = new MarkDailyAttendanceRequest(schoolId, student.StudentPersonId, date, academicYearId, selectedStatus.Code, notesEntry.Text);
                var (success, error) = await _api.MarkAttendanceAsync(request);
                if (!success)
                {
                    failures.Add($"{student.NameEn}: {error}");
                }
            }

            await DisplayAlertAsync(
                failures.Count == 0 ? "Attendance Saved" : "Some Entries Failed",
                failures.Count == 0 ? "Attendance recorded for the whole class." : string.Join("\n", failures),
                "OK");
        }
        catch (HttpRequestException)
        {
            await DisplayAlertAsync("Connection Error", "Could not reach the server. Check your connection and try again.", "OK");
        }
        finally
        {
            SubmitButton.IsEnabled = true;
        }
    }
}
