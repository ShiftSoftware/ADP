using ShiftSoftware.ADP.Menus.Sample.API.Data.Entities;
using ShiftSoftware.ADP.Menus.Sample.API.Data.Repositories;
using ShiftSoftware.ADP.Menus.Sample.Web.DTOs.Todo;
using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.Menus.Sample.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TodoItemController : ShiftEntityControllerAsync<TodoItemRepository, TodoItem, TodoItemListDTO, TodoItemDTO>
{
}
