using MIF.SharedKernel.Domain;

namespace MIF.Modules.Todos.Domain;

public class TodoItem : EntityBase
{
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
