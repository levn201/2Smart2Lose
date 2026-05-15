# Workshop Live Dashboard – Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Admins und Quiz-Autoren können per Klick von der Fragebogen-Liste aus ein Live-Dashboard öffnen, das alle aktiven Teilnehmer eines laufenden Quizzes mit Fortschrittsbalken, Punkten und Aktivitätsstatus anzeigt.

**Architecture:** Die Teilnehmerfortschritte werden bei jeder Antwort per `INSERT ... ON DUPLICATE KEY UPDATE` in die neue Tabelle `WorkshopTeilnehmer` geschrieben. Das Dashboard pollt alle 5 Sekunden per `fetch()` einen AJAX-Handler der JSON zurückgibt und aktualisiert die Tabelle ohne Seitenreload. Kein SignalR, keine Sessions.

**Tech Stack:** ASP.NET Core 8 Razor Pages · Raw MySql.Data (kein EF Core) · Bootstrap 5 · Vanilla JS fetch/setInterval

> **Hinweis:** Kein Test-Projekt vorhanden (siehe CLAUDE.md). Statt `dotnet test` wird nach jedem Task `dotnet build` zur Verifikation ausgeführt und am Ende manuell getestet.

---

## Betroffene Dateien

| Aktion | Datei |
|---|---|
| Erstellen (SQL) | `Smart2Lose/SQL/create_workshop_teilnehmer.sql` |
| Modifizieren | `Smart2Lose/Pages/1Viewer/Playground.cshtml.cs` |
| Modifizieren | `Smart2Lose/Pages/Admin/Frageboegen.cshtml` |
| Erstellen | `Smart2Lose/Pages/Admin/WorkshopDashboard.cshtml.cs` |
| Erstellen | `Smart2Lose/Pages/Admin/WorkshopDashboard.cshtml` |
| Erstellen | `Smart2Lose/wwwroot/css/Admin/WorkshopDashboard.css` |

---

## Task 1: WorkshopTeilnehmer Tabelle anlegen

**Files:**
- Create: `Smart2Lose/SQL/create_workshop_teilnehmer.sql`

- [ ] **Schritt 1: SQL-Datei erstellen**

Erstelle `Smart2Lose/SQL/create_workshop_teilnehmer.sql`:

```sql
CREATE TABLE IF NOT EXISTS WorkshopTeilnehmer (
    GamePin       INT          NOT NULL,
    Nickname      VARCHAR(100) NOT NULL,
    AktuelleOffset INT         NOT NULL DEFAULT 0,
    QuestionCount  INT         NOT NULL DEFAULT 0,
    Punkte        INT          NOT NULL DEFAULT 0,
    LetztesUpdate DATETIME     NOT NULL,
    PRIMARY KEY (GamePin, Nickname)
);
```

`GamePin` entspricht `Fragebogen.Join_ID` als INT (gleicher Typ wie `sd.GameID` im Playground).
`AktuelleOffset` = Anzahl beantworteter Fragen (nicht SQL-OFFSET). Wertebereich 0–QuestionCount.

- [ ] **Schritt 2: Tabelle in der Datenbank anlegen**

```bash
mysql -u <user> -p KahootDatabase < Smart2Lose/SQL/create_workshop_teilnehmer.sql
```

Erwartetes Ergebnis: kein Fehler, Tabelle `WorkshopTeilnehmer` existiert.

- [ ] **Schritt 3: Commit**

```bash
git add Smart2Lose/SQL/create_workshop_teilnehmer.sql
git commit -m "feat: add WorkshopTeilnehmer table migration"
```

---

## Task 2: Playground – Tracking nach jeder Antwort

**Files:**
- Modify: `Smart2Lose/Pages/1Viewer/Playground.cshtml.cs`

- [ ] **Schritt 1: Private Hilfsmethode hinzufügen**

Füge am Ende der Klasse `PlaygroundModel` (vor der letzten `}`) diese Methode ein:

