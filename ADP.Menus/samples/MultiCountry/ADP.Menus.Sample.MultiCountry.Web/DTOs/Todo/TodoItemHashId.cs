using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace ShiftSoftware.ADP.Menus.Sample.MultiCountry.Web.DTOs.Todo;

public class TodoItemHashId : JsonHashIdConverterAttribute<TodoItemHashId>
{
    public TodoItemHashId() : base(5) { }
}
