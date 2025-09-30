// See https://aka.ms/new-console-template for more information
using _5_EF_Core;

Console.WriteLine("Hello World");


var db = new BloggingContext();

Console.WriteLine($"Database path: {db.DbPath}.");

//Create
var blog = new Blog (){Url = "http://blogs.msdn.com/adonet"};
db.Blogs.Add(blog);
await db.SaveChangesAsync();