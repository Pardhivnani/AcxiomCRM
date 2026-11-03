using AcxiomCRM.Data;
using AcxiomCRM.Middleware;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Implementations;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration
var configuredProvider = builder.Configuration["DatabaseProvider"] ?? "Auto";
var sqlServerConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=AcxiomCRM.db";

bool useSqlServer = false;

if (configuredProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    useSqlServer = true;
}
else if (configuredProvider.Equals("Auto", StringComparison.OrdinalIgnoreCase))
{
    // Check if SQL Server is accessible with a fast ping/test
    try
    {
        using var testConn = new SqlConnection(sqlServerConnection);
        testConn.Open();
        useSqlServer = true;
    }
    catch
    {
        useSqlServer = false;
    }
}

if (useSqlServer)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(sqlServerConnection));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(sqliteConnection));
}

// 2. Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".AcxiomCRM.AuthCookie";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// 3. Register Application Services
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IUserScopeService, UserScopeService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<IFollowUpService, FollowUpService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IUserService, UserService>();

// 4. MVC and API Controllers
builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AcxiomCRM REST API",
        Version = "v1",
        Description = "Secured REST API for AcxiomCRM Enterprise Platform"
    });
});

var app = builder.Build();

// 5. Database Initialization & Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        await DbInitializer.SeedAsync(services);
        logger.LogInformation("AcxiomCRM database initialized and seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the AcxiomCRM database.");
    }
}

// 6. HTTP Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

// Swagger Documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcxiomCRM API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
