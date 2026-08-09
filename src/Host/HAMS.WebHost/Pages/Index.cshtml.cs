using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages;

public sealed class IndexModel : PageModel
{
    public IActionResult OnGet()
        => Redirect(User.Identity?.IsAuthenticated == true ? "/dashboard" : "/account/login");
}
