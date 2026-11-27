# Smart-X Engineering Console — Final PoE

Smart-X is an academic IoT management prototype built with ASP.NET Core, React, Entity Framework Core, and SQL Server. The complete application integrates all three project modules:

1. **Sensor Data Ingestion and Telemetry:** registration, typed packets, batches, encrypted attachments, and anomaly investigations.
2. **Real-Time Command Stream and History:** FIFO and priority processing, undo, command lookup, timelines, and rule-based recommendations.
3. **Network Topology and Mesh Routing:** timestamp-indexed history searches, a weighted mesh graph, BFS connectivity, Prim’s minimum spanning tree, route tracing, and offline-node simulation.

An Operations Dashboard and About page provide unified navigation. Saved sensor follow-ups encourage operators to return and review outstanding work.

Part 1 demo video: https://youtu.be/b3rcDilfN6U

Part 2 demo video: https://www.youtube.com/watch?v=x0-NHEjXI5c

Part 3 POE demo video: 
Video 1: https://youtu.be/VuuiuGOzA0M
Video 2: https://youtu.be/x987nUobs6c

## Features

- Register and list environmental, power-consumption, and actuator sensors.
- Validate and normalize MAC addresses while preventing duplicate device registration.
- Ingest strongly typed `float`, `integer`, and `boolean` telemetry packets.
- Process individual packets or pre-sized jagged-array batches of up to 5,000 packets.
- Prevent duplicate telemetry with a unique sensor/sequence-number constraint.
- Display current readings and historical trends in a responsive React dashboard.
- Simulate telemetry through the user interface.
- Detect moisture, power, sudden-change, and disconnected-device anomalies.
- Present guided investigations with severity, confidence, evidence, and recommended steps.
- Validate recursive facility, zone, subzone, gateway, and sensor deployment trees.
- Upload, list, and download sensor attachments encrypted at rest.
- Seed realistic demonstration sensors and telemetry automatically in Development.
- Verify the core data structures and algorithms with xUnit tests.

### Part 2 command stream

- Queue Restart, Calibrate, ChangeSampleInterval, Activate, Deactivate, and RequestStatus commands.
- Process Critical requests before High requests, then process Normal requests in FIFO order.
- Prevent duplicate active commands using a HashSet.
- Undo the most recently completed command using a stack.
- Look up commands by ID using a dictionary.
- Review newest-first history or a chronological timeline.
- Track the number of unique devices receiving commands.
- Filter devices by name, MAC address, or deployment location.
- Record the latest 20 operator queries and track keyword frequencies.
- Generate rule-based recommendations from telemetry, sensor contact history, and repeated operator searches.
- Share the selected sensor between dashboards through React context.

Command completion and undo are simulated state changes. Command history, active queues, and operator query tracking are held in memory and reset when the API restarts. Sensor data, telemetry, and attachment metadata persist in SQL Server.

### Part 3 topology and historical queries

- Model mesh nodes and weighted, bidirectional connections with an adjacency-list graph.
- Store mesh nodes and connections in SQL Server.
- Use BFS to identify nodes reachable from a selected gateway and calculate the fewest hops.
- Use Prim’s algorithm to calculate a minimum spanning tree for the gateway’s connected component.
- Display the full network or MST using an SVG diagram with link costs and highlighted routes.
- Trace a device-to-gateway route using its MAC address.
- Simulate an offline node and recalculate connectivity and routes without modifying saved topology.
- Search a selected sensor’s timestamped telemetry through an AVL tree with inclusive date boundaries.
- Select date ranges with native calendar and time inputs; the browser converts local input to UTC.

### Navigation and follow-ups

- Open all modules from the landing page or central Operations Dashboard.
- Review sensor, investigation, queued-command, and completed-command totals.
- Read the project purpose and prototype scope on the About page.
- Schedule sensor follow-ups with a note and review date/time.
- See due, upcoming, and completed tasks; mark complete, reopen, or remove tasks.
- Open the relevant sensor directly from a follow-up.
- Save follow-ups in this browser’s localStorage across refreshes and return visits.

## Architecture

<img width="738" height="749" alt="image" src="https://github.com/user-attachments/assets/55c2ea84-3fe9-4de9-8176-8c99e54f1bf9" />


The dependency direction keeps domain types in `SmartX.Core`, persistence and service implementations in `SmartX.Infrastructure`, HTTP concerns in `SmartX.Api`, and automated tests in `SmartX.Api.Tests`.

