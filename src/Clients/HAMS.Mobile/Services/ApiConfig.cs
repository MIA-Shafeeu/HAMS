namespace HAMS.Mobile.Services;

/// <summary>
/// Base API address for local development. A real device build needs the school's actual server
/// address here instead (build plan §8: LAN-internal for the staff-facing API, matching how the
/// Blazor Server admin UI is also never exposed through the DMZ) — this constant is a
/// local-development/emulator convenience only, not a production configuration story.
/// </summary>
public static class ApiConfig
{
    // Android emulator's special loopback alias for the host machine; the Windows target and iOS
    // simulator can both reach the host machine directly via "localhost".
    public static readonly string BaseUrl =
#if ANDROID
        "http://10.0.2.2:5080/";
#else
        "http://localhost:5080/";
#endif
}
