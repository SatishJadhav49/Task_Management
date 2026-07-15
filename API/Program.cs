using Taskmanagement_API.Data;
using Taskmanagement_API.Utils;
using Taskmanagement_API.Middleware;
using Taskmanagement_API.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Preserve original property naming (PascalCase with underscores)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add connection string as a service
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();


// Configure SMTP Settings
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// Register Data Services
builder.Services.AddScoped<MM_EmployeeDataService>();
builder.Services.AddScoped<MM_ErrorLogDataService>();
builder.Services.AddScoped<MM_ActivityDetailsService>();
builder.Services.AddScoped<MM_TaskDetailsService>();
builder.Services.AddScoped<MM_Daily_StatusDataService>();
builder.Services.AddScoped<MM_ShiftDataService>();
builder.Services.AddScoped<MM_DeploymentDataService>();
builder.Services.AddScoped<MM_DeploymentNotificationService>();
builder.Services.AddSingleton<IEmailService, EmailService>();

// Register Utils Services
builder.Services.AddSingleton<IApiResponseHelper, ApiResponseHelper>();

// Add CORS
builder.Services.AddCors(options =>
{   
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
