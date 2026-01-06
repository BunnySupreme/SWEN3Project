using FluentValidation;

namespace paperless.Api.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator() {
            RuleFor(x => x.Username)
                .NotEmpty()
                .MaximumLength(64);
            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);
        }
    }
}
