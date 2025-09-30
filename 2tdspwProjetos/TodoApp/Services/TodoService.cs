using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TodoApp.Protos;

namespace TodoApp.Services;

public class TodoService : Protos.TodoService.TodoServiceBase
{
    private readonly TodoDb _db;
    private readonly ILogger<TodoService> _logger;

    public TodoService(TodoDb db, ILogger<TodoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public override async Task<GetAllTodosResponse> GetAllTodos(GetAllTodosRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received gRPC request to get all todos");
        
        var response = new GetAllTodosResponse();
        var todos = await _db.Todos.ToListAsync();
        
        foreach (var todo in todos)
        {
            response.Todos.Add(new TodoItem
            {
                Id = todo.Id,
                Name = todo.Name ?? string.Empty,
                IsComplete = todo.IsComplete
            });
        }
        
        return response;
    }

    public override async Task<GetCompleteTodosResponse> GetCompleteTodos(GetCompleteTodosRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received gRPC request to get complete todos");
        
        var response = new GetCompleteTodosResponse();
        var completeTodos = await _db.Todos
            .Where(t => t.IsComplete)
            .ToListAsync();
        
        foreach (var todo in completeTodos)
        {
            response.Todos.Add(new TodoItem
            {
                Id = todo.Id,
                Name = todo.Name ?? string.Empty,
                IsComplete = todo.IsComplete
            });
        }
        
        return response;
    }

    public override async Task<GetTodoByIdResponse> GetTodoById(GetTodoByIdRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received gRPC request to get todo by id: {Id}", request.Id);
        
        var response = new GetTodoByIdResponse();
        var todo = await _db.Todos.FindAsync(request.Id);
        
        if (todo == null)
        {
            response.Error = $"Todo with ID {request.Id} not found";
            return response;
        }
        
        response.Todo = new TodoItem
        {
            Id = todo.Id,
            Name = todo.Name ?? string.Empty,
            IsComplete = todo.IsComplete
        };
        
        return response;
    }

    public override async Task<CreateTodoResponse> CreateTodo(CreateTodoRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received gRPC request to create a todo");
        
        var todo = new Todo
        {
            Name = request.Name,
            IsComplete = request.IsComplete
        };
        
        _db.Todos.Add(todo);
        await _db.SaveChangesAsync();
        
        return new CreateTodoResponse
        {
            Todo = new TodoItem
            {
                Id = todo.Id,
                Name = todo.Name ?? string.Empty,
                IsComplete = todo.IsComplete
            }
        };
    }

    public override async Task<UpdateTodoResponse> UpdateTodo(UpdateTodoRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received gRPC request to update todo with id: {Id}", request.Id);
        
        var response = new UpdateTodoResponse();
        var todo = await _db.Todos.FindAsync(request.Id);
        
        if (todo == null)
        {
            response.Error = $"Todo with ID {request.Id} not found";
            return response;
        }
        
        todo.Name = request.Name;
        todo.IsComplete = request.IsComplete;
        
        await _db.SaveChangesAsync();
        
        response.Success = true;
        return response;
    }

    public override async Task<DeleteTodoResponse> DeleteTodo(DeleteTodoRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received gRPC request to delete todo with id: {Id}", request.Id);
        
        var response = new DeleteTodoResponse();
        var todo = await _db.Todos.FindAsync(request.Id);
        
        if (todo == null)
        {
            response.Error = $"Todo with ID {request.Id} not found";
            return response;
        }
        
        _db.Todos.Remove(todo);
        await _db.SaveChangesAsync();
        
        response.Success = true;
        return response;
    }
}
