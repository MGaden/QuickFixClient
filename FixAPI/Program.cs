using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Reflection;
using FixAPI.Services;
using FixAPI.BackgroundServices;
using FixAPI.Hubs;
using FixAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOriginWithCredentials", builder =>
    {
        builder.SetIsOriginAllowed(origin => true) // Allow any origin dynamically
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});


// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
                configuration.ReadFrom.Configuration(context.Configuration).Enrich.WithProperty("Application", context.Configuration.GetValue<string>("Serilog:Properties:Application")));

// Add services
// Configure database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FixConnection")));

// Register repository and service
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddScoped<IExecutionReportRepository, ExecutionReportRepository>();
builder.Services.AddScoped<IExecutionReportService, ExecutionReportService>();

// Add SignalR and the background service to the DI container
builder.Services.AddSignalR();
builder.Services.AddHostedService<ExecutionReportMonitorService>();
// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "FixAPI", Version = "v1" });

    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));

    c.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});

// Configure JWT authentication
#region Authentication
builder.Services.AddAuthentication("Bearer")
   .AddJwtBearer("Bearer", options =>
   {
       // IdentityModelEventSource.ShowPII = true;
       options.Authority = builder.Configuration.GetSection("DerayahIdentityServer:AuthorityServer").Value;
       options.RequireHttpsMetadata = false; // Should use HTTPS on Production
       options.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateAudience = false,
           ValidateIssuer = true,
           ValidateLifetime = false,
           ValidIssuers = builder.Configuration.GetSection("DerayahIdentityServer:AllowedIssuers")?.GetChildren()?.Select(x => x.Value)?.ToList(),
           ValidateIssuerSigningKey = false
       };

       // Enable SignalR JWT authentication
       options.Events = new JwtBearerEvents
       {
           OnMessageReceived = context =>
           {
               // Check if the token is in the query string
               // Check if the token is in the Authorization header (used by SignalR with accessTokenFactory)
               if (context.Request.Headers.ContainsKey("Authorization"))
               {
                   context.Token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
               }
               return Task.CompletedTask;
           }
       };
   });
#endregion
#region Authorization
//  adds an authorization policy to make sure the token is for scope 'FixAPI'
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FixAPI", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", builder.Configuration.GetSection("DerayahIdentityServer:AllowedScopes")?.GetChildren()?.Select(x => x.Value)?.ToList());
    });
});
#endregion

var app = builder.Build();

// Enable CORS for the app
app.UseCors("AllowAnyOriginWithCredentials");

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoint
//app.MapHealthChecks("/health");

// Enable static file serving
app.UseStaticFiles();

// Map SignalR hub
app.MapHub<NotificationHub>("/notifications");

app.MapGet("/", async context =>
{
    await context.Response.WriteAsync("Welcome to FixAPI!");
});

app.MapControllers();

app.Run();
