using ConsensusAlgorithm.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConsensusRelatedServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Making the Program class public using a partial class declaration
// for using in Integration Tests
public partial class Program { }
