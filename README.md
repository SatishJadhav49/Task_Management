# Task Management System

Internal task/activity tracking web app (Mahindra plant environment). Employees log daily activities and in/out status, manage tasks, and generate daily/weekly/monthly reports. Managers and team leads can view their team's tasks and add remarks.

This file is the codebase orientation document — read it first before making changes.

## Repository Layout

```
Task_Management/
├── API/                      ASP.NET Core 8 Web API (C#)
├── UI/                       Angular 18 standalone-components app (Tailwind CSS)
└── PQ_Task_Management.sql    Full SSMS dump (schema + data) of the database
```

The database is SQL Server (`PQ_Task_Management`); a full dump with sample data is at the repo root and is restored on the local dev machine at `localhost\SQLEXPRESS`. All data access is raw ADO.NET (`Microsoft.Data.SqlClient`) with inline parameterized SQL. **No Entity Framework, no migrations; data services don't use stored procedures** (the dump contains a couple of legacy SPs/views the API never calls).

---

## Backend (`API/`)

- **Framework:** .NET 8, controllers + Swagger (dev only), CORS `AllowAll`.
- **Entry point:** [Program.cs](API/Program.cs) — registers data services (scoped), `IApiResponseHelper` (singleton), `IDbConnectionFactory`, SMTP settings, and `ExceptionHandlingMiddleware`.
- **Runs on:** `http://localhost:5000` (see [launchSettings.json](API/Properties/launchSettings.json)). Start with `dotnet run` inside `API/`.
- **Config:** [appsettings.json](API/appsettings.json) — connection string, SMTP, and a `Modes` map (`manager: 1, lead: 2, developer: 3`). [connectionstrings.txt](API/connectionstrings.txt) holds per-plant (Nashik/Chakan) connection strings for deployment.
- **JSON naming:** `PropertyNamingPolicy = null` — API responses keep C# property names verbatim (`Task_ID`, `Employee_Name`, PascalCase with underscores). The UI models match this convention. **Keep this naming style for any new DTOs.**

### Layering pattern (follow this for new features)

```
Controller (API/Controllers/) → DataService (API/Data/) → SQL Server
```

1. **Controllers** inherit [BaseController](API/Controllers/BaseController.cs), which provides `HandleExceptionAsync(ex, operation)` — logs the exception to the `MM_Error_Log` table via `MM_ErrorLogDataService` and returns a 500 wrapped in the standard response. Every action is a try/catch calling this on failure.
2. **Data services** (e.g. [MM_TaskDataService.cs](API/Data/MM_TaskDataService.cs)) get connections from `IDbConnectionFactory` ([ApplicationDbContext.cs](API/Data/ApplicationDbContext.cs)) and execute inline SQL with `SqlCommand` + `AddWithValue` parameters, manually mapping readers to DTOs.
3. **Responses** are always wrapped by [ApiResponseHelper](API/Utils/ApiResponseHelper.cs): success → `{ datalist, res: { messageTitle, messageDetail, isErrorMessage: false } }`; error → `res.isErrorMessage = true`. The UI's `ApiRequestService` unwraps `datalist` automatically.

### Controllers & key endpoints

All routes are `api/{ControllerName}/{Action}`.

| Controller | Purpose |
|---|---|
| [MM_EmployeeController.cs](API/Controllers/MM_EmployeeController.cs) (route `MM_EmployeeMaster`) | Auth (`GetTokenCurrent`, `SearchToken/{id}`, `getCurrentHostName`), employee CRUD, `GetAllDesignations`, `GetEmployeesByMode` (self / lead's team / manager's hierarchy) |
| [MM_Task_DetailsController.cs](API/Controllers/MM_Task_DetailsController.cs) | Task CRUD (`CreateTaskDetail`, `GetTasksByEmployee?employeeId=&mode=`, `UpdateTaskDetail/{id}`, `DeleteTaskDetail/{id}`), `UpdateManagerRemark/{taskId}` (server-side manager check, 403 otherwise) |
| [MM_Activity_DetailsController.cs](API/Controllers/MM_Activity_DetailsController.cs) | Daily activity CRUD, `GetAllActivityTypes`, `GetActivitiesByDate?filterFrom=&filterTo=&employeeId=` |
| [MM_Daily_StatusController.cs](API/Controllers/MM_Daily_StatusController.cs) | Per-day in/out time + working status (`GetDailyStatusByDate`, `CreateDailyStatus`, `UpdateDailyStatus/{id}`) |
| [MM_ShiftController.cs](API/Controllers/MM_ShiftController.cs) | Shift scheduling — `GetAllShifts`, `GetWeeklySchedule?teamId=&weekStart=`, `SaveWeeklyAssignment`, `GetDayOverrides?employeeId=&weekStart=`, `SaveDayOverride` |

### Database tables (inferred from SQL in data services)