## Repository structure

```text
SmartX/
├── SmartX.Api/                 ASP.NET Core Web API and controllers
├── SmartX.Core/                Entities, DTOs, interfaces, enums, validation,
│                                exceptions, and advanced data structures
├── SmartX.Infrastructure/      EF Core context, migrations, services, and seeding
├── SmartX.Api.Tests/           xUnit automated tests
├── frontend/
│   └── smartx-client/          React and Vite client application
├── SmartX.slnx                 Visual Studio solution
├── .gitattributes
├── .gitignore
└── README.md
```

## Technology stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core on .NET 10 |
| Client | React, Vite, React Router |
| Visualization | Recharts telemetry charts and native SVG mesh diagram |
| HTTP client | Axios |
| Icons | Lucide React |
| Persistence | SQL Server LocalDB and Entity Framework Core 10 |
| Testing | xUnit and Microsoft.NET.Test.Sdk |
| Development | Visual Studio / Visual Studio Code and the .NET CLI |

## Prerequisites

Install the following before running Smart-X:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A Visual Studio version compatible with your .NET 10 SDK, or Visual Studio Code and the .NET CLI
- SQL Server Express LocalDB (normally installed with Visual Studio)
- A current Node.js LTS release and npm
- Visual Studio Code for the React client (optional but recommended)
- Git

Confirm the command-line tools are available:

```powershell
dotnet --version
node --version
npm --version
git --version
```

## Quick start

### 1. Clone and restore the repository

```powershell
git clone https://github.com/EMKNDN/emkndn-prog7312-g2-2026-prog7312-poe-nikhil4vc.git SmartX
cd SmartX
git switch master
dotnet restore .\SmartX.slnx
```

### 2. Configure the database

Add the following connection string to `SmartX.Api/appsettings.Development.json` if it is not already present:

```json
{
  "ConnectionStrings": {
    "SmartXDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=SmartXDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Merge these settings into the existing configuration rather than replacing unrelated settings. Apply the checked-in migrations explicitly before starting the API; the migration commands are shown below. Development startup seeds demonstration data.

### 3. Configure encrypted attachment storage

The encryption key must never be committed to Git. Store a randomly generated 256-bit key with .NET User Secrets.

From the repository root, run:

```powershell
$fileKeyBytes = New-Object byte[] 32
$fileKeyGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$fileKeyGenerator.GetBytes($fileKeyBytes)
$fileKeyGenerator.Dispose()
$fileEncryptionKey = [Convert]::ToBase64String($fileKeyBytes)

dotnet user-secrets init --project .\SmartX.Api\SmartX.Api.csproj
dotnet user-secrets set "FileEncryption:Key" "$fileEncryptionKey" --project .\SmartX.Api\SmartX.Api.csproj
```

The `UserSecretsId` in `SmartX.Api.csproj` identifies the external secret store; it is not the encryption key.

Ensure the API configuration also contains:

```json
{
  "FileStorage": {
    "RootPath": "App_Data/EncryptedUploads",
    "MaximumFileSizeBytes": 10485760
  }
}
```

`App_Data` is excluded from source control. Smart-X encrypts uploaded file bytes before writing them to disk.

### 4. Apply database migrations

The current `Program.cs` seeds data in Development but **does not automatically apply migrations**. From the repository root, apply the checked-in migrations before the first API startup:

```powershell
dotnet tool install --global dotnet-ef --version "10.*"
dotnet ef database update --project .\SmartX.Infrastructure\SmartX.Infrastructure.csproj --startup-project .\SmartX.Api\SmartX.Api.csproj
```

Install the global tool only if it is missing. If it is already installed and needs updating, use `dotnet tool update --global dotnet-ef --version "10.*"`. Match the EF CLI major version to the project’s EF Core packages. Do not create a new migration merely to run the application.

### 5. Build and test the backend

```powershell
dotnet build .\SmartX.slnx
dotnet test .\SmartX.slnx
```

The verified final suite contains **75 passing backend tests**, covering the earlier modules and the AVL history tree, history index, mesh graph, BFS traversal, and minimum spanning tree. Formatting verification, frontend lint, and the production client build also passed.

### 6. Run the API

Use either Visual Studio or the command line, not both at the same time.

In Visual Studio:

1. Open `SmartX.slnx`.
2. Set `SmartX.Api` as the startup project.
3. Start the project with the HTTP profile to match the client URL below. If using HTTPS instead, update the client base URL and trust the development certificate.

Or from a terminal:

```powershell
dotnet run --project .\SmartX.Api\SmartX.Api.csproj --launch-profile http
```

The local development profile used during development is `http://localhost:5229`. Always use the URL printed by the API process if your launch profile assigns a different port.

