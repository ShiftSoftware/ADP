using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Menus.Sample.API.Data.Entities;

public class TodoItem : ShiftEntity<TodoItem>
{
    public string Title { get; set; } = default!;
    public string? Notes { get; set; }
    public bool IsDone { get; set; }

    public TodoItem() { }
    public TodoItem(long id) : base(id) { }
}
