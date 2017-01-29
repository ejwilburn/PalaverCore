using System;
using AutoMapper;
using Palaver.Models;
using Palaver.Models.ThreadViewModels;

namespace Palaver.Models.MappingProfiles
{
    public class ThreadMappingProfile : Profile
    {
        public ThreadMappingProfile()
        {
            CreateMap<Thread, CreateResultViewModel>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.UserName));

            CreateMap<Thread, SelectedViewModel>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.UserName));

            CreateMap<Thread, CreateResultViewModel>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.UserName));

            CreateMap<Thread, StickyChangeViewModel>();
        }
    }
}
