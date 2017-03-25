using AutoMapper;
using Palaver.Models.CommentViewModels;

namespace Palaver.Models.MappingProfiles
{
    public class CommentMappingProfile : Profile
    {
        public CommentMappingProfile()
        {
            CreateMap<Comment, CreateResultViewModel>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.UserName))
                .ForMember(d => d.EmailHash, opt => opt.MapFrom(s => s.User.EmailHash));
            CreateMap<Comment, CreateViewModel>();
            CreateMap<Comment, DetailViewModel>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.UserName))
                .ForMember(d => d.EmailHash, opt => opt.MapFrom(s => s.User.EmailHash));
            CreateMap<Comment, EditResultViewModel>();
            CreateMap<Comment, SearchResultViewModel>()
                .ForMember(d => d.Title, opt => opt.MapFrom(s => $"[{s.CreatedDisplay}] {s.User.UserName} - {s.Thread.Title}"))
                .ForMember(d => d.Url, opt => opt.MapFrom(s => $"Thread/{s.ThreadId}/{s.Id}"));
        }
    }
}
