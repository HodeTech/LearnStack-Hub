using FluentValidation;
using LearnStack.Hub.Modules.Subscriptions.Application.Contracts;

namespace LearnStack.Hub.Modules.Subscriptions.Application.Validators;

public sealed class StartTrialCommandValidator : AbstractValidator<StartTrialCommand>
{
    public StartTrialCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty().WithErrorCode("lockey_subscription_tenant_required");
        RuleFor(c => c.PlanId).NotEmpty().WithErrorCode("lockey_subscription_plan_required");
        RuleFor(c => c.TrialDays).GreaterThan(0).WithErrorCode("lockey_subscription_trial_days_invalid");
    }
}
