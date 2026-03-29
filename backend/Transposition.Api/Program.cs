using Transposition.Api.Services;
using Transposition.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------------
// Services
// ------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// In-memory job queue shared between controllers and the background worker.
builder.Services.AddSingleton<IAnalysisJobQueue, InMemoryAnalysisJobQueue>();

// Core skill-analysis service (stateless, safe to use as a singleton).
builder.Services.AddSingleton<IResumeAnalysisService, ResumeAnalysisService>();

// Event-driven background worker — processes queued resume-analysis jobs
// as soon as a thread is available.
builder.Services.AddHostedService<ResumeAnalysisWorker>();

// Allow the React frontend (default CRA / Vite dev server ports) to call the API.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ------------------------------------------------------------------
// Pipeline
// ------------------------------------------------------------------
var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program accessible to the test project
public partial class Program { }
