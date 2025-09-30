using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TodoApp;

public class Todo
{
    [property: Required]
    [property: Description("Identificador único da tarefa")]
    public int Id { get; set; }
    
    [property: MaxLength(120)]
    [property: Description("Nome da tarefa")]
    public string? Name { get; set; }
    
    [property: DefaultValue(false)]
    [property: Description("Status da tarefa")]
    public bool IsComplete { get; set; }
}