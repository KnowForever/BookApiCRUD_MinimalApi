using FluentValidation;
using Library.Api.Data;
using Library.Api.Endpoints.Internal;

var builder = WebApplication.CreateBuilder(args);

// SERVICES
//

// add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AnyOriginDuuuh", x => x.AllowAnyOrigin());
});

// add IDbConnectionFactory injection
builder.Services.AddSingleton<IDbConnectionFactory>(_ => 
    new SqliteConnectionFactory(builder.Configuration.GetValue<string>("Database:ConnectionString")!));

// add DatabaseInitializer service
builder.Services.AddSingleton<DatabaseInitializer>();

// add my custom services for endpoints
//LibraryEndpoints.AddServices(builder.Services);
//HealthEndpoints.AddServices(builder.Services);
builder.Services.AddEndpoints(typeof(Program), builder.Configuration);

// add all Fluent Validators found in the current assembly
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// MIDDLEWARE
//

// use swagger
app.UseSwagger();
app.UseSwaggerUI();

// use my CORS policy
app.UseCors();

// app use endpoints to create routes
//LibraryEndpoints.DefineEndpoints(app);
//HealthEndpoints.DefineEndpoints(app);
app.UseEndpoints(typeof(Program));

// use DatabaseInitializer service to create SQLite database locally
var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();
