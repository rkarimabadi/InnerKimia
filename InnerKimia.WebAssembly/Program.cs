using InnerKimia.Application.Interfaces;
using InnerKimia.Application.Services;
using InnerKimia.Application.Validators;
using InnerKimia.Domain.Services;
using InnerKimia.Infrastructure.Repositories;
using InnerKimia.WebAssembly;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IGameRepository, LocalStorageGameRepository>();
builder.Services.AddScoped<IElementRelationshipService, ElementRelationshipService>();
builder.Services.AddScoped<IPlaceCardValidator, PlaceCardValidator>();
builder.Services.AddScoped<IRemoveStoneValidator, RemoveStoneValidator>();


await builder.Build().RunAsync();
