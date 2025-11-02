using FluentValidation;
using Paperless.Api;
using Paperless.DAL.Repositories;

namespace paperless.Api.Validators
{
    public class DocumentCreateDtoValidator : AbstractValidator<DocumentCreateDto>
    {
        #region Fields
        private readonly IDocumentRepository _repo;
        #endregion

        #region Constructors
        public DocumentCreateDtoValidator(IDocumentRepository repo)
        {
            _repo = repo;

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

            // Commented out until automatic summary generation is implemented
            //RuleFor(x => x.Summary)
            //    .NotEmpty().WithMessage("Summary is required");

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
