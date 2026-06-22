using MudBlazor.Services;
using SmartDocQA.Core.Interfaces;
using SmartDocQA.Core.Services;
using SmartDocQA.Core.Models;
using SmartDocQA.Infrastructure.AI;
using SmartDocQA.Infrastructure.Documents;
using SmartDocQA.Web;

// Load environment variables from .env
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddHttpClient();

// Register ALL services here:
builder.Services.AddScoped<DocumentSessionState>();
builder.Services.AddScoped<SqliteQueryEngine>();
builder.Services.AddScoped<ILLMService, LLMService>();
builder.Services.AddScoped<IVectorStore, VectorStoreService>();
builder.Services.AddScoped<IGroqService, GroqService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IGoogleAIService, GoogleAIService>();
builder.Services.AddScoped<IOllamaService, OllamaService>();

builder.Services.AddScoped<IDocumentProcessor, DocumentProcessor>();
builder.Services.AddScoped<IPdfProcessor, PdfProcessor>();
builder.Services.AddScoped<IExcelProcessor, ExcelProcessor>();
builder.Services.AddScoped<IDocxProcessor, DocxProcessor>();
builder.Services.AddScoped<ICsvProcessor, CsvProcessor>();
builder.Services.AddScoped<IImageOcrProcessor, ImageOcrProcessor>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<SmartDocQA.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();