- `MM_Employee` — Employee_ID, Employee_No, Employee_Name, Email_Address, Designation_ID, Reporting_Manager_ID, Team_Lead_ID, Plant_ID, Audit_Type_Id, Shop_ID, Is_Deleted (soft delete)
- `MM_Task_Details` — Task_ID, Task_Description, Employee_ID, Due_Date, Priority (High/Medium/Low), Status (To Do/In Progress/Completed), Manager_Remark, Remark_Updated_By/Date, plus audit columns
- `MM_Activity_Details` — daily activity entries (Activity_ID, Activity_Type_ID, Description, Date, Add_In_Mail flag)
- `MM_Activity_Type_Master` — lookup for activity types (Meeting, New Requirement, Issues, Change Requirement, Other…)
- `MM_Daily_Status` — Status_ID, Date, In_Time, Out_Time, Status/Remark (working/leave/holiday/weeklyoff/comp-off)
- `MM_Designation` — 1 = Manager, 2 = Lead, 3 = Developer (these IDs are hardcoded logic — see Roles below)
- `MM_Team_List` — team master (Team_ID, Team_Name, SORTORDER); teams are added manually in SQL
- `MM_Employee_Team` — employee↔team membership (created by [SQL/Add_Team_Support.sql](SQL/Add_Team_Support.sql)); a developer/lead has one row, a manager can have many
- `MM_Shift_Master` — the fixed shifts: 1 General, 2 First, 3 Second, 4 Third (created/seeded by [SQL/Add_Shift_Management.sql](SQL/Add_Shift_Management.sql))
- `MM_Shift_Schedule` — one row per employee per week (Monday start) = their weekly shift; absence of a row means General
- `MM_Shift_Day_Override` — mid-week per-day overrides; a row here beats the weekly shift for that single date
- `MM_Error_Log` — API exception log (written by BaseController)

Audit-column convention on every table: `Inserted_Host`, `Inserted_User_ID`, `Inserted_Date`, `Updated_Host`, `Updated_User_ID`, `Updated_Date`, `Plant_Code`.

Note: `API/Models/` also contains `MM_Menus`, `MM_Menu_Role`, `MM_Roles`, `MM_User_Role`, `MM_User_Shop_Model` — carried over from a sibling project (menu/role-based permissions); currently **not wired into any controller** here.

---

## Frontend (`UI/`)

- **Stack:** Angular 18, standalone components (no NgModules), Tailwind CSS, `xlsx-js-style` for Excel export. Karma/Jasmine for tests.
- **Run:** `npm install` then `npm start` (ng serve, default `http://localhost:4200`).
- **API base URL:** built in [app.config.service.ts](UI/src/app/config/app.config.service.ts) as `{protocol}//{current-hostname}:5000/api/` — the API port **5000 is hardcoded**; the host follows wherever the UI is served from.

### Routes ([app.routes.ts](UI/src/app/app.routes.ts))

| Route | Component | Purpose |
|---|---|---|
| `/` , `/auth` | AuthLoadingComponent | Runs the auth handshake, then redirects |
| `/no-access` | NoAccessComponent | Shown on auth/API failure (401/403/network) |
| `/dashboard` | [dashboard](UI/src/app/pages/dashboard/dashboard.component.ts) | Task stat cards (total/in-progress/completed/overdue) + recent tasks |
| `/my-tasks` | [my-tasks](UI/src/app/pages/my-tasks/my-tasks.component.ts) | Task list with status/priority filters, create/edit form, manager remark modal |
| `/daily-update` | [daily-update](UI/src/app/pages/daily-update/daily-update.component.ts) | Daily activity entries + daily status panel (in/out time, working/leave) |
| `/reports` (lazy) | [report](UI/src/app/pages/report/report.component.ts) | Daily / weekly / monthly views, Excel export |
| `/user-management` (lazy) | [user-management](UI/src/app/pages/user-management/user-management.component.ts) | Employee CRUD (designation, reporting manager, team lead, team membership) |
| `/shift-management` (lazy) | [shift-management](UI/src/app/pages/shift-management/shift-management.component.ts) | Weekly drag-drop shift board per team, with per-day mid-week overrides (managers + leads edit; developers read-only) |

[app.component.ts](UI/src/app/app.component.ts) hides the sidebar/topbar layout on auth routes.

### Authentication flow (intranet SSO, no login page)

Implemented in [auth.service.ts](UI/src/app/auth/auth.service.ts):

1. `GET MM_EmployeeMaster/GetTokenCurrent` — API reads the Windows identity (or server `Environment.UserName` as fallback), encrypts the username with [EncryptDecrypt](API/Utils/EncryptDecrypt.cs), returns it as the "token".
2. `GET MM_EmployeeMaster/SearchToken/{token}` — API decrypts and looks up the employee in `MM_Employee`.
3. User data goes to **localStorage** (keys: `user` (token), `Employee_ID`, `Designation_ID`, `Name`, `Employee_No`, `Manager_ID`, `Plant_ID`, `Plant_Code`, `Shop_ID`, `Hostname`, `Email` = `{Employee_No}@mahindra.com`).

There is no JWT validation on the API side — `UseAuthorization()` exists but no endpoint requires auth. Components read identity directly from localStorage (`localStorage.getItem('Employee_ID')` etc.).

### Roles / view modes

