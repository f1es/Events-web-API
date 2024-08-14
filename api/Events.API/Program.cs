using Events.API.Extensions;
using Events.Infrastructure.Extensions;
using Events.Infrastructure.Context;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Events.Application.Extensions;
using Events.Application.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(nameof(JwtOptions)));
builder.Services.Configure<RefreshTokenOptions>(builder.Configuration.GetSection(nameof(RefreshTokenOptions)));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureValidators();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.ConfigureRepositories();
builder.Services.ConfigureServices();
builder.Services.ConfigureApiAuthentication(builder.Configuration);

builder.Services.AddDbContext<EventsDBContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("sqlConnection"));
});
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCookiePolicy(new CookiePolicyOptions()
{
	MinimumSameSitePolicy = SameSiteMode.Strict,
	HttpOnly = HttpOnlyPolicy.Always,
	Secure = CookieSecurePolicy.Always
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
