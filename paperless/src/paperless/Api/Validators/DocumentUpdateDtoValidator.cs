using FluentValidation;
using Paperless.Api;
using Paperless.DAL.Repositories;

namespace paperless.Api.Validators
{
    public class DocumentUpdateDtoValidator : AbstractValidator<DocumentUpdateDto>
    {
        #region Fields
        private readonly IDocumentRepository _repo;
        #endregion

        #region Constructors
        public DocumentUpdateDtoValidator(IDocumentRepository repo)
        {
            _repo = repo;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id is required");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

            When(x => x.Tags != null && x.Tags.Count > 0, () =>
            {
                RuleFor(x => x.Tags)
                    .Must(tags => tags!.Count <= 10).WithMessage("A maximum of 10 tags are allowed");

                RuleForEach(x => x.Tags)
                    .NotEmpty().WithMessage("Empty tags are not allowed")
                    .MaximumLength(30).WithMessage("Each tag must not exceed 30 characters");
            });
        }
        #endregion
    }
}
