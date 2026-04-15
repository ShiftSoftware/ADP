using AutoMapper;
using ShiftSoftware.ADP.Menus.Sample.API.Data.Entities;
using ShiftSoftware.ADP.Menus.Sample.Web.DTOs.Todo;

namespace ShiftSoftware.ADP.Menus.Sample.API.Data.AutoMapperProfiles;

public class TodoMappingProfile : Profile
{
    public TodoMappingProfile()
    {
        CreateMap<TodoItem, TodoItemDTO>().ReverseMap();
        CreateMap<TodoItem, TodoItemListDTO>();
    }
}
