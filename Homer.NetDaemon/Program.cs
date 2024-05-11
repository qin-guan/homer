using System.Reflection;
using NetDaemon.AppModel;
using NetDaemon.Extensions.Logging;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.Extensions.Tts;
using NetDaemon.Runtime;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Host.UseNetDaemonAppSettings();
builder.Host.UseNetDaemonDefaultLogging();
builder.Host.UseNetDaemonRuntime();
builder.Host.UseNetDaemonTextToSpeech();

builder.Services.AddProblemDetails();

builder.Services.AddAppsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddNetDaemonStateManager();
builder.Services.AddNetDaemonScheduler();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
