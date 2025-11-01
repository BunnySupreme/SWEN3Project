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

            // Don't forget rule for ID here

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

            // WIP: Add additional rules
        }
        #endregion
    }
}
