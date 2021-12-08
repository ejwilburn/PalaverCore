/*
Copyright 2021, E.J. Wilburn, Marcus McKinnon, Kevin Williams
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

using System;
using AutoMapper;
using PalaverCore.Models.CommentViewModels;

namespace PalaverCore.Models.MappingProfiles
{
    public class CommentMappingProfile : Profile
    {
        public CommentMappingProfile()
        {
            CreateMap<Comment, CreateViewModel>();
            CreateMap<Comment, DetailViewModel>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.UserName))
                .ForMember(d => d.EmailHash, opt => opt.MapFrom(s => s.User.EmailHash))
                .ForMember(d => d.Url, opt => opt.MapFrom(s => $"{Startup.SiteRoot}/Thread/{s.ThreadId}/{s.Id}"));
            CreateMap<Comment, EditViewModel>();
            CreateMap<Comment, EditResultViewModel>();
            CreateMap<Comment, SearchResultViewModel>()
                .ForMember(d => d.Title, opt => opt.MapFrom(s => $"[{s.CreatedDisplay}] {s.User.UserName} - {s.Thread.Title}"))
                .ForMember(d => d.Url, opt => opt.MapFrom(s => $"{Startup.SiteRoot}/Thread/{s.ThreadId}/{s.Id}"));
        }
    }
}
