using CertificateMailer.Services;
using MailSender.Interfaces;
using MailSender.Models;
using MailSender.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services .Configure<EmailSettings>( builder.Configuration .GetSection("EmailSettings"));
// ============================================================ // Dependency Injection // ============================================================ 
builder.Services .AddScoped<IExcelService, ExcelService>();
builder.Services .AddScoped<ICertificateService, CertificateService>();
builder.Services .AddScoped<IEmailService, EmailService>();
builder.Services .AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddSingleton<SendProgressService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();