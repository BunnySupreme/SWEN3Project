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

            // WIP: Add additional rules
        }
        #endregion
    }
}
