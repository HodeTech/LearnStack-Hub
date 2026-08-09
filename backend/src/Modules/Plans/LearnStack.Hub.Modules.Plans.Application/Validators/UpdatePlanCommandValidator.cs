using FluentValidation;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Domain;
using LearnStack.Hub.SharedKernel.FeatureFlags;

namespace LearnStack.Hub.Modules.Plans.Application.Validators;

public sealed class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanCommandValidator()
    {
        RuleFor(c => c.PlanId)
            .NotEmpty()
            .WithErrorCode("lockey_plan_id_required");

        RuleFor(c => c.Name)
            .NotEmpty()
            .WithErrorCode("lockey_plan_name_required")
            .MaximumLength(200)
            .WithErrorCode("lockey_plan_name_too_long");

        RuleFor(c => c.BillingCycle)
            .Must(value => Enum.TryParse<BillingCycle>(value, ignoreCase: true, out _))
            .WithErrorCode("lockey_plan_billing_cycle_invalid");

        RuleFor(c => c.Currency)
            .Must(value => value is { Length: 3 } && value.All(char.IsLetter))
            .WithErrorCode("lockey_plan_currency_invalid");

        RuleFor(c => c.BasePriceUsd)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("lockey_plan_base_price_invalid");

        RuleFor(c => c.Features)
            .Must(map => map is not null && map.Keys.All(FeatureKeys.IsKnown))
            .WithErrorCode("lockey_plan_feature_key_unknown");

        RuleFor(c => c.Limits)
            .Must(map => map is not null && map.Keys.All(LimitKeys.IsKnown))
            .WithErrorCode("lockey_plan_limit_key_unknown");
    }
}
