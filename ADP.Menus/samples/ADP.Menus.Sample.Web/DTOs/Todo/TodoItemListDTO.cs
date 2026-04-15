using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Menus.Sample.Web.DTOs.Todo;

[ShiftEntityKeyAndName(nameof(ID), nameof(Title))]
public class TodoItemListDTO : ShiftEntityListDTO
{
    [TodoItemHashId]
    public override string? ID { get; set; }

    public string Title { get; set; } = default!;
    public bool IsDone { get; set; }
}
