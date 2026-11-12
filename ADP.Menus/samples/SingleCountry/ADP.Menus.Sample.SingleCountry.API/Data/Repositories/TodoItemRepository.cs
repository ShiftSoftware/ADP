using ShiftSoftware.ADP.Menus.Sample.SingleCountry.API.Data.Entities;
using ShiftSoftware.ADP.Menus.Sample.SingleCountry.Web.DTOs.Todo;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Sample.SingleCountry.API.Data.Repositories;

public class TodoItemRepository : ShiftRepository<ShiftDbContext, TodoItem, TodoItemListDTO, TodoItemDTO>
{
    public TodoItemRepository(ShiftDbContext db) : base(db)
    {
    }
}
