# Updates and Improvements Log

**Module:** Advanced Application Development (AAPD7112w)
**Student:** Lesego Letsapa (ST10351970)
**Repository:** https://github.com/cptlsg/AAPD7112

This file lists the changes made to the application after Parts 1 and 2 were
submitted, and why each one was made.

## Lecturer feedback received

Parts 1 and 2 were marked without written feedback asking for changes, so there were no
specific corrections to make. Instead, the work from Parts 1 and 2 was reviewed against
the Part 3 requirements and against its own known limitations. The updates below come
from that review. Each one names the problem it fixes, what was changed and the files
involved, so it can be checked in the source code and the Git history.

## Summary

| # | Area | Update | Type |
|---|---|---|---|
| 1 | Main menu | Service Request Status enabled, with a live summary | Completes the menu planned in Part 1 |
| 2 | Report Issues | Reference numbers can no longer clash with tracked requests | Bug fix |
| 3 | Report Issues | Every reported issue becomes a trackable service request | Integration |
| 4 | Report Issues | Emergency reports are automatically marked Critical | Enhancement |
| 5 | Report Issues | Confirmation message tells residents where to track their report | Usability |
| 6 | Theme | Status and priority colours defined in one place | Consistency |
| 7 | Repository | Merge conflict markers removed from `.gitignore` | Bug fix |
| 8 | Documentation | README rewritten to cover the complete application | Documentation |
| 9 | Documentation | Framework deviation (.NET 8) explained again for Part 3 | Transparency |

## Details

### 1. Service Request Status enabled on the main menu

- **Before:** In Parts 1 and 2 the third menu button was greyed out, with the text
  *"Not yet available"*.
- **Change:** The button now opens the new Service Request Status page. The description
  beside it is generated when the menu appears: it shows how many requests are on record,
  how many are open and, when there is one, the most urgent request waiting for a crew
  (the top of the min-heap). Like the Local Events summary, it refreshes every time the
  menu is shown.
- **Files:** `MainMenuForm.cs`

### 2. Reference numbers can no longer clash

- **Problem found:** `IssueManager` issued reference numbers from its own counter,
  starting at `MSA-YYYY-0001` each session. Part 3 brings in requests that already have
  numbers, so a new report could have received a number already used by another request.
  Tracking by reference number would then have found the wrong request.
- **Change:** Numbers are now issued by `ServiceRequestManager.NextReferenceNumber`. It
  runs a range query on the AVL tree for the current year's numbers and adds one to the
  highest. The format `MSA-YYYY-NNNN` is unchanged, so the Part 1 confirmation message
  still looks the same.
- **Files:** `Services/IssueManager.cs`, `Services/ServiceRequestManager.cs`

### 3. Reported issues become trackable service requests

- **Before:** A reported issue was stored in a list and could not be looked at again.
- **Change:** `IssueManager.AddIssue` now also calls
  `ServiceRequestManager.RegisterIssue`. The issue is added to every Part 3 structure: the
  AVL and red-black trees, the suburb BST, the department tree and the urgent heap. It
  appears on the Service Request Status page straight away, marked as reported by the
  resident. If the location names a known suburb, e.g. *"14 Burnett Street, Hatfield"*,
  the suburb is recognised so the request appears on the crew route map. The history
  notes whether a supporting file was attached.
- **Files:** `Services/IssueManager.cs`, `Services/ServiceRequestManager.cs`

### 4. Emergency reports are automatically marked Critical

- **Change:** Each category has a default priority (for example, Water and Sewerage is
  High and Parks is Low). The priority is raised to Critical if the description mentions
  an emergency such as *burst*, *flood*, *sewage*, *exposed cable* or *no water*. This
  puts dangerous problems at the top of the urgent queue without asking residents to
  judge priority themselves.
- **Files:** `Models/ServiceDepartments.cs`

### 5. Confirmation message points to the tracking page

- **Before:** *"Please keep this reference number so you can follow up on your report."*
  There was nowhere in the application to follow up.
- **Change:** *"Keep this reference number to track your report on the Service Request
  Status page."* This links the Part 1 engagement feature to the new page.
- **Files:** `ReportIssuesForm.cs`

### 6. Status and priority colours in the shared theme

- **Change:** `Theme.GetStatusColour` and `Theme.GetRequestPriorityColour` were added to
  `Theme.cs`, alongside the existing category and notice colours. Every Part 3 control
  takes its colours from there, so the list, tree, timeline and map always agree, and
  Part 3 matches the look of Parts 1 and 2.
- **Files:** `UI/Theme.cs`

### 7. `.gitignore` merge conflict fixed

- **Problem found:** When the Part 1 push was rejected because the remote repository had
  its own auto-generated `.gitignore`, the two versions were merged. The file was
  committed with the conflict markers (`<<<<<<< HEAD`, `=======`, `>>>>>>>`) still in it,
  and with a stray byte-order mark on its second line.
- **Change:** The markers and the stray character were removed, and the rules from both
  versions were kept. Build output (`bin/`, `obj/`) and Visual Studio's `.vs/` folder are
  still ignored.
- **Files:** `.gitignore`

### 8. README covers the complete application

- **Change:** The README now covers all three parts. It adds:
  - quick links to the most-used sections;
  - step-by-step instructions for every feature of the Service Request Status page;
  - a walkthrough that exercises every data structure;
  - an in-depth section explaining each Part 3 data structure, its role and its effect on
    efficiency, with examples from the sample data;
  - updated project structure, data storage, known limitations and troubleshooting
    sections.

  The *Known limitations* entry saying that Service Request Status is disabled was
  removed.
- **Files:** `README.md`

### 9. Framework deviation explained again

- **Context:** The brief specifies .NET Framework 4.8. The project targets .NET 8.0
  (`net8.0-windows`) because only .NET 7, 8 and 9 were available in the Visual Studio
  installation used. This was raised in Parts 1 and 2.
- **Change:** The README's framework note now also says that Part 3 uses only its own
  data structure classes and standard WinForms controls, with no NuGet packages. The
  application is demonstrated on the developer's own laptop.
- **Files:** `README.md`

## Earlier refinements made before the Part 2 submission

These were already in the Git history before Part 3, and are listed for completeness:

- The module code in the README was corrected to **AAPD7112w**
  (commit *"Fix module code in README"*).
- The README's command-line build instructions were changed to name the solution file,
  because `dotnet build` failed when the folder contained both a solution and a project
  file.
