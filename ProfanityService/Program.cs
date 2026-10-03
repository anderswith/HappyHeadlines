using Microsoft.EntityFrameworkCore;
using ProfanityService.BLL;
using ProfanityService.BLL.Interfaces;
using ProfanityService.DAL;
using ProfanityService.DAL.Repositories;
using ProfanityService.DAL.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ProfanityContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ProfanityDatabase"),
        postgres => postgres.CommandTimeout(5)
    );
});

builder.Services.AddScoped<
    IProfanityRepository,
    ProfanityRepository>();

builder.Services.AddScoped<IProfanityLogic, ProfanityLogic>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();