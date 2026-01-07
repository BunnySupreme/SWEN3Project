using AutoMapper;
using Paperless.Api;
using Paperless.DAL.Models;
using Paperless.Search.Models;

namespace paperless.Api
{
    public class MappingProfile : Profile
    {
        #region Constructors
        public MappingProfile()
        {
            CreateMap<DocumentModel, DocumentReadDto>()
               .ForCtorParam(nameof(DocumentReadDto.Id), opt => opt.MapFrom(src => src.Id))
               .ForCtorParam(nameof(DocumentReadDto.Title), opt => opt.MapFrom(src => src.Title))
               .ForCtorParam(nameof(DocumentReadDto.Summary), opt => opt.MapFrom(src => src.Summary))
               .ForCtorParam(nameof(DocumentReadDto.Tags), opt => opt.MapFrom(src => SplitTags(src.Tags)))
               .ForCtorParam(nameof(DocumentReadDto.UploadedAt), opt => opt.MapFrom(src => src.UploadedAt));

            CreateMap<DocumentSearchModel, DocumentReadDto>()
                .ForCtorParam(nameof(DocumentReadDto.Id), opt => opt.MapFrom(src => src.Id))
                .ForCtorParam(nameof(DocumentReadDto.Title), opt => opt.MapFrom(src => src.Title))
                .ForCtorParam(nameof(DocumentReadDto.Summary), opt => opt.MapFrom(src => src.Summary))
                .ForCtorParam(nameof(DocumentReadDto.Tags), opt => opt.MapFrom(src => SplitTags(src.Tags)))
                .ForCtorParam(nameof(DocumentReadDto.UploadedAt), opt => opt.MapFrom(src => src.UploadedAt));

            CreateMap<DocumentCreateDto, DocumentModel>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Summary, opt => opt.MapFrom(src => src.Summary))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => JoinTags(src.Tags)))
                .ForMember(dest => dest.UserId, opt => opt.Ignore());

            CreateMap<DocumentUpdateDto, DocumentModel>() // No Id mapping, as we do not wish to update the Id
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Summary, opt => opt.MapFrom(src => src.Summary))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => JoinTags(src.Tags)))
                .ForMember(dest => dest.UserId, opt => opt.Ignore());
        }
        #endregion

        #region Helper Methods
        private static IReadOnlyList<string> SplitTags(string? tags)
        {
            // No tags: Empty array (array for efficiency)
            if (string.IsNullOrWhiteSpace(tags)) return Array.Empty<string>();

            // Else: Array of tags
            return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .ToList()
                       .AsReadOnly();
        }

        private static string JoinTags(IReadOnlyList<string>? tags)
        {
            // No tags: Empty string
            if (tags is null || tags.Count == 0) return string.Empty;

            // Else: Comma-separated string
            return string.Join(',', tags);
        }
        #endregion
    }
}
