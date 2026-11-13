# Municipal Services Application

A C# Windows Forms application for South African residents to report municipal issues,
find local events and service notices, and track the progress of their service requests.

---

## 1. Requirements

| Requirement | Version |
|---|---|
| Operating system | Windows 10 or 11 |
| IDE | Visual Studio 2022 (17.8 or later) with the **.NET desktop development** workload |
| Framework | .NET 8.0 (`net8.0-windows`) |

**Framework note:** the brief specifies .NET Framework 4.8. This project targets .NET 8.0
because the Visual Studio installation used offered only .NET 7, 8 and 9. No NuGet
packages are needed.

---

## 2. How to compile

**Visual Studio**

1. Open `MunicipalServicesApp.sln`.
2. Select **Build > Rebuild Solution**.
3. Check that the Output window shows `Build succeeded` with 0 errors.

**Command line** (in the project folder):

```
dotnet build MunicipalServicesApp.sln
```

---

## 3. How to run

- **Visual Studio:** press **F5** (with debugging) or **Ctrl + F5** (without).
- **Command line:** `dotnet run --project MunicipalServicesApp.csproj`
- **Executable:** after building, run `bin\Debug\net8.0-windows\MunicipalServicesApp.exe`.

All data is kept in memory and is cleared when the application closes. Sample events,
notices and service requests are loaded when it starts, with dates relative to today.

---

## 4. How to use the software

### 4.1 Main menu

Choose one of three services: **Report Issues**, **Local Events and Announcements** or
**Service Request Status**. The text beside each button shows live information, such as
the most urgent notice and the most urgent open request. **Exit** closes the application
after asking you to confirm.

### 4.2 Report Issues

1. **Location:** where the issue is, e.g. `14 Burnett Street, Hatfield` (required).
2. **Category:** choose from the list (required).
3. **Description:** what is wrong, at least 10 characters (required).
4. **Attach Photo or Document:** optional; images, PDF, Word or text files up to 10 MB.
5. Click **Submit Report**.

The progress bar and message at the top fill up as you complete each step. After you
submit, a confirmation shows your **reference number** (e.g. `MSA-2026-0027`). Keep it:
you can use it to track the report on the Service Request Status page. **Clear** empties
the form and **Back to Main Menu** returns to the menu.

### 4.3 Local Events and Announcements

- **Service notices** (left) are listed most urgent first. **Events** (centre) are grouped
  by day.
- **Search:** enter a keyword, choose a category, and/or tick **From** and **To** to set
  dates, then click **Search** or press **Enter**.
- **Previous search** goes back one search at a time. **Clear** shows everything again.
- Click a day on the **calendar** (days with events are bold) to see that day's events.
- Click an event card to see its full details.
- **Recommended for you** (right) suggests events based on what you search for and open.
  Before you search, it shows events coming up soon.

### 4.4 Service Request Status

**Track a request:** type a reference number (e.g. `MSA-2026-0006`, or just `6` for this
year) and click **Track** or press **Enter**. The request is selected in the list and
opened on the **Details** tab.

**Browse and filter the list:**

| Control | What it does |
|---|---|
| Department tree (left) | Click a department or category to list only its requests; click a request to open it. |
| Sort | Newest first, oldest first, or by reference number. |
| Status | All, open, waiting for a crew, blocked, or resolved/closed. |
| Suburb starts with | e.g. `S` shows Silverton and Sunnyside. |
| Submitted from / to | Tick a box to use that date. |
| Clear filters | Removes all filters. |

Requests you reported in this session are marked `*`. Status colours: grey = Submitted,
blue = Acknowledged, purple = Assigned, orange = In progress, green = Resolved,
slate = Closed. A request shown as **(blocked)** in red is waiting for another request.

**Tabs on the right:**

| Tab | What you can do |
|---|---|
| **Details** | See the request's information, a timeline of its stages and its history. **Advance to next stage** moves it on (a staff demonstration; a blocked request cannot start work). |
| **Urgent queue** | See the 10 most urgent requests waiting for a crew. **Dispatch next job** assigns the most urgent one that can start. Double-click a row to open it. |
| **Related** | See which requests the selected one is waiting on, which are waiting on it, and the order to resolve them. Double-click a row to open it. |
| **Crew route** | Choose a department to see a route linking the depot to every suburb with open requests. Stops are numbered in visiting order; hover over a suburb for details. **Open larger map** shows it full size (Escape closes it). |
| **Under the hood** | See the data structures behind the page and the results of automatic self-checks. Click **Run checks again** after making changes. |

**Quick tour:**

1. Track `MSA-2026-0008`. It is blocked, and the **Related** tab shows it waiting on
   `MSA-2026-0007` and `MSA-2026-0006`.
2. Track `MSA-2026-0006` and click **Advance to next stage**. It becomes Resolved, and
   `MSA-2026-0007` is no longer blocked.
3. On the **Urgent queue** tab, click **Dispatch next job**.
4. On the **Crew route** tab, choose *Roads and Transport*.
5. Report a new issue on the Report Issues page, then track its reference number here.

**Back to Main Menu** (or **Escape**) returns to the menu.

---

## 5. Troubleshooting

| Problem | Solution |
|---|---|
| Build fails: target framework missing | Install the .NET 8.0 SDK and reopen the solution. |
| `dotnet build` asks which project to build | Name the solution: `dotnet build MunicipalServicesApp.sln`. |
| A form's designer will not open | Build the solution first (the forms load their styles from `Theme.cs`). |
| A reference number is not found | Check the number on your confirmation. Numbers from earlier runs are not saved. |
| A request will not move to In progress | It is waiting for another request; see its **Related** tab. |

---

## 6. Author

| | |
|---|---|
| Name | Lesego Letsapa |
| Student number | ST10351970 |
| Module | Advanced Application Development (AAPD7112w) |
| Assessment | Portfolio of Evidence, Part 3 (complete application) |
| Repository | https://github.com/cptlsg/AAPD7112 |
