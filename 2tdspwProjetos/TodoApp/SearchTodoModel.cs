namespace TodoApp;

public record SearchTodoModel(int totalItems, int page, List<Todo> data);