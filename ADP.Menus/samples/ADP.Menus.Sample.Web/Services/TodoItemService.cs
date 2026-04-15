using ShiftSoftware.ADP.Menus.Sample.Web.DTOs.Todo;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;

namespace ShiftSoftware.ADP.Menus.Sample.Web.Services;

public class TodoItemService
{
    private readonly HttpClient http;

    public TodoItemService(HttpClient http)
    {
        this.http = http;
    }

    public async Task<List<TodoItemListDTO>?> GetListAsync()
    {
        var url = $"TodoItem?filter={nameof(TodoItemListDTO.IsDeleted)} eq false";
        var response = await http.GetFromJsonAsync<ODataDTO<TodoItemListDTO>>(url);
        return response?.Value;
    }
}
