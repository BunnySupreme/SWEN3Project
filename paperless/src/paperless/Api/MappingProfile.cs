using AutoMapper;
using Paperless.Api;
using Paperless.DAL.Models;

namespace paperless.Api
{
    public class MappingProfile : Profile
    {
        #region Constructors
        public MappingProfile()
        {
            CreateMap<DocumentModel, DocumentReadDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.Summary, opt => opt.MapFrom(src => src.Summary))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => SplitTags(src.Tags)))
                .ForMember(dest => dest.UploadedAt, opt => opt.MapFrom(src => src.UploadedAt));

            CreateMap<DocumentCreateDto, DocumentModel>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.Summary, opt => opt.MapFrom(src => src.Summary))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => JoinTags(src.Tags)));

            CreateMap<DocumentUpdateDto, DocumentModel>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.Summary, opt => opt.MapFrom(src => src.Summary))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => JoinTags(src.Tags)));
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
