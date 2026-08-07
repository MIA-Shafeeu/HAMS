using HAMS.Platform.Workflow.Application;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.Platform.Workflow;

/// <summary>
/// Registration entry point for the Workflow platform kernel. Every business module and
/// `HAMS.WebHost` may depend on this; per the plan's kernel design, this is reused rather than
/// re-implemented per-module.
/// </summary>
public static class PlatformWorkflowExtensions
{
    /// <summary>Registers the Workflow kernel's <see cref="IWorkflowEngine"/> — stateless, so a singleton is safe and avoids a needless per-request allocation.</summary>
    public static IServiceCollection AddPlatformWorkflow(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowEngine, WorkflowEngine>();
        return services;
    }
}
