using ShiftSoftware.ADP.Menus.Sample.MultiCountry.API.Data.Entities;
using ShiftSoftware.ADP.Menus.Sample.MultiCountry.Web.DTOs.Todo;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Sample.MultiCountry.API.Data.Repositories;

public class TodoItemRepository : ShiftRepository<ShiftDbContext, TodoItem, TodoItemListDTO, TodoItemDTO>
{
    public TodoItemRepository(ShiftDbContext db) : base(db)
    {
    }
}
