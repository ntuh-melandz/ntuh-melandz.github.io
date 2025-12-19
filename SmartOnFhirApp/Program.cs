using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SmartOnFhirApp;
using SmartOnFhirApp.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add LocalStorage
builder.Services.AddBlazoredLocalStorage();

// 註冊 SMART on FHIR 服務
builder.Services.AddScoped<SmartAuthService>();
builder.Services.AddScoped<FhirClientService>();
builder.Services.AddScoped<AiSummaryService>();

await builder.Build().RunAsync();
