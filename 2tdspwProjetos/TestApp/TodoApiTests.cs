using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApp;

namespace TestApp;

public class TodoApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TodoApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetTodos_ShouldReturnEmptyList()
    {
        //Arrange
        _client.DefaultRequestHeaders.Add("X-API-Key", "minha-chave-secreta1");
        
        // Act
        var request = await _client.GetAsync("/todoitems");
        
        // Assert
        request.EnsureSuccessStatusCode();
        var todos = await request.Content.ReadFromJsonAsync<List<Todo>>();
        Assert.NotNull(todos);
        Assert.Empty(todos);
    }

    [Fact]
    public async Task GetTodos_ShouldReturnListWithItems()
    {
        // Arrange
        var newTodo = new Todo() { Name = "varrer a casa", IsComplete = true };
        
        var IdempotencyKey = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("IdempotencyKey", IdempotencyKey);
        _client.DefaultRequestHeaders.Add("X-API-Key", "minha-chave-secreta1");

        var postResponse = await _client.PostAsJsonAsync("/todoItems", newTodo);

            
        //Act
        var request = await _client.GetAsync("/todoitems");
        
        //Assert
        postResponse.EnsureSuccessStatusCode(); // Verificar se o código está entre a faixa 2xx
        var todos = await request.Content.ReadFromJsonAsync<List<Todo>>();
        Assert.NotNull(todos);
        Assert.NotEmpty(todos);
        Assert.Single(todos);
        
        //Clean up
        await _client.DeleteAsync($"/todoItems/{todos[0].Id}");
    }

    [Fact]
    
    public async Task PostTodo_ShouldCreateNewTodo()
    {
        // Arrange
        var newTodo = new Todo() { Name = "varrer a casa", IsComplete = true };
        // Adding Bogus to create a random newTodo
        var faker = new Bogus.Faker();
        newTodo.Name = faker.Internet.UserName();
        newTodo.IsComplete = faker.Random.Bool();
        var IdempotencyKey = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("IdempotencyKey", IdempotencyKey);
            
        //Act
        var postResponse = await _client.PostAsJsonAsync("/todoItems", newTodo);
        
        //Assert
        postResponse.EnsureSuccessStatusCode(); // Verificar se o código está entre a faixa 2xx
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var createdTodo = await postResponse.Content.ReadFromJsonAsync<Todo>();
        Assert.NotNull(createdTodo);
        Assert.Equal(newTodo.Name, createdTodo.Name);
    }
}