using System.Text.RegularExpressions;
using FluentValidation;
using LearnStack.Hub.Modules.TenantLifecycle.Application.Contracts;
using LearnStack.Hub.SharedKernel.Hosting;

namespace LearnStack.Hub.Modules.TenantLifecycle.Application.Validators;

public sealed partial class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(c => c.Slug)
            .NotEmpty()
            .WithErrorCode("lockey_tenant_slug_required")
            .Must(slug => SlugPattern().IsMatch(slug))
            .WithErrorCode("lockey_tenant_slug_invalid");

        RuleFor(c => c.DisplayName)
            .NotEmpty()
            .WithErrorCode("lockey_tenant_display_name_required")
            .MaximumLength(200)
            .WithErrorCode("lockey_tenant_display_name_too_long");

        RuleFor(c => c.DeploymentMode)
            .Must(IsProductionDeploymentMode)
            .WithErrorCode("lockey_tenant_deployment_mode_invalid");

        RuleFor(c => c.InitialPlanId)
            .NotEmpty()
            .WithErrorCode("lockey_tenant_plan_required");
    }

    // A tenant's deployment_mode takes only the four production values
    // (Development is a host-composition concern, never a tenant attribute).
    private static bool IsProductionDeploymentMode(string value) =>
        Enum.TryParse<DeploymentMode>(value, ignoreCase: true, out var mode)
        && mode is not DeploymentMode.Development;

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
