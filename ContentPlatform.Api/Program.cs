using Carter;
using ContentPlatform.Api.AspireHub;
using ContentPlatform.Api.Database;
using ContentPlatform.Api.Extensions;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.CustomSchemaIds(id => id.FullName!.Replace('+', '-')));
builder.Services.AddCors();
builder.Services.AddSignalR();

builder.Services.AddDbContext<ApplicationDbContext>(o =>
	o.UseNpgsql(builder.Configuration.GetConnectionString("contentplatform-db")));

var assembly = typeof(Program).Assembly;

builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(assembly));

builder.Services.AddCarter();
builder.Services.AddValidatorsFromAssembly(assembly);

builder.Services.AddMassTransit(busConfigurator =>
{
	busConfigurator.SetKebabCaseEndpointNameFormatter();

	busConfigurator.UsingRabbitMq((context, configurator) =>
	{
		configurator.Host(builder.Configuration.GetConnectionString("contentplatform-mq"));

		configurator.ConfigureEndpoints(context);
	});
});

var app = builder.Build();

app.MapDefaultEndpoints();

var allowedOrigins = new[] { "https://localhost:5001", "https://localhost:7098" };

app.UseCors(policy =>
		policy.WithOrigins(allowedOrigins) // Specify allowed origins
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials() // Allow credentials only with specific origins
);

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
	//app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

	app.ApplyMigrations();
}

app.MapCarter();

app.UseHttpsRedirection();
app.MapHub<NotificationHub>("/notificationHub");
app.Run();
