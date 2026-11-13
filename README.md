# Municipal Services Application

A Windows Forms application that allows South African residents to report municipal
service issues, such as water leaks, potholes and uncollected refuse, and to find local
events and service notices from their municipality.

This README covers the complete application (Part 3 of the Portfolio of Evidence).
All three services on the main menu are available:

- **Report Issues** (Part 1): report a problem and attach a supporting file.
- **Local Events and Announcements** (Part 2): browse and search upcoming events, read
  service notices in order of urgency, and get event suggestions based on your searches.
- **Service Request Status** (Part 3): track a request by its reference number, browse
  every request and its status, see which requests are blocked by others, view the most
  urgent jobs, and plan crew routes. It is built on binary search trees, AVL and
  red-black trees, a general tree, a min-heap and graphs, with graph traversal and
  minimum spanning trees.

Quick links: [How to compile](#2-how-to-compile) · [How to run](#3-how-to-run) ·
[Using Service Request Status](#47-service-request-status) ·
[Data structures for Part 3](#5-data-structures-service-request-status-part-3)

---

## 1. Requirements

| Requirement | Version |
|---|---|
| Operating system | Windows 10 or Windows 11 |
| IDE | Visual Studio 2022 (17.8 or later) |
| Framework | .NET 8.0 (`net8.0-windows`) |
| Workload | .NET desktop development |
| Language | C# |

### Framework note

The brief specifies .NET Framework 4.8. This project targets .NET 8.0 because the Visual
Studio installation used for development offered only .NET 7, 8 and 9. Part 2 also uses
`PriorityQueue<TElement, TPriority>`, which is part of .NET 6 and later and is not
available in .NET Framework 4.8. Part 3 uses only its own data structure classes and
standard WinForms controls, and needs no NuGet packages.

---

## 2. How to compile

### Using Visual Studio

1. Open `MunicipalServicesApp.sln` in Visual Studio 2022.
2. Wait for the solution to finish loading in Solution Explorer.
3. Select **Build > Build Solution**, or press **F6**.
4. Confirm that the Output window reports `Build succeeded` with 0 errors.

### Using the command line

From the project folder (it contains both the solution and the project file, so name
the one to build):

```
dotnet build MunicipalServicesApp.sln
```

---

## 3. How to run

### Using Visual Studio

Press **F5** to run with debugging, or **Ctrl + F5** to run without debugging.

### Using the command line

From the project folder:

```
dotnet run --project MunicipalServicesApp.csproj
```

### Running the compiled executable

After a successful build, the executable is located in the project folder at:

```
bin\Debug\net8.0-windows\MunicipalServicesApp.exe
```

---

## 4. How to use the software

### 4.1 Main menu

The main menu appears on startup and presents three municipal services:

- **Report Issues**: available.
- **Local Events and Announcements**: available. The description beside the button
  shows how many upcoming events there are and, when there is one, the most urgent
  active notice marked *Urgent*.
- **Service Request Status**: available. The description beside the button shows how
  many service requests are on record, how many are open and, when there is one, the most
  urgent request still waiting for a crew.

A summary at the bottom of the window shows how many issues have been reported during
the current session. Click **Exit** to close the application; a confirmation prompt is
shown before it closes.

### 4.2 Reporting an issue

Click **Report Issues** to open the reporting form, then complete the following:

1. **Location**: type where the issue is, for example `Corner of Main Road and 5th Avenue, Kempton Park`. Required, minimum 3 characters.
2. **Category**: choose from the dropdown list: Sanitation, Roads and Potholes, Water and Sewerage, Electricity, Refuse Removal, Street Lighting, Parks and Public Spaces, or Other. Required.
3. **Description**: describe the issue in the rich text box. Required, minimum 10 characters.
4. **Attachment**: click **Attach Photo or Document** to open a file dialog and select a supporting image or document. Optional, maximum file size 10 MB. Accepted formats are PNG, JPG, JPEG, BMP, GIF, PDF, DOCX, DOC and TXT.

Then click **Submit Report**.

Other controls on this form:

- **Clear**: empties all fields after a confirmation prompt.
- **Back to Main Menu**: closes the form and returns to the main menu.

### 4.3 User engagement feature

The application implements **gamification through progressive feedback** as its user
engagement strategy.

A progress bar at the top of the reporting form fills as each of the four stages is
completed, in 25% increments:

| Stage completed | Progress |
|---|---|
| Location entered | 25% |
| Category selected | 50% |
| Description entered | 75% |
| Attachment added | 100% |

Above the bar, a message updates in real time to acknowledge what has been completed and
name the next useful step, for example *"Almost there. Describe the issue so the team
knows what to expect."*

On successful submission, a confirmation message displays a unique reference number in
the format `MSA-YYYY-NNNN` (for example `MSA-2026-0027`), together with an
acknowledgement of how many reports the resident has submitted during the session.
The report immediately becomes a service request with that number, which can be tracked
on the **Service Request Status** page (section 4.7). Numbers continue from the highest
number already issued this year, so they never clash with existing requests.

### 4.4 Validation and feedback

The application will not accept an incomplete report. If a required field is missing or
too short, a message explains what is needed and the cursor is placed in the field that
requires attention. Successful actions, warnings and errors are all communicated through
message boxes.

### 4.5 Local Events and Announcements

Click **Local Events and Announcements** on the main menu. The page has a search bar
across the top and three columns:

| Area | What it shows |
|---|---|
| **Service notices** (left) | Active notices from the municipality, most urgent first. Urgent notices have a red stripe and a tinted background; important notices are amber. Notices that have ended are hidden. |
| **Events** (centre) | Upcoming events grouped under a heading for each day, in date and time order. Each card shows the category (colour coded), time, title, venue, area and a short description. |
| **Calendar and suggestions** (right) | A calendar with days that have events shown in **bold**, and a column of suggested events. |

When the page first opens, every upcoming event is listed.

**Searching**

1. **Keyword**: type one or more words, for example `clinic` or `water meter`. Titles,
   descriptions, venues, areas, categories and topics are searched. When several words are
   entered, an event must match all of them.
2. **Category**: choose a category, or leave it on *All categories*. Choosing a category
   also filters the notices on the left to that category.
3. **From** and **To**: tick the box on a date to use it. Leave a box unticked for no
   limit on that side. The From date must be on or before the To date; if it is not, a
   message explains the problem and the search is not run.
4. Click **Search** or press **Enter**.

The heading above the events describes the search and how many events matched. If nothing
matches, a message suggests how to widen the search.

**Other controls**

- **Calendar**: click a day to see everything happening on that day.
- **Previous search**: goes back one search at a time, restoring the filters as well as
  the results. Hover over it to see which search it will go back to. It is disabled when
  there is nothing to go back to.
- **Clear**: removes all filters and lists every upcoming event again.
- **Event cards**: click a card, or select it with Tab and press Enter, to see the full
  details of the event.
- **Back to Main Menu**, or **Escape**: returns to the main menu.

The status line at the bottom shows how many events are displayed out of the total, and
how many service notices are active.

### 4.6 Recommended events

The right-hand column suggests three events based on what you search for and which events
you open.

- Before you have searched or opened anything, it is headed **Coming up soon** and shows
  the next events on the calendar.
- After you search or open an event, it is headed **Recommended for you**. Each card gives
  the reason for the suggestion, for example *Matches your search for "water"* or
  *Similar topics: budget, public participation*.
- A note at the top of the column says what the suggestions are based on (for example
  *Based on your last 3 searches and 1 event you opened*) and lists your top interests.
  Hover over the **Recommended for you** heading to see your recent searches.
- Events already showing in your search results, and events you have already opened, are
  not suggested.
- Suggestions update as soon as you search or open an event, and are kept when you leave
  the page and come back during the same session.

**To see the feature working:**

1. Open the page and note the **Coming up soon** suggestions.
2. Search for `water`. The suggestions change to events related to the results, such as
   meetings that share topics with them.
3. Search for `clinic`. Health and wellness events move to the top, while events related
   to the earlier water search move down.
4. Open one of the suggested events. It is removed from the suggestions, and the list
   updates to reflect the topics of the event you opened.

### 4.7 Service Request Status

Click **Service Request Status** on the main menu. The description beside the button
shows how many requests are on record, how many are still open and, if there is one,
the most urgent request still waiting for a crew.

The page opens with 31 sample requests (see section 8) plus any issue you reported on the
**Report Issues** page during this session. Every reported issue becomes a service
request with the same reference number, marked with `*` in the list and *"reported by
you"* on the details tab.

The page has a filter bar across the top and three columns:

| Area | What it shows |
|---|---|
| **Browse by department** (left) | A tree of the municipality's departments, the categories each one handles and the requests in each category. Every branch shows how many requests below it are open, out of the total, e.g. *Water and Sanitation (5 open / 8)*. Blocked requests are red; resolved and closed ones are grey. |
| **Service requests** (centre) | The requests that match the filters, with their reference number, title, suburb, priority and status. Status colours: grey = Submitted, blue = Acknowledged, purple = Assigned, orange = In progress, green = Resolved, slate = Closed. A request waiting for another request shows *(blocked)* in red. |
| **Tabs** (right) | *Details*, *Urgent queue*, *Related*, *Crew route* and *Under the hood*, described below. |

**Tracking a request**

1. Type the reference number into **Track a request**, for example `MSA-2026-0006`.
   You can also type just the number (`6`) for this year, or leave out the dashes
   (`msa 2026 6`).
2. Click **Track** or press **Enter**.
3. If the number exists, the request is selected in the list and opened on the
   **Details** tab, and the line beside the Track button shows its current status.
   If a filter was hiding it, the filters are cleared so it can be shown.
4. If the number does not exist, a red message says so. Check the number on the
   confirmation message you received when you reported the issue.

**Filtering and sorting the list**

| Control | What it does |
|---|---|
| **Sort** | *Newest first* or *Oldest first* (by date submitted), or *Reference number*. |
| **Status** | *All statuses*, *Open*, *Waiting for a crew* (Submitted or Acknowledged), *Blocked*, or *Resolved or closed*. |
| **Suburb starts with** | Type the start of a suburb's name; for example `S` shows requests in Silverton and Sunnyside. |
| **Submitted from / to** | Tick a box to use that date. The *from* date must be on or before the *to* date. |
| **Department tree** | Click a department or category to list only its requests. Click a request in the tree to open it. Click *City of Tshwane* for everything. |
| **Clear filters** | Removes every filter (the sort order is kept). |

The line above the list shows how many requests match, e.g. *7 of 31 in Roads and
Transport*.

**Details tab**

Shows the selected request: reference number, title, department and category, location,
priority, when it was submitted, the team assigned and the description. Below these:

- a sentence explaining what the current status means for the resident;
- a red **Blocked** line naming the request it is waiting for, if any;
- a **progress timeline** of the six stages (Submitted, Acknowledged, Assigned, In
  progress, Resolved, Closed). Stages reached are filled in with the date reached; if the
  request is blocked, the next stage is outlined in red;
- the **progress history**, newest first, with a note for each stage.

**Advance to next stage** moves the request on by one stage. It stands in for the
municipal staff who would normally update requests, so the effect on every structure
can be seen. A blocked request cannot move to *In progress*; a message explains which
request it is waiting for. **Show related requests** opens the Related tab.

**Urgent queue tab**

Lists the ten most urgent requests still waiting for a crew: Critical before High before
Medium before Low, and within a priority the one that has waited longest first.
Double-click a row to open it. **Dispatch next job** assigns the most urgent request
that can start to its department's team, and the queue updates. Blocked requests are
skipped but keep their place in the queue.

**Related tab**

Some jobs depend on others. For example, the road on Church Street cannot be repaired
until the burst water main under it is fixed. The tab shows three groups for the
selected request:

- **Waiting on**: requests that must be resolved first, directly or indirectly;
- **Will be unblocked by this request**: requests waiting for this one;
- **Order to resolve**: the open requests in the order they must be finished so this one
  can go ahead (step 1 first).

Double-click a row to open that request.

**Crew route tab**

Choose a department (or *All departments*) to plan a route for its crew. The map shows
every suburb and road. The depot in Pretoria CBD is a blue square, and each stop with open
requests is an orange circle numbered in visiting order. The route is drawn in green along
the roads it uses. Hover over a suburb to see its name and how many open requests it has.
**Open larger map** shows the same route in a resizable window next to the route details;
press **Escape** to close it.

The map is schematic. The central suburbs are only a few kilometres apart, while
Atteridgeville, Mamelodi and Centurion are 15 to 20 km out, so a true-scale map would
squeeze the centre into a corner. Each suburb keeps its direction from the middle of the
network, but its distance from the middle is replaced by the square root of that
distance. This spreads out the centre and pulls in the edges. Distances in the text are
the real road estimates.

The text below the map gives:

- the length of the route network;
- the suggested visiting order and the length of the round trip;
- each link, with the suburbs it passes through.

**Under the hood tab**

Lists every data structure on the page: what it is used for, how many items it holds and
its shape (height, rotations, number of edges). It also runs **self-checks** that confirm
each structure is valid, e.g. that the AVL tree is balanced, the red-black tree obeys its
colour rules and the heap property holds. After you track a request, a line compares how
many steps the lookup took in the AVL tree with an unbalanced binary search tree. Click
**Run checks again** after changing some requests.

**To see the whole feature working:**

1. Report an issue on the **Report Issues** page, e.g. location `14 Burnett Street,
   Hatfield`, category *Water and Sewerage*, description `Burst pipe flooding the
   pavement.` Note the reference number in the confirmation (e.g. `MSA-2026-0027`).
2. Open **Service Request Status** and track that number. It appears in the list
   marked `*`, as *Critical* (the description mentions a burst pipe), with status
   *Submitted*. In the **Urgent queue** it joins the other Critical requests, behind
   the ones that have been waiting longer.
3. Track `MSA-2026-0008` (road markings). It is blocked. On the **Related** tab it is
   waiting on `MSA-2026-0007` (direct) and `MSA-2026-0006` (level 2), and the order to
   resolve is 0006, 0007, 0008.
4. Track `MSA-2026-0006`. Click **Advance to next stage** to move it to *Resolved*. The
   message says this unblocks `MSA-2026-0007`, which is no longer red.
5. On the **Crew route** tab choose *Roads and Transport*. The route links Arcadia,
   Gezina and Centurion to the depot.
6. Open **Under the hood** and check that every self-check says *Pass*.

---

## 5. Data structures: Service Request Status (Part 3)

Every structure on this page was written from scratch in the `DataStructures` folder,
rather than taken from the .NET collections, so that each one can be explained and
checked. `ServiceRequestManager` owns one instance of each and adds every request to
all of them through a single `Add` method, so they can never disagree. The examples
below use the 31 sample requests, which you can reproduce in the running application.

### 5.1 Summary

| Structure | Class | Feature it drives | Key operation and cost |
|---|---|---|---|
| Binary tree | `BinaryTree<TKey, TValue>` | Shared base: traversals, height, node count | Any traversal O(n) |
| Binary search tree | `BinarySearchTree<TKey, TValue>` | *Suburb starts with* search | Range query O(h + k) |
| AVL tree | `AvlTree<TKey, TValue>` | Track by reference number; list by reference | Search and insert O(log n), guaranteed |
| Red-black tree | `RedBlackTree<TKey, TValue>` | List newest or oldest first; date-range filter | Search and insert O(log n), guaranteed |
| General (n-ary) tree | `GeneralTree<T>` | Department browser with open/total counts | Subtree collection and totals O(n) |
| Min-heap | `MinHeap<T>` | Urgent queue and *Dispatch next job*; inside Dijkstra and Prim | Peek O(1); insert and remove O(log n) |
| Directed graph | `Graph<string>` | Dependencies between requests; *blocked* status | Neighbours O(degree) |
| Weighted graph | `Graph<string>` | Road network for crew routes | Neighbours O(degree) |
| Graph traversal | `GraphAlgorithms` | BFS: related requests by level. DFS: order to resolve, cycle prevention, visiting order | O(V + E) |
| Minimum spanning tree | `GraphAlgorithms` | Crew route network (Prim), checked against Kruskal | O(E log E) |

*n* = number of requests, *h* = tree height, *k* = number of results, *V* and *E* =
number of vertices and edges.

### 5.2 Binary trees

**What it is.** A tree in which every node has at most two children, a left and a right.
The *height* is the number of levels; the cost of finding something in a binary tree
depends on its height, not on how many items it holds.

**Role in the application.** `BinaryTree<TKey, TValue>` is the base class for all three
search trees. It holds what they have in common: the root, the count, the height and four
traversals:

| Traversal | Order | Used for |
|---|---|---|
| In-order | left, node, right | Lists sorted by key: *Reference number* and *Oldest first* |
| Reverse in-order | right, node, left | *Newest first* |
| Pre-order / post-order | node first / node last | Available to all trees; the general tree uses both |
| Level-order | one level at a time | *Nodes per level* on the Under the hood tab, and measuring height |

**Contribution to efficiency.** Because the search trees keep their keys in order, the
request list is produced by a single O(n) traversal every time the filters change. No
sort is needed, which would cost O(n log n). The traversals use an explicit stack or
queue instead of recursion, so even a tree that has turned into a long chain (see 5.3)
cannot overflow the call stack.

**Example.** An in-order traversal of the AVL tree returns `MSA-2025-0001`,
`MSA-2025-0002` … `MSA-2026-0026` in order. The level-order traversal shows its shape
on the Under the hood tab as *AVL nodes per level: 1, 2, 4, 8, 16*, which is a
perfectly full tree of 31 nodes.

### 5.3 Binary search trees

**What it is.** A binary tree with an ordering rule: every key in a node's left subtree
is smaller than the node's key, and every key in its right subtree is larger. A search
compares the key with the root and goes left or right, discarding half of what is left
at each step. That costs O(h) comparisons, which is O(log n) while the tree is
balanced.

**Role in the application.** `BinarySearchTree<string, List<ServiceRequest>>` indexes
requests by suburb (case-insensitive). The **Suburb starts with** box runs a *range
query*: every suburb beginning with `S` sorts between `"S"` and `"S\uffff"`. The search
skips any subtree that cannot hold a key in that range and stops as soon as it passes
the upper bound.

**Contribution to efficiency.** Without the index, every request would be checked
against the prefix on every keystroke. With it, only the branch of the tree that can
match is visited (O(h + k)), and each suburb's requests come back as a ready-made list.
Suburb names arrive in no particular order, so a plain BST stays reasonably shallow:
13 suburbs, height 7.

**Example of the weakness, and why the AVL tree exists.** The manager keeps a second,
unbalanced BST keyed by reference number, purely for comparison. Reference numbers are
issued in ascending order, so every new key is larger than all the others and goes to
the right. The tree becomes a chain of height 31. Track `MSA-2026-0026` and the Under
the hood tab reports that the AVL tree needed 5 comparisons, against 31 for the
unbalanced BST. With 10,000 requests the gap would be about 14 comparisons against
10,000.

### 5.4 AVL tree

**What it is.** A binary search tree that rebalances itself after every insert. Each node
stores its height, and its *balance factor* (left height minus right height) must stay
-1, 0 or +1. When an insert pushes a node to +2 or -2, one or two **rotations** fix it:

```
 Right-Right case (keys inserted in order 1, 2, 3)

   1                         2
    \                       / \
     2      rotate left    1   3
      \     ----------->
       3
```

The other cases are Left-Left (rotate right), Left-Right (rotate the child left, then
the node right) and Right-Left (the mirror image). A rotation only relinks a few
pointers, so it costs O(1). The height can never exceed about 1.44 log₂ n.

**Role in the application.** `AvlTree<string, ServiceRequest>` is the primary index:

- **Track a request**: the reference number is looked up in O(log n);
- **Sort by reference number**: an in-order traversal;
- **Issuing reference numbers**: `NextReferenceNumber` runs a range query for the
  current year (`MSA-2026-0000` to `MSA-2026-9999`); the last result is the highest
  number issued, so the next one is that plus one. This is how issues from the Report
  Issues page get numbers that never clash with existing requests;
- the **Related** tab and the dependency graph look up requests by reference here.

**Contribution to efficiency.** Reference numbers always arrive in sorted order, which is
the worst case for a plain BST (see 5.3). The AVL tree guarantees O(log n) lookups
however the keys arrive. Loading the 31 sample requests took 26 rotations and produced
a tree of height 5, the smallest height possible for 31 nodes.

### 5.5 Red-black tree

**What it is.** Another self-balancing binary search tree. Instead of storing heights,
each node is coloured red or black, and four rules hold after every insert:

1. Every node is red or black.
2. The root is black.
3. A red node never has a red child.
4. Every path from a node down to an empty child has the same number of black nodes.

Together, rules 3 and 4 mean that no path can be more than twice as long as any other,
so the height is at most 2 log₂(n + 1). A new node is inserted red. If its parent is also
red, the tree is repaired by recolouring (when the "uncle" node is red) or by at most two
rotations (when it is black). Compared with an AVL tree, a red-black tree is balanced
slightly less strictly, but an insert never needs more than two rotations. .NET's own
`SortedSet` and `SortedDictionary` use red-black trees.

**Role in the application.** `RedBlackTree<(DateTime, string), ServiceRequest>`
indexes requests by the moment they were submitted. The reference number is added to
the key so that two requests submitted at the same moment still have different keys.

- **Newest first** is a reverse in-order traversal, and **Oldest first** an in-order
  traversal. This is the default view, because residents usually look for their most
  recent report.
- **Submitted from / to** is a range query: only the part of the tree inside the dates
  is visited, and the walk stops at the first request after the *to* date.

**Contribution to efficiency.** The list can be shown in date order in O(n), and a date
filter costs O(log n + k) instead of checking all n requests. The sample data has a
height of 8 and a black height of 4, and needed 23 rotations. Its height is a little
more than the AVL tree's 5, as the rules above allow, but it is still logarithmic.

### 5.6 General (basic) tree

**What it is.** A tree in which a node can have any number of children. It has one root,
and every other node has exactly one parent. This is the natural shape for hierarchies
such as folders or an organisation chart.

**Role in the application.** `GeneralTree<CategoryTreeItem>` models how the municipality
is organised:

```
City of Tshwane                          (root)
├── Water and Sanitation                 (department)
│   ├── Water and Sewerage               (category)
│   │   ├── MSA-2026-0006 Burst water main      (request, a leaf)
│   │   └── ...
│   └── Sanitation
├── Energy and Electricity
├── Roads and Transport
├── Environment and Waste
└── General Services
```

- A **post-order** traversal (children before parents) adds up the *open / total*
  counts from the leaves upwards in one pass. It runs again after every change, so the
  numbers on the tree are always current.
- A **pre-order** traversal (parents before children) copies the tree into the
  `TreeView`, and collects every request below a department when you click it.
- Each node knows its parent, so a request's breadcrumb (*Water and Sanitation >
  Water and Sewerage*) is a walk up to the root.

**Contribution to efficiency.** Clicking a department collects only that branch's
requests instead of scanning every request and looking up its department. Totals for
all 45 nodes are updated in one O(n) pass rather than counted separately for each
branch.

**Example.** At start-up the tree shows *Water and Sanitation (5 open / 8)* and *Roads
and Transport (5 open / 7)*. Advance `MSA-2026-0006` to Resolved and the totals become
*4 open / 8*, and *City of Tshwane* drops by one as well.

### 5.7 Min-heap

**What it is.** A *complete* binary tree in which every parent is no larger than its
children, so the smallest item is always at the root. Because the tree is complete, it
is stored in a plain array: the children of index *i* are at *2i + 1* and *2i + 2*.

| Operation | How | Cost |
|---|---|---|
| Peek | read index 0 | O(1) |
| Insert | add at the end, then *sift up* while smaller than the parent | O(log n) |
| Remove the minimum | move the last item to the root, then *sift down* | O(log n) |
| Build from a list | sift down every parent, from the last one up (Floyd's method) | O(n) |

**Role in the application.** `MinHeap<ServiceRequest>` holds the requests waiting for a
crew, ordered by priority (Critical = 0 comes first), then by submission time, then by
reference number.

- **Urgent queue tab**: the heap is copied (O(n)) and the top ten are taken from the
  copy, so the real queue is not disturbed.
- **Dispatch next job**: removes the top of the heap and assigns it to a team. Requests
  that have moved on since they were queued are dropped when they reach the top
  (*lazy deletion*). Blocked requests are put aside and pushed back afterwards, so they
  keep their place in the queue.
- **Main menu**: the top of the heap is named as *Most urgent*.
- Dijkstra's and Prim's algorithms (5.9 and 5.10) use the same class to pick the closest
  suburb or the cheapest road next.

**Contribution to efficiency.** A sorted list would make every new request cost O(n) to
insert. Scanning an unsorted list for the most urgent request would cost O(n) for every
dispatch. The heap does both in O(log n) and reads the next job in O(1).

**Example.** The top of the heap is `MSA-2026-0016` (exposed cables, Critical, waiting 15
days), ahead of `MSA-2026-0022` (sinkhole, Critical, 4 days). Next come the High
requests `MSA-2026-0007` and `MSA-2026-0011`, but both are blocked. After the two
Critical jobs are dispatched, **Dispatch next job** skips them and assigns
`MSA-2026-0019` (fire hydrant).

### 5.8 Graphs

**What it is.** A set of *vertices* joined by *edges*. Unlike a tree there is no root, a
vertex can have any number of neighbours, and paths can loop. Edges can be **directed**
(one way) and **weighted** (with a cost). `Graph<T>` stores an *adjacency list*: a
dictionary from each vertex to the list of edges leaving it. This uses O(V + E) memory,
which suits sparse graphs far better than a V × V matrix.

**Role in the application: dependencies between requests.** A directed graph whose
vertices are reference numbers; an edge A → B means *B cannot start until A is
resolved*. The same edges are kept reversed in a second graph so that both directions
can be followed directly.

```
MSA-2026-0006 Burst water main ──► MSA-2026-0007 Road surface collapsed ──► MSA-2026-0008 Road markings
MSA-2026-0009 Substation fault ──► MSA-2026-0010 Streetlights out
                               └─► MSA-2026-0011 Traffic signals dark
MSA-2026-0012 Blocked sewer (resolved) ──► MSA-2026-0013 Playground clean-up (not blocked)
```

An open request is **blocked** while any request it waits for is still open. The graph
drives the *(blocked)* status in the list and tree, the red **Blocked** line and dashed
timeline stage on the Details tab, the rule that a blocked request cannot move to *In
progress*, and the skipping of blocked jobs when dispatching.

**Role in the application: the road network.** An undirected, weighted graph of 14
Tshwane suburbs and 24 main roads. Each road's weight is the straight-line distance
between the suburbs (haversine formula) multiplied by 1.3 to allow for bends.

**Contribution to efficiency.** Checking whether a request is blocked only reads that
request's own edges (O(degree)). It does not search every request for references to it.

### 5.9 Graph traversal

**Breadth-first search (BFS)** uses a queue. It visits the start, then all of its
neighbours, then theirs, so each vertex is reached in the fewest possible steps and is
labelled with its *level*.

- **Related tab**: BFS over the reversed graph lists everything a request waits on.
  BFS over the forward graph lists everything waiting on it. For `MSA-2026-0006`:
  `MSA-2026-0007` is *Direct* (level 1) and `MSA-2026-0008` is *Level 2*.
- **Self-check**: BFS from the depot must reach all 14 suburbs.

**Depth-first search (DFS)** uses a stack (or recursion). It follows one path as far as
it goes before backing up.

- **Order to resolve**: a DFS that adds a request only after all of its prerequisites
  (post-order). For `MSA-2026-0008` it gives step 1 `MSA-2026-0006`, step 2
  `MSA-2026-0007`, step 3 `MSA-2026-0008`.
- **Cycle prevention**: before a new dependency A → B is added, DFS checks whether B can
  already reach A. If it can, the link would make each request wait for the other
  forever, so it is refused. The self-checks try exactly this and confirm it is refused.
- **Visiting order** for the crew route: a DFS walk of the minimum spanning tree from the
  depot (5.10).
- **Topological sort** (Kahn's algorithm): a self-check that orders the whole dependency
  graph, which is only possible if it has no cycles.

Every traversal keeps a `HashSet` of visited vertices, so each runs in O(V + E) and can
never loop forever.

### 5.10 Minimum spanning tree

**What it is.** For a connected, weighted, undirected graph, a spanning tree is a set of
V - 1 edges that connects every vertex without forming a cycle. The *minimum* spanning
tree (MST) is the one with the lowest total weight.

- **Prim's algorithm** grows the tree from one vertex, each time adding the cheapest edge
  that reaches a new vertex. A min-heap of candidate edges finds that edge in O(log E).
- **Kruskal's algorithm** sorts all edges and adds each one unless its ends are already
  connected, which a disjoint-set (union-find) structure answers in almost O(1).

**Role in the application.** The **Crew route** tab plans how a department's crew can
reach every suburb where it has open requests:

1. **Dijkstra's algorithm** (with the min-heap) finds the shortest road distance between
   every pair of sites: the depot plus each suburb with open work.
2. Those distances form a complete graph of just the sites. **Prim's algorithm** finds its
   MST, the cheapest set of links that connects them all. It is drawn in green on the
   map, along the roads each link actually uses.
3. A **DFS walk** of the MST from the depot gives the visiting order. A walk like this is
   never more than twice the length of the tree, which is a standard guarantee.

Kruskal's algorithm runs on the full road network as a self-check: both algorithms must
find a tree of the same length (70.7 km out of 166.3 km of road).

**Example.** For *Roads and Transport* the open requests are in Arcadia (2), Gezina (1)
and Centurion (2). The MST links the depot to Arcadia (2.6 km), Arcadia to Gezina
(3.7 km) and the depot to Centurion (16.4 km), 22.7 km in all. The visiting order is
Pretoria CBD > Arcadia > Gezina > Centurion > Pretoria CBD, about 43.8 km. For *Water and
Sanitation* the Arcadia to Brooklyn link is drawn through Sunnyside, because that is the
shortest road path between them.

**Contribution to efficiency.** Planning routes by trying every order of *k* sites would
take O(k!) time. The MST approach takes O(k · E log V) for the shortest paths and
O(k² log k) for Prim's algorithm, and still gives a route with a guaranteed upper bound.
The map updates instantly when the department or a request's status changes.

### 5.11 Checking the structures

Correctness was treated as part of efficiency: a fast structure that is wrong is no use.
Each tree has a `Validate` method that checks its own rules:

- **BST**: keys strictly ascending in order;
- **AVL tree**: every stored height correct and every balance factor between -1 and +1;
- **Red-black tree**: all four colour rules and every parent link.

The heap checks that every parent is no larger than its children. The **Under the hood**
tab runs these checks together with cross-checks: all indexes hold the same requests,
the department totals add up, every request can be found within *height* comparisons,
the dependency graph has no cycles, a cycle is refused, every suburb is reachable, and
Prim's and Kruskal's algorithms agree. The AVL and red-black insert logic was also
tested against randomised, sorted and reverse-sorted keys (with duplicates) before being
written in C#.

---

## 6. Data structures: Local Events (Part 2)

Each data structure is used where it makes a real difference to how the page organises or
retrieves information. In .NET, `Dictionary<TKey, TValue>` is the generic hash table.

### 6.1 Stacks, queues and priority queues

| Structure | Where | How it is used |
|---|---|---|
| `Stack<SearchQuery>` | `LocalEventsForm` | **Previous search.** Each new search pushes the search it replaces. Clicking the button pops the most recent one (last in, first out), and `Peek` supplies the tooltip. |
| `Queue<SearchRecord>` | `SearchTracker` | The last 10 searches and what they found. New searches join the back; once there are more than ten, the oldest is removed from the front (first in, first out). |
| `Queue<LocalEvent>` | `SearchTracker` | The last 10 events the resident opened, managed the same way. |
| `PriorityQueue<Announcement, (int, DateTime)>` | `EventManager` | Service notices, ordered by priority (Critical, then High, then Normal) and then by start date. The queue is copied and dequeued for display, so the stored queue is never emptied. |
| `PriorityQueue<Recommendation, double>` | `RecommendationEngine` | A min-heap that keeps only the best three suggestions while every candidate is scored. |

### 6.2 Hash tables, dictionaries and sorted dictionaries

| Structure | Where | How it is used |
|---|---|---|
| `SortedDictionary<DateTime, List<LocalEvent>>` | `EventManager` | The main index: events grouped by calendar day and always kept in date order. Listing events needs no sorting, and a date-range search stops at the first day after the end date. |
| `SortedDictionary<DateTime, List<LocalEvent>>` | `EventManager.Search`, `EventSearchResult` | Search results are grouped by day in date order, ready to display under day headings. |
| `Dictionary<string, List<LocalEvent>>` | `EventManager` | Category index. A category search starts from this list instead of scanning every event. |
| `Dictionary<int, LocalEvent>` | `EventManager` | Finds an event by its ID, and rejects duplicate IDs when events are added. |
| `Dictionary<string, double>` and `Dictionary<DayOfWeek, double>` | `InterestProfile` | Interest weights for categories, keywords, topics and days of the week, used to score suggestions. |
| `Dictionary<string, Color>` | `Theme` | The colour for each category, used on the event cards. |

### 6.3 Sets

| Structure | Where | How it is used |
|---|---|---|
| `HashSet<string>` | `EventManager` | Unique categories across events and notices, used to fill the category list so each category appears exactly once. |
| `HashSet<DateTime>` | `EventManager` | Unique days that have events. Supplies the bold dates on the calendar and answers "is anything on this day?" without searching. |
| `HashSet<string>` | `LocalEvent.Tags` | Each event's topics, case-insensitive and without duplicates. Suggestions use a set intersection to find topics an event shares with the resident's interests. |
| `HashSet<int>` | `EventSearchResult`, `SearchTracker` | The IDs of events on screen and of events already opened, so suggestions can skip them. |
| `HashSet<string>` | `SearchTracker`, `InterestProfile` | Common words ignored in keyword searches, and the categories the resident chose directly. |

---

## 7. Recommendation algorithm

The suggestions use **weighted content-based scoring with top-k selection**.

1. **Interest profile.** `SearchTracker` builds a profile from the queued searches and
   opened events. Each search adds weight to:
   - the category it filtered on;
   - each meaningful word in its keyword;
   - for keyword and category searches, the categories and topics of the events it
     found, shared out across the results;
   - for a search on a single day, that day of the week.

   Each opened event adds weight to its category, its topics and its day of the week.
   Newer activity counts more than older activity: in a queue of *n* items, the newest
   counts fully and the oldest counts 1/*n*.
2. **Candidates.** These are upcoming events that the resident has not opened and that are
   not already in the current results.
3. **Scoring.** Each candidate is scored on four signals, each scaled from 0 to 1:

   | Signal | Weight | Meaning |
   |---|---|---|
   | Category | 3.0 | Interest in the event's category, compared with the strongest category |
   | Keywords | 2.0 | The share of the resident's searched words that the event matches |
   | Topics | 2.0 | Topics the event shares with the profile |
   | Day | 0.5 | Whether the event falls on a day of the week the resident often looks at |

   A small bonus is added for events happening soon, which mostly breaks ties. Events with
   too little in common with the profile are left out.
4. **Top three.** Scored events go into a min-heap of size three; whenever it holds more
   than three, the lowest score is removed. This takes O(n log k) time rather than sorting
   every candidate.
5. **Topping up.** If fewer than three events qualify, the column is filled with the
   soonest remaining events, labelled *Coming up soon*.

The reason shown on each card comes from whichever signal contributed most to its score.

---

## 8. Data storage

All data is held in memory for the duration of the session and is cleared when the
application closes.

- **Reported issues** are stored in a `List<Issue>` managed by the static `IssueManager`
  class. A `List` suits reports because they are added in order and read back as a whole.
- **Events and notices** are sample data created in `Data/SeedData.cs` when the
  application starts: 27 events across nine categories and nine notices, one of
  which has already ended to show that expired notices are hidden. Every date is
  calculated relative to the current date, so the events are always upcoming whenever the
  application is run.
- **Search history** used for suggestions is kept in `SearchTracker` for the session only.
- **Service requests** are 31 samples created in `Data/ServiceRequestSeedData.cs` when the
  Service Request Status page (or the main menu summary) is first used: 5 from last year
  and 26 from this year, covering every department and every stage, including three
  chains of dependent requests. Submission dates are relative to the current date, and
  reference numbers are issued in date order exactly as they would have been at the
  time. Each sample's history is filled in stage by stage up to its current status.
- **Suburbs and roads** for the crew route are 14 Tshwane suburbs with approximate
  coordinates and 24 main roads, also in `ServiceRequestSeedData.cs`.
- Issues reported on the Report Issues page are stored by `IssueManager` and also added
  to `ServiceRequestManager`, so they appear on the status page straight away.

---

## 9. Project structure

```
MunicipalServicesApp/
├── Controls/
│   ├── CardBase.cs               Shared drawing and behaviour for the cards
│   ├── EventCard.cs              An event in the results list
│   ├── AnnouncementCard.cs       A service notice
│   ├── RecommendationCard.cs     A suggested event with its reason
│   ├── AutoHeightLabel.cs        Label that grows to fit its text
│   ├── StatusTimeline.cs         Drawn progress timeline of a request's stages (Part 3)
│   └── RouteMapPanel.cs          Drawn map of suburbs, roads and the crew route (Part 3)
├── Data/
│   ├── SeedData.cs               Sample events and notices, dated relative to today
│   └── ServiceRequestSeedData.cs Sample requests, suburbs and roads (Part 3)
├── DataStructures/               Written from scratch for Part 3
│   ├── BinaryTreeNode.cs         Node shared by the binary trees
│   ├── BinaryTree.cs             Base class: height and traversals
│   ├── BinarySearchTree.cs       Search, insert and range queries
│   ├── AvlTree.cs                Self-balancing by height and rotations
│   ├── RedBlackTree.cs           Self-balancing by node colours
│   ├── GeneralTree.cs            N-ary tree with pre-, post- and level-order traversals
│   ├── MinHeap.cs                Array-based binary min-heap
│   ├── Graph.cs                  Adjacency-list graph, directed or undirected, weighted
│   └── GraphAlgorithms.cs        BFS, DFS, topological sort, Dijkstra, Prim, Kruskal
├── Models/
│   ├── Issue.cs                  A reported issue (Part 1)
│   ├── LocalEvent.cs             A municipal event
│   ├── Announcement.cs           A service notice
│   ├── AnnouncementPriority.cs   Critical, High or Normal
│   ├── EventCategories.cs        Category names
│   ├── SearchQuery.cs            One search: keyword, category and dates
│   ├── EventSearchResult.cs      Search results grouped by day
│   ├── InterestProfile.cs        Interest weights used for suggestions
│   ├── Recommendation.cs         A suggested event, its score and its reason
│   ├── ServiceRequest.cs         A tracked request and its status history (Part 3)
│   ├── StatusUpdate.cs           One entry in a request's history
│   ├── RequestStatus.cs          The six stages and their descriptions
│   ├── RequestPriority.cs        Critical, High, Medium or Low
│   ├── ServiceDepartments.cs     Departments, categories, teams and priorities
│   ├── SuburbLocation.cs         A suburb's coordinates and distances
│   └── ServiceRequestReports.cs  Query, tracking, route and self-check results
├── Services/
│   ├── IssueManager.cs           Stores reported issues (Part 1)
│   ├── EngagementTracker.cs      Progress feedback for reports (Part 1)
│   ├── EventManager.cs           Event and notice storage, indexes and search
│   ├── SearchTracker.cs          Records searches and opened events
│   ├── RecommendationEngine.cs   Scores and selects suggested events
│   └── ServiceRequestManager.cs  Owns every Part 3 structure and keeps them in step
├── UI/
│   └── Theme.cs                  Shared colours, fonts and button styles
├── MainMenuForm.cs               Main menu presented on startup
├── ReportIssuesForm.cs           Issue reporting form (Part 1)
├── LocalEventsForm.cs            Local Events and Announcements page (Part 2)
├── ServiceRequestStatusForm.cs   Service Request Status page (Part 3)
└── Program.cs                    Application entry point
```

---

## 10. Known limitations

- Reported issues, events and search history are not saved between sessions.
- Events and notices are sample data; there is no screen for municipal staff to add them.
- Attachments are recorded by file path only; the file itself is not copied or embedded.
- Reference numbers are unique within a session, but because nothing is saved they
  start again from the sample data each time the application is launched.
- Request statuses are changed with **Advance to next stage** and **Dispatch next job**,
  which stand in for municipal staff. There is no separate staff login.
- Dependencies between requests come from the sample data. The manager supports adding
  new ones (`TryAddDependency`), but there is no screen for it.
- Suburb coordinates are approximate and road distances are estimates, so crew routes
  are illustrative rather than real driving directions.
- An issue whose location does not name one of the 14 suburbs is still tracked, but it
  cannot be placed on the crew route map.

---

## 11. Troubleshooting

**The solution will not build, and the target framework is reported as missing.**
Install the .NET 8.0 SDK, then reopen the solution.

**The designer will not open for a form.**
Build the solution first (F6). The forms style themselves from `Theme.cs` at runtime, so
the project must compile before the designer can load them.

**An attached file cannot be read.**
Confirm the file still exists at its original location and is not open in another
application.

**The Local Events window does not fit on the screen.**
The window opens at 1160 × 660 and can be resized down to 1060 × 640, or maximised.
The event, notice and suggestion columns scroll independently.

**The Service Request Status window does not fit on the screen.**
It opens at 1360 × 800 and opens maximised automatically on smaller screens. It can be
resized down to 1180 × 700.

**A reference number is not found.**
Check the year and number on the confirmation message. Reference numbers from an earlier
run of the application are not kept (see Known limitations).

**A request cannot be moved to In progress.**
It is waiting for another request to be resolved. Its Details tab names the request, and
the Related tab shows the order in which to resolve them.

**A self-check on the Under the hood tab says FAIL.**
This should not happen. Note which check failed and its detail text, then restart the
application.

**A search returns no events.**
Check that the From and To dates are not too narrow, or choose *All categories*. Days
shown in bold on the calendar have at least one event.

---

## 12. Author

| | |
|---|---|
| Name | Lesego Letsapa |
| Student number | ST10351970 |
| Module | Advanced Application Development (AAPD7112w) |
| Assessment | Part 3, Portfolio of Evidence (complete application) |
| Repository | https://github.com/cptlsg/AAPD7112 |
