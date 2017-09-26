/*
Copyright 2017, E.J. Wilburn, Marcus McKinnon, Kevin Williams
This program is distributed under the terms of the GNU General Public License.

This file is part of Palaver.

Palaver is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

Palaver is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Palaver.  If not, see <http://www.gnu.org/licenses/>.
*/

using AutoMapper;
using PalaverCore.Models.ThreadViewModels;

namespace PalaverCore.Models.MappingProfiles
{
    public class ThreadMappingProfile : Profile
    {
        public ThreadMappingProfile()
        {
            CreateMap<Thread, CreateResultViewModel>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.UserName));
            CreateMap<Thread, SelectedViewModel>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.UserName))
                .ForMember(d => d.Comments, opt => opt.MapFrom(s => s.ImmediateChildren));
            CreateMap<Thread, ListViewModel>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.UserName))
                .ForMember(d => d.hasUnread, opt => opt.MapFrom(s => s.UnreadCount > 0));
            CreateMap<Thread, StickyChangeViewModel>();
        }
    }
}