```csharp
private void AktualisiereWorkshopTracking()
{
    try
    {
        var db = new SQLconnection.DatenbankZugriff();
        using var con = db.GetConnection();
        con.Open();
        var cmd = new MySqlCommand(@"
            INSERT INTO WorkshopTeilnehmer (GamePin, Nickname, AktuelleOffset, QuestionCount, Punkte, LetztesUpdate)
            VALUES (@pin, @nick, @offset, @count, @pts, NOW())
            ON DUPLICATE KEY UPDATE
                AktuelleOffset = @offset,
                QuestionCount  = @count,
                Punkte         = @pts,
                LetztesUpdate  = NOW()", con);
        cmd.Parameters.AddWithValue("@pin",    sd.GameID);
        cmd.Parameters.AddWithValue("@nick",   sd.UserName);
        cmd.Parameters.AddWithValue("@offset", CurrentOffset + 1);
        cmd.Parameters.AddWithValue("@count",  QuestionCount);
        cmd.Parameters.AddWithValue("@pts",    fp.PlayerPoints);
        cmd.ExecuteNonQuery();
    }
    catch
    {
        // Tracking-Fehler sollen das Spielerlebnis nicht unterbrechen
    }
}
```

- [ ] **Schritt 2: Methode in `OnPostCheckAnswer` aufrufen**

In `OnPostCheckAnswer` nach `ComputeProgress();` und vor `return Page();`:

```csharp
AllQuestionStates[CurrentOffset] = new QuestionState { Correct = isCorrect, SelectedAnswer = selectedAnswer };
SaveQuestionStates(AllQuestionStates);

ComputeProgress();
AktualisiereWorkshopTracking();   // NEU
return Page();
```

- [ ] **Schritt 3: Build-Check**

```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```

Erwartetes Ergebnis: `Build succeeded. 0 Error(s)`

- [ ] **Schritt 4: Commit**

```bash
git add Smart2Lose/Pages/1Viewer/Playground.cshtml.cs
git commit -m "feat: track workshop participant progress on answer"
```

---

## Task 3: Dashboard-Button in Frageboegen.cshtml

**Files:**
- Modify: `Smart2Lose/Pages/Admin/Frageboegen.cshtml`

- [ ] **Schritt 1: Dashboard-Link innerhalb der Auth-Bedingung ergänzen**

Im bestehenden `@if`-Block mit den Bearbeiten/Löschen-Buttons (Zeile ~85) den neuen Link nach dem Löschen-Formular einfügen:

```cshtml
@if (User.IsInRole("Admin") || (User.IsInRole("User") && Model.fHelper.CheckIfPlayerIsAutor(fragebogen.JoinId)))
{
    <form method="post" asp-page-handler="Edit" style="display:inline;">
        <input type="hidden" name="id" value="@fragebogen.JoinId" />
        <button type="submit" class="knopf">Bearbeiten</button>
    </form>

    <form method="post" asp-page-handler="Loeschen" style="display:inline;">
        <input type="hidden" name="id" value="@fragebogen.JoinId" />
        <button type="submit" class="knopf gefahr">Löschen</button>
    </form>

    <a asp-page="/Admin/WorkshopDashboard"
       asp-route-joinId="@fragebogen.JoinId"
       class="knopf">Live</a>
}
```

- [ ] **Schritt 2: Build-Check**

```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```

Erwartetes Ergebnis: `Build succeeded. 0 Error(s)`

- [ ] **Schritt 3: Commit**

```bash
git add Smart2Lose/Pages/Admin/Frageboegen.cshtml
git commit -m "feat: add live dashboard link to Frageboegen list"
```

---

## Task 4: WorkshopDashboard.cshtml.cs

**Files:**
- Create: `Smart2Lose/Pages/Admin/WorkshopDashboard.cshtml.cs`

- [ ] **Schritt 1: Page Model erstellen**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Smart2Lose.Model;
using System.Security.Claims;
using static Smart2Lose.Helper.SQLconnection;

namespace Smart2Lose.Pages.Admin
{
    [Authorize(Roles = "Admin,User")]
    public class WorkshopDashboardModel : PageModel
    {
        public projektName pn = new projektName();

        [BindProperty(SupportsGet = true)]
        public int JoinId { get; set; }