- `Designation_ID` drives roles: **1 = Manager, 2 = Lead, 3 = Developer** (hardcoded in [common.service.ts](UI/src/app/pages/common.service.ts) and mirrored in appsettings `Modes`).
- [CommonService](UI/src/app/pages/common.service.ts) is the single UI data-access + role-state service. It holds `Is_Developer_Mode` / `Is_Lead_Mode` / `Is_Manager_Mode` flags and a `taskViewMode$` observable ('developer' | 'lead' | 'manager'). Managers/leads toggle between "self" and "team" view (topbar toggle); the mode is sent to the API as `?mode=`.
- Server side: `manager` mode is **team-based** — any manager who is a member of a team sees every member of that team (via `MM_Employee_Team`), regardless of reporting hierarchy. An optional `teamId` query param narrows to one team (`0`/omitted = all of the manager's teams; self is always included). `lead` mode returns self + direct `Team_Lead_ID` reports; otherwise self only.

### Teams

- Team master lives in `MM_Team_List`; membership in `MM_Employee_Team`. Assigned from the User Management form: managers get a multi-select (can manage multiple teams), leads/developers a single-team dropdown.
- The login handshake (`SearchToken`) returns the user's `Teams`, stored in localStorage under `Teams`.
- The topbar shows the current team next to the Manager Mode toggle: a dropdown ("All Teams" + each of the manager's teams) when the manager belongs to multiple teams, a static badge when only one. `CommonService.selectedTeamId` (0 = all) is sent as `teamId` on `GetTasksByEmployee` / `GetEmployeesByMode`; pages subscribe to `taskViewContext$` (mode + team) so a team switch reloads dashboard/my-tasks/reports.

### Shift Management

- Nav item and route (`/shift-management`) are available to Designation 1 (manager), 2 (lead), and 3 (developer). Managers and leads can edit the schedule; **developers get a read-only view** of their team (`isReadOnly` in the component: drag disabled, move dropdown hidden, day-editor selects disabled, and all save paths guarded so nothing can be changed). The page still redirects any unknown designation to `/no-access`.
- The page has its own team selector (from the user's `Teams` in localStorage) and a week navigator (Monday–Sunday; defaults to the current week's Monday). All dates are the week's **Monday**, normalized server-side.
- Board = one vertical column per shift (General / 1st / 2nd / 3rd). The roster is the team's **leads and developers** (managers are excluded — they schedule). Everyone starts in General. Dragging a card to another column auto-saves that person's shift for the whole selected week via `SaveWeeklyAssignment`. A per-card dropdown is the touch/accessibility fallback.
- Mid-week changes: the calendar icon on a card opens a day editor (Mon–Sun) where each day can be set to a different shift (`SaveDayOverride`). An override that matches the weekly shift is deleted server-side, so the "Nd changed" badge / `Override_Count` stays accurate. Effective shift for a date = day override if present, else the weekly shift, else General.

### UI conventions

- Pages call **CommonService** (not ApiRequestService directly) for domain operations; CommonService delegates to [ApiRequestService](UI/src/app/shared/services/api-request.service.ts), which prefixes the base URL, unwraps the `{res, datalist}` envelope, shows toast errors ([toast.service.ts](UI/src/app/shared/services/toast.service.ts)), and redirects to `/no-access` on 401/403/network errors.
- Model interfaces live next to their page (`task.model.ts`, `activity.model.ts`) and use the API's underscore naming (`Task_ID`, `Employee_Name`).
- Styling is Tailwind utility classes in the HTML templates; component `.css` files are mostly empty.
- Dates are handled as `yyyy-MM-dd` strings end-to-end.

---

## How to run locally

```
# API (from API/)
dotnet run            # http://localhost:5000, Swagger at /swagger

# UI (from UI/)
npm install
npm start             # http://localhost:4200, expects API on port 5000 of same host
```

The API points at `localhost\SQLEXPRESS` / `PQ_Task_Management` with Windows auth (`ConnectionStrings:DefaultConnection` in appsettings.json). If the DB is missing, restore it by running `PQ_Task_Management.sql` (note: its `CREATE DATABASE` uses hardcoded `D:\SQL_DATA` / `E:\SQL_LOG` paths — pre-create an empty `PQ_Task_Management` first, or edit those paths). Per-plant production connection strings are in [connectionstrings.txt](API/connectionstrings.txt).

## Gotchas / things to know before changing code

- **No auth enforcement on the API** — endpoints are open; the only server-side permission check is the manager check inside `UpdateManagerRemarkAsync`.
- **Port 5000 hardcoded** in the UI's `AppConfig`; changing the API port breaks the UI.
- **JSON casing matters** — the API preserves C# property names; new DTO properties must match what UI models expect (`Underscore_Pascal` style).
- **Response envelope** — always return through `_responseHelper.CreateSuccessResponse/CreateErrorResponse`; the UI unwrapping depends on it.
- Employee/user identity in the UI comes from **localStorage**, populated once during the auth handshake.
- `MM_Menus`/roles models exist but are unused; don't assume menu-permission infrastructure works.
- `UI/README.md` is just the default Angular CLI readme; this file is the real documentation.
