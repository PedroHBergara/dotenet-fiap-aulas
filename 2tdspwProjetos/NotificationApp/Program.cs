using NotificationApp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapHub<StockHub>("/hubs/stock");

app.Run();