        public string FragebogenTitel { get; set; } = "";
        public List<TeilnehmerRow> Teilnehmer { get; set; } = new();

        public class TeilnehmerRow
        {
            public string Nickname { get; set; } = "";
            public int AktuelleOffset { get; set; }
            public int QuestionCount { get; set; }
            public int Punkte { get; set; }
            public DateTime LetztesUpdate { get; set; }
        }

        public IActionResult OnGet()
        {
            if (!IstAutorisiert())
                return RedirectToPage("/Admin/Frageboegen");

            FragebogenTitel = LadeFragebogenTitel();
            Teilnehmer = LadeTeilnehmer();
            return Page();
        }

        public IActionResult OnGetData()
        {
            if (!IstAutorisiert())
                return Unauthorized();

            return new JsonResult(LadeTeilnehmer());
        }

        private bool IstAutorisiert()
        {
            if (User.IsInRole("Admin"))
                return true;

            if (!User.IsInRole("User"))
                return false;

            var autorEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var db = new DatenbankZugriff();
            using var con = db.GetConnection();
            con.Open();
            using var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM Fragebogen WHERE Join_ID = @id AND Autor = @autor", con);
            cmd.Parameters.AddWithValue("@id",    JoinId);
            cmd.Parameters.AddWithValue("@autor", autorEmail);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private string LadeFragebogenTitel()
        {
            var db = new DatenbankZugriff();
            using var con = db.GetConnection();
            con.Open();
            using var cmd = new MySqlCommand(
                "SELECT Titel FROM Fragebogen WHERE Join_ID = @id", con);
            cmd.Parameters.AddWithValue("@id", JoinId);
            return cmd.ExecuteScalar()?.ToString() ?? "Unbekannt";
        }

        private List<TeilnehmerRow> LadeTeilnehmer()
        {
            var liste = new List<TeilnehmerRow>();
            var db = new DatenbankZugriff();
            using var con = db.GetConnection();
            con.Open();
            using var cmd = new MySqlCommand(@"
                SELECT Nickname, AktuelleOffset, QuestionCount, Punkte, LetztesUpdate
                FROM WorkshopTeilnehmer
                WHERE GamePin = @pin
                ORDER BY AktuelleOffset DESC, Punkte DESC", con);
            cmd.Parameters.AddWithValue("@pin", JoinId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                liste.Add(new TeilnehmerRow
                {
                    Nickname       = reader.GetString("Nickname"),
                    AktuelleOffset = reader.GetInt32("AktuelleOffset"),
                    QuestionCount  = reader.GetInt32("QuestionCount"),
                    Punkte         = reader.GetInt32("Punkte"),
                    LetztesUpdate  = reader.GetDateTime("LetztesUpdate")
                });
            }
            return liste;
        }
    }
}
```

- [ ] **Schritt 2: Build-Check**

```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```

Erwartetes Ergebnis: `Build succeeded. 0 Error(s)`

- [ ] **Schritt 3: Commit**

```bash
git add Smart2Lose/Pages/Admin/WorkshopDashboard.cshtml.cs
git commit -m "feat: add WorkshopDashboard page model with polling handler"
```

---

## Task 5: WorkshopDashboard.cshtml + CSS

**Files:**
- Create: `Smart2Lose/Pages/Admin/WorkshopDashboard.cshtml`
- Create: `Smart2Lose/wwwroot/css/Admin/WorkshopDashboard.css`

- [ ] **Schritt 1: CSS erstellen**

```css
/* Smart2Lose/wwwroot/css/Admin/WorkshopDashboard.css */

.dashboard-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    flex-wrap: wrap;
    gap: 16px;
    margin-bottom: 28px;
}

.stats-reihe {
    display: flex;
    gap: 16px;
    flex-wrap: wrap;
    margin-bottom: 28px;
}

.stat-karte {
    background: var(--card-bg, #1e1e2e);
    border: 1px solid var(--border-color, #2a2d3e);
    border-radius: 10px;
    padding: 16px 24px;
    min-width: 130px;
}

.stat-karte .zahl {
    font-size: 32px;
    font-weight: 800;
}

.stat-karte .bezeichnung {
    font-size: 12px;
    opacity: 0.6;
    margin-top: 2px;
}

.fortschritt-balken {
    background: rgba(255, 255, 255, 0.1);
    border-radius: 4px;
    height: 8px;
    overflow: hidden;
    min-width: 120px;
}

.fortschritt-balken .fuellstand {
    height: 100%;
    border-radius: 4px;
    background: #6c63ff;
    transition: width 0.4s ease;
}

.fuellstand.fertig  { background: #22c55e; }
.fuellstand.inaktiv { background: #6b7280; }

.aktualisierung-info {
    font-size: 13px;
    opacity: 0.5;
    margin-top: 4px;
}

.teilnehmer-tabelle {
    width: 100%;
    border-collapse: collapse;
}

.teilnehmer-tabelle th {
    text-align: left;
    padding: 10px 16px;
    font-size: 12px;
    opacity: 0.5;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    border-bottom: 1px solid var(--border-color, #2a2d3e);
}

.teilnehmer-tabelle td {
    padding: 14px 16px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.04);
    vertical-align: middle;
}

.teilnehmer-tabelle tr:last-child td {
    border-bottom: none;
}

.badge-fertig  { background: rgba(34,197,94,0.15);  color: #22c55e; padding: 3px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }
.badge-aktiv   { background: rgba(108,99,255,0.15); color: #6c63ff; padding: 3px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }
.badge-inaktiv { background: rgba(107,114,128,0.15);color: #9ca3af; padding: 3px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }
```

- [ ] **Schritt 2: Razor Page erstellen**

```cshtml
@page "{joinId:int}"
@model Smart2Lose.Pages.Admin.WorkshopDashboardModel
@{
    ViewData["Title"] = "Live Dashboard – " + Model.FragebogenTitel;
}

<link rel="stylesheet" href="~/css/Admin/GrundDashboard.css" asp-append-version="true" />
<link rel="stylesheet" href="~/css/Admin/WorkshopDashboard.css" asp-append-version="true" />

<header class="header">
    <div class="logo">@Model.pn.Name</div>
    <nav class="nav-links">
        <a href="/Admin/Dashboard">Dashboard</a>
        <a href="/Admin/Frageboegen" class="active">Fragebögen</a>
        <a href="/Index">Quit</a>
    </nav>
</header>

<div style="max-width:1100px; margin:32px auto; padding:0 16px;">

    <div class="dashboard-header">
        <div>
            <h1 class="seiten-titel">Live Dashboard</h1>
            <p style="opacity:.6; margin-top:4px;">
                @Model.FragebogenTitel &middot; PIN: <strong>@Model.JoinId</strong>
            </p>
            <p class="aktualisierung-info" id="aktualisierungInfo">Aktualisiert vor 0s</p>
        </div>
        <a href="/Admin/Frageboegen" class="knopf">&larr; Zurück</a>
    </div>

    <div class="stats-reihe">
        <div class="stat-karte">
            <div class="zahl" id="statFertig" style="color:#22c55e">0</div>
            <div class="bezeichnung">Fertig</div>
        </div>
        <div class="stat-karte">
            <div class="zahl" id="statAktiv" style="color:#6c63ff">0</div>
            <div class="bezeichnung">Aktiv</div>
        </div>
        <div class="stat-karte">
            <div class="zahl" id="statInaktiv" style="color:#9ca3af">0</div>
            <div class="bezeichnung">Inaktiv</div>
        </div>
        <div class="stat-karte">
            <div class="zahl" id="statGesamt">@Model.Teilnehmer.Count</div>
            <div class="bezeichnung">Gesamt</div>
        </div>
    </div>

    <div style="background:var(--card-bg,#1e1e2e); border:1px solid var(--border-color,#2a2d3e); border-radius:12px; padding:8px 0;">
        <table class="teilnehmer-tabelle">
            <thead>
                <tr>
                    <th>Nickname</th>
                    <th>Fortschritt</th>
                    <th>Frage</th>
                    <th>Punkte</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody id="teilnehmerBody">
                @foreach (var t in Model.Teilnehmer)
                {
                    var prozent = t.QuestionCount > 0
                        ? (int)((double)t.AktuelleOffset / t.QuestionCount * 100)
                        : 0;
                    var fertig  = t.AktuelleOffset >= t.QuestionCount && t.QuestionCount > 0;
                    var inaktiv = !fertig && (DateTime.UtcNow - t.LetztesUpdate.ToUniversalTime()).TotalMinutes >= 2;
                    var fuellung    = fertig ? "fertig" : (inaktiv ? "inaktiv" : "");
                    var badgeKlasse = fertig ? "badge-fertig" : (inaktiv ? "badge-inaktiv" : "badge-aktiv");
                    var statusText  = fertig ? "Fertig" : (inaktiv ? "Inaktiv" : "Aktiv");
                    <tr>
                        <td><strong>@t.Nickname</strong></td>
                        <td>
                            <div class="fortschritt-balken">
                                <div class="fuellstand @fuellung" style="width:@prozent%"></div>
                            </div>
                        </td>
                        <td>@t.AktuelleOffset / @t.QuestionCount</td>
                        <td><strong>@t.Punkte</strong></td>
                        <td><span class="@badgeKlasse">@statusText</span></td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
</div>

<script>
    const joinId = @Model.JoinId;
    const INAKTIV_MINUTEN = 2;
    let sekundenSeit = 0;

    // XSS-Schutz: Nutzereingaben (Nickname) vor DOM-Einfügen escapen
    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function statusBerechnen(aktuelleOffset, questionCount, letztesUpdateIso) {
        const fertig = questionCount > 0 && aktuelleOffset >= questionCount;
        if (fertig) return 'fertig';
        const diffMinuten = (Date.now() - new Date(letztesUpdateIso).getTime()) / 60000;
        return diffMinuten >= INAKTIV_MINUTEN ? 'inaktiv' : 'aktiv';
    }

    function zeileErstellen(t) {
        const status   = statusBerechnen(t.aktuelleOffset, t.questionCount, t.letztesUpdate);
        const prozent  = t.questionCount > 0
            ? Math.round(t.aktuelleOffset / t.questionCount * 100) : 0;
        const fuellung    = status === 'fertig' ? 'fertig' : (status === 'inaktiv' ? 'inaktiv' : '');
        const badgeKlasse = status === 'fertig' ? 'badge-fertig' : (status === 'inaktiv' ? 'badge-inaktiv' : 'badge-aktiv');
        const badgeText   = status === 'fertig' ? 'Fertig' : (status === 'inaktiv' ? 'Inaktiv' : 'Aktiv');

        const tr = document.createElement('tr');

        const tdName = document.createElement('td');
        const strong = document.createElement('strong');
        strong.textContent = t.nickname;   // textContent verhindert XSS
        tdName.appendChild(strong);

        const tdBalken = document.createElement('td');
        const balken = document.createElement('div');
        balken.className = 'fortschritt-balken';
        const fuell = document.createElement('div');
        fuell.className = 'fuellstand ' + fuellung;
        fuell.style.width = prozent + '%';
        balken.appendChild(fuell);
        tdBalken.appendChild(balken);

        const tdFrage = document.createElement('td');
        tdFrage.textContent = t.aktuelleOffset + ' / ' + t.questionCount;

        const tdPunkte = document.createElement('td');
        const strongPts = document.createElement('strong');
        strongPts.textContent = t.punkte;
        tdPunkte.appendChild(strongPts);

        const tdStatus = document.createElement('td');
        const span = document.createElement('span');
        span.className = badgeKlasse;
        span.textContent = badgeText;
        tdStatus.appendChild(span);

        tr.appendChild(tdName);
        tr.appendChild(tdBalken);
        tr.appendChild(tdFrage);
        tr.appendChild(tdPunkte);
        tr.appendChild(tdStatus);
        return tr;
    }

    function statsAktualisieren(daten) {
        let fertig = 0, aktiv = 0, inaktiv = 0;
        daten.forEach(t => {
            const s = statusBerechnen(t.aktuelleOffset, t.questionCount, t.letztesUpdate);
            if (s === 'fertig') fertig++;
            else if (s === 'inaktiv') inaktiv++;
            else aktiv++;
        });
        document.getElementById('statFertig').textContent  = fertig;
        document.getElementById('statAktiv').textContent   = aktiv;
        document.getElementById('statInaktiv').textContent = inaktiv;
        document.getElementById('statGesamt').textContent  = daten.length;
    }

    function aktualisieren() {
        fetch('/Admin/WorkshopDashboard/' + joinId + '?handler=Data')
            .then(function(r) { return r.json(); })
            .then(function(daten) {
                const tbody = document.getElementById('teilnehmerBody');
                tbody.replaceChildren.apply(tbody, daten.map(zeileErstellen));
                statsAktualisieren(daten);
                sekundenSeit = 0;
            })
            .catch(function() { /* Netzwerkfehler ignorieren, naechster Versuch in 5s */ });
    }

    setInterval(function() {
        sekundenSeit++;
        document.getElementById('aktualisierungInfo').textContent =
            'Aktualisiert vor ' + sekundenSeit + 's';
    }, 1000);

    setInterval(aktualisieren, 5000);

    // Initiale Stats aus server-seitig gerendertem HTML berechnen
    document.addEventListener('DOMContentLoaded', function() {
        const zeilen = document.querySelectorAll('#teilnehmerBody tr');
        let fertig = 0, aktiv = 0, inaktiv = 0;
        zeilen.forEach(function(z) {
            const badge = z.querySelector('span');
            if (!badge) return;
            if (badge.classList.contains('badge-fertig'))  fertig++;
            else if (badge.classList.contains('badge-inaktiv')) inaktiv++;
            else aktiv++;
        });
        document.getElementById('statFertig').textContent  = fertig;
        document.getElementById('statAktiv').textContent   = aktiv;
        document.getElementById('statInaktiv').textContent = inaktiv;
    });
</script>
```

- [ ] **Schritt 3: Build-Check**

```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```

Erwartetes Ergebnis: `Build succeeded. 0 Error(s)`

- [ ] **Schritt 4: Commit**

```bash
git add Smart2Lose/Pages/Admin/WorkshopDashboard.cshtml
git add Smart2Lose/wwwroot/css/Admin/WorkshopDashboard.css
git commit -m "feat: add WorkshopDashboard view with XSS-safe live polling"
```

---

## Task 6: Manueller End-to-End-Test

- [ ] **Schritt 1: App starten**

```bash
dotnet run --project Smart2Lose/Smart2Lose.csproj
```

- [ ] **Schritt 2: Teilnehmer spielen lassen**

1. Browser-Tab 1: `/Index` öffnen, gültigen Join-PIN eingeben, Nickname eingeben
2. Mindestens 2 Fragen beantworten ("Antwort Prüfen" klicken)

- [ ] **Schritt 3: Dashboard als Admin prüfen**

1. Browser-Tab 2: Als Admin einloggen (`admin@smart2lose.com` / `Admin123!`)
2. `/Admin/Frageboegen` → "Live"-Button ist neben dem Quiz sichtbar
3. Klicken → Dashboard öffnet sich mit korrektem Titel und PIN
4. Teilnehmer erscheint in Tabelle mit Fortschrittsbalken, Fragen-Zähler, Punkte
5. 5 Sekunden warten → Zähler läuft hoch
6. In Tab 1 weitere Frage beantworten → innerhalb 5s aktualisiert sich Tab 2

- [ ] **Schritt 4: Autorisierung prüfen**

1. Als `User`-Rolle: eigener Fragebogen → "Live"-Button sichtbar ✓
2. Als `User`-Rolle: fremder Fragebogen → kein Button ✓
3. URL eines fremden Dashboards direkt aufrufen → Redirect zu Frageboegen ✓

- [ ] **Schritt 5: Abschluss-Commit**

```bash
git add .
git commit -m "feat: workshop live dashboard complete"
```
