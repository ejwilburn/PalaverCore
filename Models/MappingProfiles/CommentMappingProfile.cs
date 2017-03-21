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
        }
    }
}
