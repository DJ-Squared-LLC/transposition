# Transposition – Architecture

> **Resume-to-role skill calibration.** Translates an applicant's resume experience into the skill language of a target job role, distinguishing between *what the person can do* (transferable skills) and *how they currently do it* (tools), then recommends upskilling paths where gaps exist.

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Frontend – React + TypeScript](#frontend--react--typescript)
3. [Backend – C# .NET Web API](#backend--c-net-web-api)
4. [Event-Based Processing Model](#event-based-processing-model)
5. [Skill vs. Tool Distinction](#skill-vs-tool-distinction)
6. [Upskilling Recommendations](#upskilling-recommendations)
7. [Data Flow](#data-flow)
8. [Project Structure](#project-structure)
9. [Running Locally](#running-locally)
10. [Future Considerations](#future-considerations)

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         Browser                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │          React + TypeScript  (Vite)                       │  │
│  │  • Resume upload form                                     │  │
│  │  • Job role builder                                       │  │
│  │  • Skill comparison table                                 │  │
│  │  • Upskilling recommendations panel                       │  │
│  └──────────────────────┬────────────────────────────────────┘  │
└─────────────────────────┼───────────────────────────────────────┘
                          │  HTTP / JSON
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│                   C# .NET 8  Web API                            │
│                                                                 │
│  POST /api/resume  ──►  ResumeController                        │
│                              │ Enqueue(AnalysisJob)             │
│                              ▼                                  │
│                    InMemoryAnalysisJobQueue ◄─── SemaphoreSlim  │
│                              │                   (WorkAvailable)│
│                              ▼                                  │
│                    ResumeAnalysisWorker  (BackgroundService)    │
│                              │ Task.Run(...)                    │
│                              ▼                                  │
│                    ResumeAnalysisService                        │
│                    • Skill ↔ requirement matching               │
│                    • Tool alignment check                       │
│                    • Upskill recommendation generation          │
│                              │                                  │
│  GET  /api/resume/{id}  ◄────┘ (poll for result)               │
└─────────────────────────────────────────────────────────────────┘
```

---

## Frontend – React + TypeScript

| Concern | Technology |
|---|---|
| Language | TypeScript 5 |
| UI framework | React 18 |
| Build tool | Vite |
| Styling | Plain CSS (no runtime dependency) |
| HTTP client | `fetch` (browser-native) |
| State management | React `useState` hooks |

### Key Components

| Component | Purpose |
|---|---|
| `ResumeForm` | Collects applicant name, email, summary and free-form experience entries. Each entry captures a *description*, one or more *skills*, and the *tools* used to demonstrate them. |
| `JobRoleForm` | Defines the target role: title, department, description and a list of skill requirements — each pairing a transferable *skill* with the employer's *preferred tool*. |
| `AnalysisResults` | Renders the analysis result: overall match score, per-skill breakdown table and expandable upskilling recommendation cards. |

### Polling Strategy

After submission the frontend receives a `jobId` and begins polling `GET /api/resume/{jobId}` every 2 seconds until the job status becomes `Completed` or `Failed` (max 60 polls / 2 minutes).

---

## Backend – C# .NET Web API

| Concern | Technology |
|---|---|
| Runtime | .NET 8 |
| Web framework | ASP.NET Core minimal-controllers |
| Testing | xUnit |
| DI container | `Microsoft.Extensions.DependencyInjection` (built-in) |
| Queue | In-memory (`ConcurrentQueue<T>` + `SemaphoreSlim`) |

### Service Registrations

```csharp
// Singleton queue shared between controllers and the background worker
services.AddSingleton<IAnalysisJobQueue, InMemoryAnalysisJobQueue>();

// Stateless analysis engine
services.AddSingleton<IResumeAnalysisService, ResumeAnalysisService>();

// Long-running event-driven background worker
services.AddHostedService<ResumeAnalysisWorker>();
```

---

## Event-Based Processing Model

The backend uses an **event-driven, non-blocking producer/consumer** pattern:

1. **Producer** – `ResumeController.Submit` enqueues an `AnalysisJob` and immediately returns `202 Accepted` with the job ID.  The controller never blocks.

2. **Queue** – `InMemoryAnalysisJobQueue` stores jobs in a `ConcurrentQueue<Guid>` (IDs only) and a `ConcurrentDictionary<Guid, AnalysisJob>` (full state).  Each `Enqueue` call releases a `SemaphoreSlim` semaphore.

3. **Consumer** – `ResumeAnalysisWorker` (an `IHostedService`) awaits the semaphore:

   ```csharp
   await _queue.WorkAvailable.WaitAsync(stoppingToken);
   ```

   When the semaphore is released the worker dequeues the next job and offloads the CPU-bound analysis to a thread-pool thread via `Task.Run(...)`.  This keeps the event loop responsive and allows multiple analyses to proceed concurrently if the semaphore is released several times.

4. **Poll** – The frontend polls `GET /api/resume/{id}`.  `GetById` is O(1) (dictionary lookup).

```
 Controller           Queue                   Worker
     │  Enqueue(job)   │                         │
     ├────────────────►│  Release semaphore       │
     │  202 Accepted   │─────────────────────────►│
     │◄────────────────│  WaitAsync completes     │
                       │  TryDequeue              │
                       │◄─────────────────────────│
                       │                          │  Task.Run(Analyse)
                       │                          │─────────────────►
```

This design is intentionally simple (in-process, in-memory) so it has **zero external dependencies**.  It can be swapped for Azure Service Bus, RabbitMQ, or any other broker by replacing `InMemoryAnalysisJobQueue` with a new `IAnalysisJobQueue` implementation without touching the rest of the codebase.

---

## Skill vs. Tool Distinction

The core insight of Transposition is that most job requirements conflate *skills* (what you do) with *tools* (what you use to do it).

| Term | Meaning | Example |
|---|---|---|
| **Skill** | A transferable capability — tool-agnostic | `REST API design` |
| **Tool** | The technology used to exercise that skill | `C# .NET`, `Node.js`, `Express` |

### Analysis Logic

For every `SkillRequirement` in the target role the engine:

1. Looks up the requirement's `Skill` in a map built from the applicant's resume experience entries.
2. If the skill **is not present** → marks it as *missing* and generates a *MissingSkill* recommendation.
3. If the skill **is present** but the applicant's tools do **not include** the employer's `PreferredTool` → marks it as a *tool mismatch* and generates a *ToolMismatch* recommendation.
4. If the skill is present **and** the preferred tool matches → marks it as *aligned* (no recommendation needed).

The **overall match percentage** is calculated on mandatory skills only:

```
overallMatch = (mandatory skills present / total mandatory skills) × 100
```

---

## Upskilling Recommendations

Every gap or mismatch produces an `UpskillRecommendation` with:

| Field | Description |
|---|---|
| `Topic` | The skill name (if missing) or the preferred tool name (if a mismatch) |
| `Reason` | `MissingSkill` or `ToolMismatch` |
| `Description` | Human-readable explanation of the gap and what the applicant should focus on |
| `SuggestedResources` | Curated list of free/affordable learning resources (docs, courses, interactive platforms) |
| `EstimatedEffort` | `Low` / `Medium` / `High` — heuristic based on the skill/tool name |

For **tool mismatches** the description explicitly acknowledges the transferable skill the applicant *already has* and frames the recommendation as "you already know how to do this — just learn the syntax and ecosystem":

> *"You have 'REST API design' experience using Node.js, which is great! However, this role prefers C# .NET. Your existing skill transfers — focus on learning the C# .NET syntax and ecosystem specifics."*

---

## Data Flow

```
User fills Resume form + Job Role form
            │
            ▼
POST /api/resume  { resume, jobRole }
            │
            ▼
  202 Accepted  { jobId }
            │
            ▼
  Poll GET /api/resume/{jobId}  every 2 s
            │                       │
            │  status = Queued       │ status = Processing
            │◄──────────────────────┤
            │                       │
            │  status = Completed    │
            ├───────────────────────┘
            │
            ▼
  Render AnalysisResults component
  • Overall match % (ring chart)
  • Skill breakdown table
  • Upskilling recommendation cards
            │
            ▼
  User reviews recommendations
  and follows learning resources
```

---

## Project Structure

```
transposition/
├── Architecture.md                   ← This file
├── README.md
├── .gitignore
│
├── backend/
│   ├── Transposition.sln
│   │
│   ├── Transposition.Api/
│   │   ├── Controllers/
│   │   │   └── ResumeController.cs   ← POST & GET /api/resume
│   │   ├── Models/
│   │   │   ├── Resume.cs             ← Resume + ExperienceEntry
│   │   │   ├── JobRole.cs            ← JobRole + SkillRequirement
│   │   │   ├── AnalysisJob.cs        ← Job lifecycle tracking
│   │   │   ├── SkillAnalysisResult.cs← Result + SkillMatch + UpskillRecommendation
│   │   │   └── Dtos.cs               ← Request / response DTOs
│   │   ├── Services/
│   │   │   ├── IResumeAnalysisService.cs
│   │   │   ├── ResumeAnalysisService.cs ← Core skill-matching engine
│   │   │   ├── IAnalysisJobQueue.cs
│   │   │   └── InMemoryAnalysisJobQueue.cs
│   │   ├── Workers/
│   │   │   └── ResumeAnalysisWorker.cs  ← BackgroundService
│   │   └── Program.cs
│   │
│   └── Transposition.Tests/
│       ├── ResumeAnalysisServiceTests.cs
│       └── InMemoryAnalysisJobQueueTests.cs
│
└── frontend/
    └── transposition-ui/             ← Vite + React + TypeScript
        ├── src/
        │   ├── types/index.ts        ← Domain types (mirrors C# models)
        │   ├── services/api.ts       ← fetch wrappers
        │   ├── hooks/usePollingStatus.ts
        │   ├── components/
        │   │   ├── ResumeForm.tsx
        │   │   ├── JobRoleForm.tsx
        │   │   └── AnalysisResults.tsx
        │   ├── App.tsx
        │   └── App.css
        └── .env.example
```

---

## Running Locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (or later)
- [Node.js 18+](https://nodejs.org/)

### Backend

```bash
cd backend/Transposition.Api
dotnet run
# API available at http://localhost:5000
```

### Run tests

```bash
cd backend
dotnet test
```

### Frontend

```bash
cd frontend/transposition-ui
cp .env.example .env.local
npm install
npm run dev
# UI available at http://localhost:5173
```

---

## Future Considerations

| Enhancement | Notes |
|---|---|
| **Persistent storage** | Replace `InMemoryAnalysisJobQueue` with a database-backed store so jobs survive restarts. |
| **External message broker** | Swap the in-process queue for Azure Service Bus / RabbitMQ for horizontal scaling. |
| **LLM-powered parsing** | Integrate an LLM (e.g. Azure OpenAI) to automatically extract skills and tools from free-text resume prose. |
| **Authentication** | Add user accounts so applicants can track multiple submissions over time. |
| **PDF upload** | Accept a PDF resume and parse it server-side. |
| **Skill taxonomy** | Maintain a curated skill ontology so synonyms (e.g. "API design" vs "REST API design") are normalised automatically. |
| **Swagger / OpenAPI** | Add `Swashbuckle` for interactive API documentation. |
