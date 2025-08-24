using FluentValidation;
using FluentValidation.AspNetCore;
using Hellang.Middleware.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Todo.Application.Contracts;
using Todo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var isTesting = builder.Environment.IsEnvironment("Testing");

 // EF Core (SQL Server) – csak ha nem Testing
 if (!isTesting)
 {
    builder.Services.AddDbContext<TodoDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("TodoDb")));
 }

// MVC + JSON
builder.Services.AddControllers()
    .AddJsonOptions(o => { o.JsonSerializerOptions.PropertyNamingPolicy = null; });

// Validáció
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTodoRequestValidator>();

// Swagger + ProblemDetails
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails(options =>
{
    options.IncludeExceptionDetails = (ctx, ex) => builder.Environment.IsDevelopment();
});

var app = builder.Build();

app.UseProblemDetails();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

// a teszt hostnak kell
public partial class Program { }
