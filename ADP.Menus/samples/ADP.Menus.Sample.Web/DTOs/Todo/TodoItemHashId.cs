using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace ShiftSoftware.ADP.Menus.Sample.Web.DTOs.Todo;

public class TodoItemHashId : JsonHashIdConverterAttribute<TodoItemHashId>
{
    public TodoItemHashId() : base(5) { }
}