### 7. Configure and run the React client

Open a second terminal:

```powershell
cd .\frontend\smartx-client
npm install
```

Create `frontend/smartx-client/.env` with the following value, or copy `.env.example` if present:

```env
VITE_API_BASE_URL=http://localhost:5229/api
```

Then run the client:

```powershell
npm run dev
```

Open [http://localhost:5173](http://localhost:5173). The API CORS policy permits this development origin.

Create a production client build with:

```powershell
npm run build
```

## Database migrations

From the repository root, stop any running API instance, install the EF CLI if needed, and apply the existing migrations:

```powershell
dotnet tool install --global dotnet-ef --version "10.*"
dotnet ef --version
dotnet ef database update --project .\SmartX.Infrastructure\SmartX.Infrastructure.csproj --startup-project .\SmartX.Api\SmartX.Api.csproj
```

If `dotnet-ef` is already installed, use `dotnet tool update --global dotnet-ef --version "10.*"` when necessary. Use the same EF major version as the project's EF packages. No tool manifest is required for this global-tool workflow.

Create a new migration only when changing the database model:

```powershell
dotnet ef migrations add MigrationName --project .\SmartX.Infrastructure\SmartX.Infrastructure.csproj --startup-project .\SmartX.Api\SmartX.Api.csproj
```

The final application uses `Sensors`, `TelemetryReadings`, `SensorAttachments`, `MeshNodes`, `MeshConnections`, and `__EFMigrationsHistory`. Apply all checked-in migrations, including `AddMeshTopology`. Command queues and query tracking remain in memory and do not add database tables.

## API reference

The API includes a development OpenAPI document and an HTTP request file for manual testing.

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/sensors` | List registered sensors |
| `GET` | `/api/sensors/{id}` | Get one sensor |
| `POST` | `/api/sensors` | Register a sensor |
| `POST` | `/api/telemetry/float` | Ingest a floating-point packet |
| `POST` | `/api/telemetry/integer` | Ingest an integer packet |
| `POST` | `/api/telemetry/boolean` | Ingest a Boolean packet |
| `POST` | `/api/telemetry/float/batch` | Ingest a jagged float batch |
| `POST` | `/api/telemetry/integer/batch` | Ingest a jagged integer batch |
| `POST` | `/api/telemetry/boolean/batch` | Ingest a jagged Boolean batch |
| `GET` | `/api/telemetry/sensor/{sensorId}?limit=100` | Get sensor history |
| `POST` | `/api/deployments/validate` | Recursively validate a deployment tree |
| `GET` | `/api/investigations` | Get guided anomaly investigations |
| `POST` | `/api/sensors/{sensorId}/attachments` | Upload and encrypt a file |
| `GET` | `/api/sensors/{sensorId}/attachments` | List a sensor's files |
| `GET` | `/api/sensors/{sensorId}/attachments/{attachmentId}/download` | Decrypt and download a file |

### Part 2 endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/commands` | Queue a device command |
| `GET` | `/api/commands` | Get newest-first command history |
| `GET` | `/api/commands/{commandId}` | Look up a command by ID |
| `GET` | `/api/commands/summary` | Get queue and unique-device totals |
| `GET` | `/api/commands/timeline` | Get chronological command history |
| `POST` | `/api/commands/process-next` | Process the next available command |
| `POST` | `/api/commands/undo` | Undo the last completed command |
| `GET` | `/api/recommendations` | Get predictive action recommendations |
| `POST` | `/api/recommendations/analyse` | Record an operator query and analyse patterns |

### Part 3 endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/telemetry/sensor/{sensorId}/range?fromUtc={start}&toUtc={end}` | Inclusive timestamp-range history query |
| `GET` | `/api/topology` | List persisted mesh nodes and connections |
| `GET` | `/api/topology/connectivity/{gatewayId}` | BFS reachability and hop counts |
| `GET` | `/api/topology/mst/{gatewayId}` | Prim’s MST and combined energy cost |
| `GET` | `/api/topology/route/{gatewayId}?macAddress={mac}&useSpanningTree=false` | Trace a fewest-hop route to the gateway |

For a route through the MST, set `useSpanningTree=true`. Connectivity, MST, and route endpoints also accept optional `offlineNodeId={meshNodeId}` for request-only failure simulation. Obtain IDs from `/api/topology`; mesh-node IDs differ from sensor IDs. `gatewayId` must identify a gateway node.

Range queries require both timestamps. Use ISO 8601 UTC values, for example `2026-08-30T13:00:00Z`; reversed ranges return a validation error. An empty valid range returns an empty list.

### Frontend pages

| Route | Page |
|---|---|
| `/` | Landing page with all three modules |
| `/about` | Project purpose, implementation, and prototype scope |
| `/dashboard` | Operations overview and saved follow-ups |
| `/telemetry` | Sensor registry, telemetry, attachments, and investigations |
| `/commands` | Command queues, history, timeline, and recommendations |
| `/topology` | Mesh visualisation, route tracing, simulation, and historical search |

### Register a sensor

```http
POST /api/sensors
Content-Type: application/json

{
  "name": "Demo Moisture Sensor B",
  "macAddress": "D2:7A:91:3C:5E:C9",
  "deploymentLocation": "Hydroponics Facility / Zone 1 / Bed A",
  "category": 1,
  "dataType": 1
}
```

Use a MAC address not already present in your registry. Copy the ID returned by registration for subsequent requests.

### Ingest telemetry

```http
POST /api/telemetry/float
Content-Type: application/json

{
  "sensorId": "YOUR_SENSOR_ID",
  "value": 42.7,
  "unit": "%",
  "sequenceNumber": 1,
  "capturedAtUtc": "2026-08-30T13:04:25Z"
}
```

Submitting the same sensor ID and sequence number again returns a conflict instead of storing duplicate telemetry. Submitting a payload type that does not match the sensor returns a validation error.

### Queue a command

First retrieve `/api/sensors` and copy a returned sensor ID. The API obtains the sensor name; do not add `sensorName` to the request.

```http
POST /api/commands
Content-Type: application/json

{
  "sensorId": "YOUR_SENSOR_ID",
  "type": 6,
  "priority": 1,
  "payload": "Return current device status"
}
```

Command types: `1` Restart, `2` Calibrate, `3` ChangeSampleInterval, `4` Activate, `5` Deactivate, `6` RequestStatus. Priorities: `1` Normal, `2` High, `3` Critical. Enum values are returned as strings in responses.

```http
POST /api/recommendations/analyse
Content-Type: application/json

{
  "query": "Investigate power spike"
}
```

## Part 2 walkthrough

1. Start the API and frontend, then open the home page.
2. Open **Real-Time Command Stream** (`/commands`).
3. Select a sensor and queue a Normal RequestStatus command.
4. Queue a Critical Restart command for the same sensor.
5. Click **Process next**: the Critical command should complete first.
6. Process the Normal command, then click **Undo last**: the Normal command should become Undone.
7. Compare **Latest first** and **Timeline** history views.
8. Use device filtering and queue a command for another sensor to demonstrate unique-device tracking.
9. Copy a returned command ID and request `/api/commands/{commandId}` to demonstrate lookup.
10. Analyse two power-related queries and review the resulting recommendations.

## Part 3 walkthrough

1. Open **Network Topology** from Home or the Operations Dashboard.
2. Select **Smart-X Central Gateway** and **No simulated failure**.
3. Click **Analyse network**. Inspect the reachable/unreachable results and hop counts in the node directory.
4. Select **Minimum spanning tree** after analysis to display its connections and combined cost.
5. Choose **Full network**, select a device MAC from the directory, and click **Trace route**. The displayed path runs from device to gateway.
6. Switch to the MST view and trace again to compare the route through the selected tree.
7. Choose an intermediate node under **Simulate offline node**, analyse again, and retrace the device. A valid alternative may be found; if none exists, the device is reported unreachable.
8. Restore **No simulated failure** and analyse again. Simulation does not change database availability flags.
9. Scroll to **Search readings by date range**, select a sensor, choose local dates/times, and search. Results are chronological and include both timestamp boundaries.
10. Open Overview, add a sensor follow-up, refresh to confirm persistence, review its sensor, and mark the task complete.

Node IDs, node counts, link counts, and costs depend on the database. On the previously tested dataset with 11 sensor nodes plus one gateway, the healthy network had 22 enabled links and MST cost 13.25. These are example results, not fixed requirements for a fresh database.

## Advanced data structures and algorithms

Smart-X demonstrates data-structure requirements across Parts 1–3 through working application features.

| Requirement | Smart-X implementation |
|---|---|
| Generics | `TelemetryPacket<T>` preserves strongly typed float, integer, and Boolean payloads without `object` boxing. |
| Jagged arrays and pre-sized lists | `T[][]` telemetry batches are flattened into a `List<TelemetryPacket<T>>` with calculated initial capacity. |
| Operator overloading | `PowerMetric` overloads arithmetic and comparison operators used by power anomaly analysis. |
| Recursion | `DeploymentTreeValidator` recursively validates the deployment hierarchy and records maximum depth and traversal errors. |
| Algorithmic safeguards | Batch size limits, sequence idempotency, maximum tree depth, and duplicate-child validation bound processing and protect data integrity. |

### Part 2 structures

| Structure | Purpose | Main file |
|---|---|---|
| `Queue<DeviceCommand>` | FIFO processing of Normal commands | `SmartX.Infrastructure/Commands/CommandStreamService.cs` |
| `PriorityQueue<DeviceCommand, int>` | Critical before High processing using negative priority values | `SmartX.Infrastructure/Commands/CommandStreamService.cs` |
| `Stack<DeviceCommand>` | Last-completed-first undo | `SmartX.Infrastructure/Commands/CommandStreamService.cs` |
| `Dictionary<Guid, DeviceCommand>` | Command lookup by ID | `SmartX.Infrastructure/Commands/CommandStreamService.cs` |
| `SortedDictionary<DateTime, List<Guid>>` | Chronological creation timeline, including equal timestamps | `SmartX.Infrastructure/Commands/CommandStreamService.cs` |
| `HashSet<string>` | Duplicate active-command prevention | `SmartX.Infrastructure/Commands/CommandStreamService.cs` |
| `HashSet<Guid>` | Unique commanded-device tracking | `SmartX.Infrastructure/Commands/CommandStreamService.cs` |
| `Queue<string>` | Latest 20 recorded queries | `SmartX.Infrastructure/Commands/OperatorQueryTracker.cs` |
| `Dictionary<string, int>` | Keyword counts across recorded queries during the API session | `SmartX.Infrastructure/Commands/OperatorQueryTracker.cs` |
| `HashSet<string>` | Excluding common words from keyword extraction | `SmartX.Infrastructure/Commands/OperatorQueryTracker.cs` |

Priority requests with the same priority have no guaranteed FIFO ordering. The summary's `criticalCommands` field counts the combined High/Critical priority queue. Undo updates recorded command state; it does not requeue the command.

### Part 3 structures and algorithms

| Structure / algorithm | Role | Main file |
|---|---|---|
| AVL tree | Timestamp-keyed insertion, rotations, and range pruning | `SmartX.Core/Topology/DataStructures/TelemetryHistoryTree.cs` |
| ConcurrentDictionary and SemaphoreSlim | Per-sensor history caches and synchronised access | `SmartX.Infrastructure/Topology/TelemetryHistoryIndex.cs` |
| Adjacency-list graph | Weighted undirected links, ID lookup, MAC lookup, and node availability | `SmartX.Core/Topology/DataStructures/MeshGraph.cs` |
| BFS using a queue | Reachability, fewest-hop counts, and predecessor paths | `SmartX.Core/Topology/Algorithms/MeshTraversal.cs` |
| Prim’s algorithm using a priority queue | Lowest combined connection cost for the gateway component | `SmartX.Core/Topology/Algorithms/MeshMinimumSpanningTree.cs` |
| SQL-backed graph integration | Builds request snapshots and selects route mode | `SmartX.Infrastructure/Topology/TopologyService.cs` |
| SVG elements | Renders nodes, weighted edges, and highlighted paths | `frontend/smartx-client/src/pages/TopologyDashboard.jsx` |

**AVL history:** readings sharing a timestamp are grouped at that key. Insertions rebalance the tree; inclusive range queries traverse only relevant branches and return chronological results. With `u` distinct timestamps and `k` returned readings, the balanced-tree range operation targets O(log u + k). The first query for a sensor loads its stored history and builds the index; later queries reuse it. Successful ingestion appends readings to an already loaded index while preventing duplicate reading IDs.

**Graph traversal:** BFS ignores energy weights and finds the fewest hops through online nodes and enabled links. The implementation orders available neighbours for reproducible traversal, so sorting adds overhead beyond the usual O(V + E) BFS traversal model.

**MST:** Prim’s algorithm uses a min-priority queue of candidate connections and a visited set to avoid cycles. It minimises the sum of selected link costs, rather than the energy cost of every individual device route. A route through the MST can therefore have more hops or greater individual cost than another route in the full graph. For disconnected networks, the result covers the gateway component and reports the remaining nodes unreachable.

**Persistence and cache scope:** SQL Server is the source of stored telemetry and topology. The AVL cache lives in one API process, is populated lazily, and is rebuilt after restart. It does not automatically detect external database edits or coordinate multiple API instances.

The supported deployment hierarchy is:

```text
Facility → Zone → SubZone → Gateway → Sensor
```

Sensor nodes require a valid MAC address and cannot contain child nodes. Validation is limited to a maximum depth of eight.

## Guided anomaly investigation

The dashboard's dynamic engagement feature is a guided anomaly investigation engine. It analyzes current and recent telemetry and can identify:

- missing initial telemetry;
- devices that have stopped reporting;
- warning and critical moisture levels;
- power-consumption spikes;
- sudden changes between consecutive readings.

Each investigation provides a stable investigation ID, severity, explanation, confidence score, evidence, and an ordered set of operator actions. These recommendations support operator judgment; they do not automatically control hardware.

## Seed data

In Development, `SmartXDataSeeder` supplies a repeatable demonstration dataset when required. It includes six representative sensors with 48 readings each across environmental, power, and actuator scenarios. User-created sensors and telemetry remain stored in SQL Server.

`SmartXTopologySeeder` creates the central gateway and missing mesh nodes for registered sensors during Development startup. When no connections exist, it creates a weighted chain with additional links and gateway connections to demonstrate cycles and alternate routes. Existing connections are preserved on later startups. Nodes added to an already configured network are not automatically given new connections.

Mesh availability is the stored mesh-node flag; it is not a live hardware heartbeat. Seeded costs are demonstration values, not measured energy consumption. The telemetry seeder refreshes the seeded sensors’ last-contact timestamps during Development startup, so these timestamps should not be treated as evidence of physical device contact.

## File security and prototype scope

- Attachments are encrypted at rest using AES-256-CBC with a new random IV for each file.
- The IV is stored with the ciphertext so the file can be decrypted during a download request.
- The encryption key is kept in .NET User Secrets and is never added to the repository.
- Uploads are limited to 10 MB.
- Allowed extensions include `.jpg`, `.jpeg`, `.png`, `.pdf`, `.txt`, `.log`, `.json`, `.csv`, `.yaml`, and `.yml`.
- File names are treated as metadata; generated server-side names prevent unsafe path construction.

## Testing

Run all tests from the repository root:

```powershell
dotnet test .\SmartX.slnx
```

The suite currently covers:

- `TelemetryPacketTests`
- `TelemetryBatchProcessorTests`
- `PowerMetricTests`
- `DeploymentTreeValidatorTests`
- `CommandStreamServiceTests`
- `OperatorQueryTrackerTests`
- AVL history-tree range and balancing tests
- Per-sensor history-index loading and concurrency tests
- Mesh graph validation and lookup tests
- BFS connectivity, path, and offline-node tests
- Prim’s minimum spanning tree tests

Verified result:

```text
Test summary: total: 75, failed: 0, succeeded: 75, skipped: 0
```

### Formatting, lint, and production build

From the repository root:

```powershell
dotnet format .\SmartX.slnx --verify-no-changes
dotnet build .\SmartX.slnx
dotnet test .\SmartX.slnx
```

From the frontend folder:

```powershell
cd .\frontend\smartx-client
npm run lint
npm run build
```

No frontend automated-test script is configured; `npm run lint` performs static checks and `npm run build` verifies the production bundle. Browser workflow checks complement the backend tests.

Run `dotnet format .\SmartX.slnx` to fix formatting before verifying again. Vite's large-chunk warning is a bundle-size advisory; a successful build still produces `dist`.

## Troubleshooting

### The API reports that the connection string is missing

Confirm `ConnectionStrings:SmartXDatabase` exists in `SmartX.Api/appsettings.Development.json`, and ensure the API is running with the Development environment.

### LocalDB is not available

Open Visual Studio Installer and add SQL Server Express LocalDB, or install it separately. Confirm `(localdb)\\MSSQLLocalDB` appears in SQL Server Object Explorer.

### Attachment encryption configuration is missing

Set `FileEncryption:Key` using the User Secrets command in the setup section. Do not publish the key or secret-list output. Changing the key prevents decryption of files encrypted with the previous key.

### Build fails because DLL files are locked

Stop the running API with Ctrl+C, or stop Visual Studio debugging with Shift+F5. Run only one backend instance. Then rebuild or restart the API.

### npm reports a missing package.json

Run npm commands from `frontend/smartx-client`, not the repository root.

### Commands disappear after restarting the API

This is expected: the Part 2 command stream and query tracker are singleton in-memory services. SQL Server-backed sensor and telemetry records remain available.

### The React client cannot reach the API

- Confirm the API is running.
- Confirm `VITE_API_BASE_URL` matches the API port and ends in `/api`.
- Restart Vite after editing `.env`.
- Use `http://localhost:5173` so it matches the configured CORS origin.

### EF Core commands use the wrong PowerShell command

`Install-Package -ProjectName` works in Visual Studio's **Package Manager Console**, not ordinary PowerShell. From a normal terminal, use `dotnet add <project> package <package-name>`.

### xUnit types such as `Fact` or `Assert` are missing

Keep test classes in `SmartX.Api.Tests`, ensure that project references `SmartX.Core`, and confirm the xUnit packages are installed in the test project. Production classes such as `TelemetryPacket<T>` remain in `SmartX.Core`.

### The MST button is disabled

Select a gateway and click **Analyse network** first. No simulated failure is required. The MST view becomes available after a tree result is returned.

### Historical search returns no readings

Confirm the selected sensor and time window. UI fields use local time; API requests use UTC. A successful empty result means no readings fall within that range. Gateways have no sensor telemetry and are excluded from the history selector.

### New sensors are absent from the mesh directory

The Development topology seeder adds missing sensor nodes at API startup. Restart the API to seed newly registered sensors into the mesh. Existing network connections are preserved, so newly added nodes may be disconnected.

### Follow-ups do not appear on another device

Tasks are saved in localStorage for the current browser origin. A different browser, host, or frontend port has separate storage. Clearing browser site data removes these tasks.

## Navigation, engagement, and exception handling improvements

- **Navigation:** the Home page, About page, and central Operations Dashboard link all three modules.
- **Repeat engagement:** browser-saved sensor follow-ups include review dates, due counts, direct sensor links, completion, reopening, and removal.
- **Exception handling:** expected validation and missing-resource errors receive appropriate responses; a global API handler returns a generic JSON message and trace ID for unexpected request failures.
- **Asynchronous operations:** database queries and saves use asynchronous EF Core methods with cancellation tokens. Graph algorithms and in-memory collection operations run synchronously.

The previously recorded Part 2 video includes an earlier feedback-improvements segment at **11:04**.

## Prototype scope and storage behaviour

| Data / behaviour | Storage or scope |
|---|---|
| Sensors, telemetry, attachment metadata, mesh nodes, and links | SQL Server |
| Encrypted attachment bytes | Configured local storage folder |
| Commands, queues, undo stack, and operator query tracking | In-memory singleton services; reset on API restart |
| AVL history indexes | Lazy in-memory per-sensor cache; rebuilt after API restart |
| Follow-up tasks | Browser localStorage; not synchronised between browsers/devices |
| Offline simulation | One request’s graph snapshot; no database mutation |

Commands simulate processing and undo; no physical ESP32 execution is confirmed. Recommendations use programmed rules and heuristic confidence scores, not a trained predictive model. Follow-ups are dashboard reminders, not background notifications. Encryption at rest does not itself provide user authentication or download authorisation.

## Source control, submission and references

- The complete PoE implementation is merged into `master`.
- The `part-2-submission` tag retains the earlier Part 2 snapshot; do not check out that tag when running the final application.
- Meaningful incremental commits record implementation progress.
- Code attribution comments remain in the relevant source files; the submission report contains the reference list.
- Final submission items include the PDF report, complete GitHub source, this README, the lecturer-specified video/presentation.
