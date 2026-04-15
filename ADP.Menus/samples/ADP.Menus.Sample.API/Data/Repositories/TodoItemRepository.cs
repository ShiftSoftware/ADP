using ShiftSoftware.ADP.Menus.Sample.API.Data.Entities;
using ShiftSoftware.ADP.Menus.Sample.Web.DTOs.Todo;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Sample.API.Data.Repositories;

public class TodoItemRepository : ShiftRepository<ShiftDbContext, TodoItem, TodoItemListDTO, TodoItemDTO>
{
    public TodoItemRepository(ShiftDbContext db) : base(db)
    {
    }
}
