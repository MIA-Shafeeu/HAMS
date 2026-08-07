using Microsoft.JSInterop;

namespace HAMS.WebHost.Client.Services;

/// <summary>Triggers a browser file download from an in-memory byte array — Blazor WASM has no built-in equivalent, so a small JS helper (wwwroot/js/downloadFile.js) does the actual anchor-click trick.</summary>
public sealed class FileDownloader(IJSRuntime jsRuntime)
{
    public async Task DownloadAsync(string fileName, string contentType, byte[] bytes)
    {
        await using var module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/downloadFile.js");
        await module.InvokeVoidAsync("downloadFileFromBase64", fileName, contentType, Convert.ToBase64String(bytes));
    }
}
