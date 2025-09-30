#region Imports
using System.ComponentModel;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.Builder;
using HealthChecks.UI.Client;
using HealthChecks.UI.Configuration;
using IdempotentAPI.Cache.DistributedCache.Extensions.DependencyInjection;
using IdempotentAPI.Core;
using IdempotentAPI.Extensions.DependencyInjection;
using IdempotentAPI.MinimalAPI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using TodoApp;
using TodoApp.Services;
#endregion

#region Service Configuration

var builder = WebApplication.CreateBuilder(args);

#region GRPC Note
// Add gRPC services
builder.Services.AddGrpc();
#endregion

#region  Database
builder.Services.AddDbContext<TodoDb>(optionsBuilder =>
    optionsBuilder.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
#endregion

#region Idempotency
builder.Services.AddIdempotentMinimalAPI(new IdempotencyOptions());
builder.Services.AddDistributedMemoryCache();
// Register the IdempotentAPI.Cache.DistributedCache.
builder.Services.AddIdempotentAPIUsingDistributedCache();
#endregion

#region Health Checks & Health Checks UI
builder.Services.AddHealthChecks()
    .AddOracle(
            connectionString: builder.Configuration.GetConnectionString("OracleFiapDbConnection") ?? "",
            name: "oracle-database",
            healthQuery: "SELECT 1 FROM dual",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "db", "oracle", "sql" },
            timeout: TimeSpan.FromSeconds(10)
        );

builder.Services.AddHealthChecksUI(opt =>
    {
        opt.SetEvaluationTimeInSeconds(10);
        opt.MaximumHistoryEntriesPerEndpoint(60);
        opt.SetApiMaxActiveRequests(1);
        opt.AddHealthCheckEndpoint("feedback-api", "/health");
    }
).AddInMemoryStorage();

#endregion

#region CORS

// Configure CORS for both REST API and gRPC
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding"
                , "Content-Type", "Accept", "Authorization");
    });
});

#endregion

#region Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(policyName: "fixed", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromSeconds(60);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        
        await context.HttpContext.Response.WriteAsync(
            $"Limite de requisições excedido para o IP: {ipAddress}. Tente novamente após 60 segundos.",
            cancellationToken);
    };
});
#endregion

#region API Versioning 
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new HeaderApiVersionReader("X-api-version");
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});
#endregion

#region OpenAPI/Swagger
builder.Services.AddOpenApi(option =>
{
    option.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Todo API",
            Version = "v1",
            Description = "API de gerenciamento de tarefas",
            License = new OpenApiLicense() {
                Name = "MIT",
                Url = new Uri("https://opensource.org/licenses/MIT")
            },
            Contact = new OpenApiContact() {
                Name = "TodoApi Support Team",
                Email = "support@todoapp.io",
                Url = new Uri("https://todoapp.io/support")
            }
        };
        return Task.CompletedTask;
    });
});
#endregion

var validKeys = builder.Configuration.GetSection("ApiKeys").Get<string[]>()
                ?? Array.Empty<string>();

#endregion

#region Build App
var app = builder.Build();
#endregion

#region  API Key Middleware

app.Use(async (ctx, next) =>
{
    var endpoint = ctx.GetEndpoint();

        var allowAnonymous = endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null;
        if (allowAnonymous)
        {
            await next();
            return;
        }

        if (ctx.Request.GetDisplayUrl().Contains("todoitems"))
        {
            if (!ctx.Request.Headers.TryGetValue("X-API-Key", out var provided) ||
                !validKeys.Contains(provided.ToString()))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { error = "API Key is missing or invalid." });
                return;
            }
        }

        await next();
        return;
});

#endregion

#region Dev Tools & Docs
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
#endregion

#region Middleware Pipeline
// Configure the HTTP request pipeline
app.UseRouting();
app.UseCors("AllowAll");
app.UseRateLimiter();
#endregion

#region Infrastructure Endpoints
// gRPC
app.MapGrpcService<TodoService>().RequireCors("AllowAll");
// Health Checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecksUI(delegate(Options options)
{
    options.UIPath = "/health-ui";
});
#endregion

#region API Versioning Setup
ApiVersionSet versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))
    .ReportApiVersions()
    .Build();
#endregion

#region Root Endpoint
app.MapGet("/", () => "Hello World! This app provides both REST and gRPC APIs for Todo management.")
    .WithName("greetings").RequireRateLimiting("fixed");
#endregion

#region Todo V1 Endpoints
var todogroup = app.MapGroup("/todoitems").WithTags("Todo").RequireRateLimiting("fixed")
    .WithApiVersionSet(versionSet)
    .MapToApiVersion(1,0);

todogroup.MapGet("/",
        async (TodoDb db) => TypedResults.Ok(await db.Todos.ToListAsync()))
    .WithSummary("Retorna todas as tarefas")
    .WithDescription("Retorna todas as tarefas cadastradas no banco de dados")
    .AllowAnonymous();

todogroup.MapGet("/search", async (TodoDb db, string name = "", int page = 0) =>
{
    var pageSize = 2;
    var results =  await db
        .Todos
        .Skip(pageSize * (page-1))
        .Take(pageSize)
        .Where(todo => 
            todo.Name != null && todo.Name
                .Contains(name))
        .ToListAsync();

    var totalItems = await db.Todos.Where(todo => todo.Name != null && todo.Name.Contains(name)).CountAsync();
    return new SearchTodoModel(totalItems, page, results);
});

todogroup.MapGet("/complete", async (TodoDb db) =>
    await db.Todos.Where(todo => todo.IsComplete).ToListAsync());

todogroup.MapGet("/{id:int}/", async Task<Results<Ok<Todo>, NotFound>>  ([Description("Id da tarefa buscada")]int id,TodoDb db) 
        => await
    db.Todos.FindAsync(id) is {  } todo 
        ? TypedResults.Ok(todo) 
        : TypedResults.NotFound() 
)
.WithSummary("Busca a tarefa por um Id")
.WithDescription("Busca a tarefa por Id, se não encontrada retorna NotFound");

todogroup.MapPost("/", async (Todo todo) =>
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TodoDb>();

    db.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todoitems/{todo.Id}", todo);
})
.Accepts<Todo>("application/json")
.AddEndpointFilter<IdempotentAPIEndpointFilter>()
.WithSummary("Adiciona uma nova tarefa")
.Produces<Todo>(StatusCodes.Status201Created);

todogroup.MapPut("/{id:int}", async (int id, Todo todo, TodoDb db) =>
{
    var td = await db.Todos.FindAsync(id);
    
    if (td is null)
        return Results.NotFound();
    
    td.Name = todo.Name;
    td.IsComplete = todo.IsComplete;
    await db.SaveChangesAsync();
    return Results.NoContent();
}).Accepts<Todo>(
    "application/json", 
    ["application/xml"]);

todogroup.MapDelete("/{id:int}", async (int id, TodoDb db) =>
{
    var td = await db.Todos.FindAsync(id);
    if (td is null)
        return Results.NotFound();

    db.Remove(td);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
#endregion

#region Todo V2 Endpoints
app.MapGet("todoItems/",
        async (TodoDb db, HttpContext ctx) =>
        {
            var results = await db.Todos.ToListAsync();
            ctx.Response.Headers.Append("X-total-count", results.Count.ToString());
            return TypedResults.Ok(results); 
        })
    .WithSummary("Retorna todas as tarefas")
    .WithDescription("Retorna todas as tarefas cadastradas no banco de dados")
    .WithApiVersionSet(versionSet)
    .MapToApiVersion(2,0);
#endregion

#region Run
app.Run();
return;
#endregion

public partial class Program();