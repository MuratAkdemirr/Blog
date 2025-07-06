using System.Net;
using System.Net.Mail;
using Blog;
using Blog.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityApiEndpoints<IdentityUser>(opt =>
    {
        opt.Password.RequiredLength = 1;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireLowercase = false;
        opt.Password.RequiredUniqueChars = 0;
        opt.Password.RequireDigit = false;
        opt.Password.RequiredUniqueChars = 0;
        opt.SignIn.RequireConfirmedEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalhostCors", policy =>
    {
        policy.SetIsOriginAllowed(origin => origin.Contains("localhost"))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Blog API",
        Version = "v1",
        Description = "Blog uygulaması için API dokümantasyonu"
    });
    c.EnableAnnotations();
});

builder.Services.Configure<SecurityStampValidatorOptions>(opt => opt.ValidationInterval = TimeSpan.Zero);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var smtpSettings = builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
builder.Services
    .AddFluentEmail(smtpSettings.FromEmail, smtpSettings.FromName)
    .AddRazorRenderer()
    .AddSmtpSender(new SmtpClient(smtpSettings.Host, smtpSettings.Port)
    {
        EnableSsl = true,
        Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password),
    });

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await IdentitySeedData.SeedAsync(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Admin seeding error: {ex.Message}");
    }
}

app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blog API v1"); });

app.UseRouting();
app.UseCors("LocalhostCors");
app.UseAuthorization();

app.MapControllers();
app.MapGroup("Auth").MapIdentityApi<IdentityUser>().WithTags("Auth");

app.Run();