using PISM.Data;
using PISM.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPismData(builder.Configuration.GetConnectionString("Default")!);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
