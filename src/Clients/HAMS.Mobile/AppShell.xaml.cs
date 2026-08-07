using HAMS.Mobile.Pages;

namespace HAMS.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(MfaPage), typeof(MfaPage));
		Routing.RegisterRoute(nameof(TimetablePage), typeof(TimetablePage));
		Routing.RegisterRoute(nameof(AttendancePage), typeof(AttendancePage));
	}
}
