using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Sample.MultiCountry.Web.DTOs.Todo;

public class TodoItemDTO : ShiftEntityViewAndUpsertDTO
{
    [TodoItemHashId]
    public override string? ID { get; set; }

    [Required]
    public string Title { get; set; } = default!;

    public string? Notes { get; set; }

    public bool IsDone { get; set; }
}
