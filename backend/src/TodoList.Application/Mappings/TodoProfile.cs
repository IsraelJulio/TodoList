using AutoMapper;
using TodoList.Application.DTOs;
using TodoList.Domain.Entities;

namespace TodoList.Application.Mappings;

public class TodoProfile : Profile
{
    public TodoProfile()
    {
        CreateMap<TodoItem, TodoItemDto>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title.Value))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
