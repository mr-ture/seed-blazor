
using MIF.Modules.Todos.Domain;
namespace MIF.Modules.Todos.Application.DTOs;

public class TodoItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public Importance Importance { get; set; } = Importance.Medium;
}
