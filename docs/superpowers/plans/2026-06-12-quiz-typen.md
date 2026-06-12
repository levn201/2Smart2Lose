# Quiz-Typen & Punkte-Entfernung — Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Zwei neue Fragetypen (TrueFalse, Passwort) hinzufügen und das Punktesystem vollständig aus allen Quiz-Typen entfernen; Auswertung nur noch über Richtig/Gesamt.

**Architecture:** Neue Spalte `Typ` in `Fragebogen` (raw SQL, kein EF). Playground liest `Typ` aus DB, cached in Session, rendert typ-abhängige UI. `FrageboegenErstellen` bekommt einen 3-Karten-Selector und normalisiert Antwortfelder serverseitig. Punkte-Logik wird komplett entfernt.

**Tech Stack:** ASP.NET Core 8 Razor Pages · Raw `MySql.Data` · Bootstrap 5 · Newtonsoft.Json · Session-State

---

### Task 1: DB-Migration — `Typ`-Spalte in `Fragebogen`

**Files:**
- Manual SQL auf `KahootDatabase`

- [ ] **Step 1: Migration ausführen**

In MySQL-Client oder Workbench:
```sql
ALTER TABLE Fragebogen
  ADD COLUMN Typ VARCHAR(20) NOT NULL DEFAULT 'MC';
```

- [ ] **Step 2: Spalte prüfen**
```sql
DESCRIBE Fragebogen;
SELECT Join_ID, Titel, Typ FROM Fragebogen LIMIT 5;
```
Erwartet: Spalte `Typ` vorhanden, alle Zeilen zeigen `'MC'`.

- [ ] **Step 3: Commit**
```bash
git add .
git commit -m "db: add Typ column to Fragebogen (default MC)"
```

---

### Task 2: `Fragebogen` C#-Model — `Typ` Property

**Files:**
- Modify: `Smart2Lose/Model/Fragebogen.cs`

- [ ] **Step 1: Property und FromReader aktualisieren**

Vollständige neue Datei `Smart2Lose/Model/Fragebogen.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;

namespace Smart2Lose.Model
{
    public class Fragebogen
    {
        public int JoinId { get; set; }
        public string Titel { get; set; } = "";
        public string Autor { get; set; } = "Keins";
        public string Kategorie { get; set; } = "Unbekannt";
        public string Typ { get; set; } = "MC";
        public DateTime ErstelltAm { get; set; }

        public List<Fragen> Fragen { get; set; } = new();

        public static Fragebogen FromReader(MySql.Data.MySqlClient.MySqlDataReader reader)
        {
            int typOrd = reader.GetOrdinal("Typ");
            return new Fragebogen
            {
                JoinId     = reader.GetInt32("Join_ID"),
                Titel      = reader.GetString("Titel"),
                Autor      = reader.GetString("Autor"),
                Kategorie  = reader.GetString("Kategorie"),
                Typ        = reader.IsDBNull(typOrd) ? "MC" : reader.GetString(typOrd),
                ErstelltAm = reader.GetDateTime("ErstelltAm")
            };
        }
    }
}
```

- [ ] **Step 2: Build**
```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```
Erwartet: 0 Errors.

- [ ] **Step 3: Commit**
```bash
git add Smart2Lose/Model/Fragebogen.cs
git commit -m "feat: add Typ property to Fragebogen model"
```

---

### Task 3: Punkte entfernen — `FragenPruefung` + `Playground.cshtml.cs`

**Files:**
- Modify: `Smart2Lose/Model/FragenPruefung.cs`
- Modify: `Smart2Lose/Pages/1Viewer/Playground.cshtml.cs`

- [ ] **Step 1: `PlayerPoints` aus `FragenPruefung` entfernen**

Vollständige neue Datei `Smart2Lose/Model/FragenPruefung.cs`:
```csharp
namespace Smart2Lose.Model
{
    public class FragenPruefung
    {
        public int RightAnswer { get; set; }
        public bool AnswerChecked { get; set; }
        public bool AnswerCorrect { get; set; }
    }
}
```

- [ ] **Step 2: `loadHTTP()` in `Playground.cshtml.cs` aktualisieren**

`loadHTTP()` ersetzen (PlayerPoints-Zeile entfernen):
```csharp
private void loadHTTP()
{
    sd.GameID  = HttpContext.Session.GetInt32("GameNumber") ?? 0;
    sd.UserName = HttpContext.Session.GetString("Name") ?? "";
    fp.RightAnswer = HttpContext.Session.GetInt32("RightAnswer") ?? 0;
}
```

- [ ] **Step 3: +100/−5 Scoring aus `OnPostCheckAnswer()` entfernen**

Den Scoring-Block ersetzen. Vorher:
```csharp
if (isCorrect)
{
    fp.RightAnswer += 1;
    fp.PlayerPoints += 100;
    HttpContext.Session.SetInt32("PlayerPoints", fp.PlayerPoints);
    HttpContext.Session.SetInt32("RightAnswer", fp.RightAnswer);
}
else
{
    fp.PlayerPoints -= 5;
    HttpContext.Session.SetInt32("PlayerPoints", fp.PlayerPoints);
}
```
Nachher:
```csharp
if (isCorrect)
{
    fp.RightAnswer += 1;
    HttpContext.Session.SetInt32("RightAnswer", fp.RightAnswer);
}
```

- [ ] **Step 4: `AktualisiereWorkshopTracking()` — Punkte auf 0 setzen**

Den `MySqlCommand`-Block ersetzen:
```csharp
using var cmd = new MySqlCommand(@"
    INSERT INTO WorkshopTeilnehmer (GamePin, Nickname, AktuelleOffset, QuestionCount, Punkte, LetztesUpdate)
    VALUES (@pin, @nick, @offset, @count, 0, NOW())
    ON DUPLICATE KEY UPDATE
        AktuelleOffset = @offset,
        QuestionCount  = @count,
        Punkte         = 0,
        LetztesUpdate  = NOW()", connection);
cmd.Parameters.AddWithValue("@pin",    sd.GameID);
cmd.Parameters.AddWithValue("@nick",   sd.UserName);
cmd.Parameters.AddWithValue("@offset", CurrentOffset + 1);
cmd.Parameters.AddWithValue("@count",  QuestionCount);
cmd.ExecuteNonQuery();
```

- [ ] **Step 5: `OnPostFinishQuiz()` — SessionPints immer 0 schreiben**

Den Rumpf von `OnPostFinishQuiz()` ersetzen:
```csharp
public IActionResult OnPostFinishQuiz()
{
    loadHTTP();

    var db = new SQLconnection.DatenbankZugriff();
    using var connection = db.GetConnection();
    connection.Open();

    string query = @"INSERT INTO playerpoints
        (User_Nickname, SessionPints, GamePin, CorrectAnswered, PossibleAnswers)
        VALUES (@name, 0, @pin, @correct, @possible);";

    using var cmd = new MySqlCommand(query, connection);
    cmd.Parameters.AddWithValue("@pin",      sd.GameID);
    cmd.Parameters.AddWithValue("@name",     sd.UserName);
    cmd.Parameters.AddWithValue("@correct",  fp.RightAnswer);
    cmd.Parameters.AddWithValue("@possible", spiel.HowManyQuestions(sd.GameID));
    cmd.ExecuteNonQuery();

    return RedirectToPage("/1Viewer/FinalResult");
}
```

- [ ] **Step 6: Build**
```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```
Erwartet: 0 Errors.

- [ ] **Step 7: Commit**
```bash
git add Smart2Lose/Model/FragenPruefung.cs Smart2Lose/Pages/1Viewer/Playground.cshtml.cs
git commit -m "feat: remove points scoring from Playground and FragenPruefung"
```

---

### Task 4: Punkte entfernen — `Filter.cs` + `FinalResult.cshtml.cs`

**Files:**
- Modify: `Smart2Lose/Helper/Filter.cs`
- Modify: `Smart2Lose/Pages/1Viewer/FinalResult.cshtml.cs`

- [ ] **Step 1: `Filter.cs` — Sortierung auf `CorrectAnswered DESC`**

Vollständige neue Datei `Smart2Lose/Helper/Filter.cs`:
```csharp
namespace Smart2Lose.Helper
{
    public class Filter
    {
        public string DefaultQuery { get; } = @"
            SELECT SessionPints, User_Nickname, GamePin, CorrectAnswered, PossibleAnswers, saveTime
            FROM PlayerPoints
            WHERE GamePin = @GamePin
            ORDER BY CorrectAnswered DESC;";

        public string Last24hQuery { get; } = @"
            SELECT SessionPints, User_Nickname, GamePin, CorrectAnswered, PossibleAnswers, saveTime
            FROM PlayerPoints
            WHERE GamePin = @GamePin
              AND saveTime >= NOW() - INTERVAL 24 HOUR
            ORDER BY CorrectAnswered DESC;";
    }
}
```

- [ ] **Step 2: `PlayerList` — `Points` entfernen**

In `FinalResult.cshtml.cs` die innere Klasse `PlayerList` ersetzen:
```csharp
public class PlayerList
{
    public string Nickname { get; set; }
    public int GamePin { get; set; }
    public int korrekteFagen { get; set; }
    public int alleFragen { get; set; }
    public DateTime Time { get; set; }
}
```

- [ ] **Step 3: `while (reader.Read())` Block aktualisieren**

Ersetzen (Points-Zeile entfernen):
```csharp
while (reader.Read())
{
    Player.Add(new PlayerList
    {
        Nickname     = reader.GetString("User_Nickname"),
        GamePin      = reader.GetInt32("GamePin"),
        korrekteFagen = reader.GetInt32("CorrectAnswered"),
        alleFragen   = reader.GetInt32("PossibleAnswers"),
        Time         = reader.GetDateTime("saveTime")
    });
}
```

- [ ] **Step 4: Top-3 Sortierung auf `korrekteFagen DESC` umstellen**

Den Top-3-Block ersetzen (beide Blöcke — der doppelte `PlaceOne` Assign ist ein bestehender Bug, wird hier bereinigt):
```csharp
var top3 = Player
    .OrderByDescending(p => p.korrekteFagen)
    .Take(3)
    .Select(p => p.Nickname)
    .ToArray();

PlaceOne   = top3.Length > 0 ? top3[0] : "-";
PlaceTwo   = top3.Length > 1 ? top3[1] : "-";
PlaceThree = top3.Length > 2 ? top3[2] : "-";
```

- [ ] **Step 5: Build**
```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```
Erwartet: 0 Errors.

- [ ] **Step 6: Commit**
```bash
git add Smart2Lose/Helper/Filter.cs Smart2Lose/Pages/1Viewer/FinalResult.cshtml.cs
git commit -m "feat: remove points from leaderboard, sort by CorrectAnswered"
```

---

### Task 5: Punkte aus Views entfernen — `Playground.cshtml` + `FinalResult.cshtml`

**Files:**
- Modify: `Smart2Lose/Pages/1Viewer/Playground.cshtml`
- Modify: `Smart2Lose/Pages/1Viewer/FinalResult.cshtml`

- [ ] **Step 1: Punkte-Zeile aus `.quiz-info` in `Playground.cshtml` entfernen**

Ersetzen:
```cshtml
<div class="quiz-info">
    <p>Frage <strong>@(Model.CurrentOffset + 1)</strong> von <strong>@Model.QuestionCount</strong></p>
    <p>Punkte: <strong>@Model.fp.PlayerPoints</strong></p>
</div>
```
Mit:
```cshtml
<div class="quiz-info">
    <p>Frage <strong>@(Model.CurrentOffset + 1)</strong> von <strong>@Model.QuestionCount</strong></p>
</div>
```

- [ ] **Step 2: Punkte-Spalte aus Tabellen-Header in `FinalResult.cshtml` entfernen**

Ersetzen:
```cshtml
<thead>
    <tr>
        <th>Punkte</th>
        <th>Name</th>
        <th>Statistik</th>
        <th>Erstellt am</th>
    </tr>
</thead>
```
Mit:
```cshtml
<thead>
    <tr>
        <th>Name</th>
        <th>Richtig / Gesamt</th>
        <th>Erstellt am</th>
    </tr>
</thead>
```

- [ ] **Step 3: Punkte-Zelle aus Tabellen-Zeilen in `FinalResult.cshtml` entfernen**

Ersetzen:
```cshtml
<tr>
    <td>@list.Points</td>
    <td>@list.Nickname</td>
    <td>@list.korrekteFagen / @list.alleFragen</td>
    <td>@list.Time.ToString("dd.MM.yyyy HH:mm")</td>
</tr>
```
Mit:
```cshtml
<tr>
    <td>@list.Nickname</td>
    <td>@list.korrekteFagen / @list.alleFragen</td>
    <td>@list.Time.ToString("dd.MM.yyyy HH:mm")</td>
</tr>
```

- [ ] **Step 4: Build**
```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```
Erwartet: 0 Errors.

- [ ] **Step 5: Commit**
```bash
git add Smart2Lose/Pages/1Viewer/Playground.cshtml Smart2Lose/Pages/1Viewer/FinalResult.cshtml
git commit -m "feat: remove points display from Playground and FinalResult views"
```

---

### Task 6: `QuizTyp` aus DB laden und in Session cachen

**Files:**
- Modify: `Smart2Lose/Pages/1Viewer/Playground.cshtml.cs`

- [ ] **Step 1: Property und Session-Key-Konstanten hinzufügen**

Direkt nach den bestehenden Konstanten `QStatesKey` und `QStatesGameKey` einfügen:
```csharp
public string QuizTyp { get; set; } = "MC";
private const string QuizTypKey     = "QuizTyp";
private const string QuizTypGameKey = "QuizTypGameId";
```

- [ ] **Step 2: `LadeQuizTyp()` Methode hinzufügen**

Neue private Methode zur Klasse `PlaygroundModel` hinzufügen:
```csharp
private void LadeQuizTyp()
{
    var storedGameId = HttpContext.Session.GetInt32(QuizTypGameKey) ?? 0;
    if (storedGameId == sd.GameID)
    {
        QuizTyp = HttpContext.Session.GetString(QuizTypKey) ?? "MC";
        return;
    }

    var db = new SQLconnection.DatenbankZugriff();
    using var connection = db.GetConnection();
    connection.Open();
    using var cmd = new MySqlCommand(
        "SELECT Typ FROM Fragebogen WHERE Join_ID = @id;", connection);
    cmd.Parameters.AddWithValue("@id", sd.GameID);
    var result = cmd.ExecuteScalar();
    QuizTyp = result?.ToString() ?? "MC";

    HttpContext.Session.SetString(QuizTypKey, QuizTyp);
    HttpContext.Session.SetInt32(QuizTypGameKey, sd.GameID);
}
```

- [ ] **Step 3: `LadeQuizTyp()` in allen Handlern aufrufen**

In `OnGet()`, direkt nach `loadHTTP();`:
```csharp
LadeQuizTyp();
```

In `OnPostCheckAnswer()`, direkt nach `loadHTTP();`:
```csharp
LadeQuizTyp();
```

In `OnPostNextQuestion()`, direkt nach `loadHTTP();`:
```csharp
LadeQuizTyp();
```

In `OnPostFinishQuiz()`, direkt nach `loadHTTP();`:
```csharp
LadeQuizTyp();
```

- [ ] **Step 4: Build**
```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```
Erwartet: 0 Errors.

- [ ] **Step 5: Commit**
```bash
git add Smart2Lose/Pages/1Viewer/Playground.cshtml.cs
git commit -m "feat: load and cache QuizTyp from DB in Playground session"
```

---

### Task 7: Playground — TrueFalse Rendering

**Files:**
- Modify: `Smart2Lose/Pages/1Viewer/Playground.cshtml`
- Modify: `Smart2Lose/wwwroot/css/Playground/Playground.css`

- [ ] **Step 1: TrueFalse CSS an `Playground.css` anhängen**

Am Ende von `Smart2Lose/wwwroot/css/Playground/Playground.css` einfügen:
```css
/* ─── Wahr/Falsch ────────────────────────────────────── */
.tf-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
    margin-top: 28px;
}

.tf-karte {
    padding: 24px 16px;
    border-radius: var(--radius-sm);
    cursor: pointer;
    border: 2px solid transparent;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    font-family: var(--font-body);
    font-weight: 800;
    font-size: 1.1rem;
    min-height: 80px;
    transition: transform 0.2s ease, box-shadow 0.2s ease;
    user-select: none;
}

.tf-karte:hover { transform: translateY(-2px); box-shadow: 0 6px 24px rgba(0,0,0,0.1); }

.tf-wahr  { background: #ECFDF5; border-color: #A7F3D0; color: #065F46; }
.tf-falsch { background: #FFF1F2; border-color: #FECDD3; color: #9F1239; }

.tf-karte.selected { transform: scale(1.03); border-width: 3px; }
.tf-wahr.selected  { border-color: #059669; box-shadow: 0 0 0 3px rgba(5,150,105,0.2); }
.tf-falsch.selected { border-color: #DC2626; box-shadow: 0 0 0 3px rgba(220,38,38,0.2); }

.tf-karte.correct-answer { background: rgba(4,198,138,0.15) !important; border-color: var(--accent) !important; }
.tf-karte.wrong-answer   { background: rgba(217,48,37,0.10) !important; border-color: var(--error) !important; }

@media (max-width: 600px) { .tf-grid { grid-template-columns: 1fr; } }
```

- [ ] **Step 2: `antwort-grid` in `Playground.cshtml` durch typ-konditionelles Rendering ersetzen**

Den gesamten `<div class="antwort-grid">` Block (4 `.antwort-card` divs) innerhalb von `@if (Model.FragenDB.Any())` ersetzen mit:

```cshtml
@if (Model.QuizTyp == "TrueFalse")
{
    <div class="tf-grid">
        <div class="tf-karte tf-wahr @(Model.fp.AnswerChecked
                ? (frage.IstAntwort1Richtig ? "correct-answer"
                : (Model.UserAnswer.IstAntwort1Richtig ? "wrong-answer" : ""))
                : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectTf(this, 'tf1')")">
            <input type="checkbox" id="tf1" class="checkmark"
                   asp-for="UserAnswer.IstAntwort1Richtig"
                   onchange="updateSelectionTf(this)"
                   disabled="@Model.fp.AnswerChecked" />
            ✓ Wahr
        </div>
        <div class="tf-karte tf-falsch @(Model.fp.AnswerChecked
                ? (frage.IstAntwort2Richtig ? "correct-answer"
                : (Model.UserAnswer.IstAntwort2Richtig ? "wrong-answer" : ""))
                : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectTf(this, 'tf2')")">
            <input type="checkbox" id="tf2" class="checkmark"
                   asp-for="UserAnswer.IstAntwort2Richtig"
                   onchange="updateSelectionTf(this)"
                   disabled="@Model.fp.AnswerChecked" />
            ✗ Falsch
        </div>
    </div>
}
else
{
    <div class="antwort-grid">
        <div class="antwort-card bg-1 @(Model.fp.AnswerChecked
                                 ? (frage.IstAntwort1Richtig ? "correct-answer"
                                 : (Model.UserAnswer.IstAntwort1Richtig ? "wrong-answer" : ""))
                                 : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectAnswer(this)")">
            <input type="checkbox" class="checkmark"
                   asp-for="UserAnswer.IstAntwort1Richtig"
                   onchange="updateSelection(this)"
                   disabled="@Model.fp.AnswerChecked" />
            <label>@frage.Antwort1</label>
        </div>
        <div class="antwort-card bg-2 @(Model.fp.AnswerChecked
                             ? (frage.IstAntwort2Richtig ? "correct-answer"
                             : (Model.UserAnswer.IstAntwort2Richtig ? "wrong-answer" : ""))
                             : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectAnswer(this)")">
            <input type="checkbox" class="checkmark"
                   asp-for="UserAnswer.IstAntwort2Richtig"
                   onchange="updateSelection(this)"
                   disabled="@Model.fp.AnswerChecked" />
            <label>@frage.Antwort2</label>
        </div>
        <div class="antwort-card bg-3 @(Model.fp.AnswerChecked
                             ? (frage.IstAntwort3Richtig ? "correct-answer"
                             : (Model.UserAnswer.IstAntwort3Richtig ? "wrong-answer" : ""))
                             : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectAnswer(this)")">
            <input type="checkbox" class="checkmark"
                   asp-for="UserAnswer.IstAntwort3Richtig"
                   onchange="updateSelection(this)"
                   disabled="@Model.fp.AnswerChecked" />
            <label>@frage.Antwort3</label>
        </div>
        <div class="antwort-card bg-4 @(Model.fp.AnswerChecked
                             ? (frage.IstAntwort4Richtig ? "correct-answer"
                             : (Model.UserAnswer.IstAntwort4Richtig ? "wrong-answer" : ""))
                             : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectAnswer(this)")">
            <input type="checkbox" class="checkmark"
                   asp-for="UserAnswer.IstAntwort4Richtig"
                   onchange="updateSelection(this)"
                   disabled="@Model.fp.AnswerChecked" />
            <label>@frage.Antwort4</label>
        </div>
    </div>
}
```

- [ ] **Step 3: TrueFalse JS-Funktionen zum `<script>` Block in `Playground.cshtml` hinzufügen**

Nach den bestehenden Funktionen `selectAnswer` und `updateSelection` einfügen:
```javascript
function selectTf(cardElement, checkboxId) {
    document.querySelectorAll('.tf-karte').forEach(c => c.classList.remove('selected'));
    document.querySelectorAll('.checkmark').forEach(cb => cb.checked = false);
    cardElement.classList.add('selected');
    document.getElementById(checkboxId).checked = true;
}

function updateSelectionTf(checkbox) {
    const card = checkbox.closest('.tf-karte');
    if (checkbox.checked) {
        document.querySelectorAll('.checkmark').forEach(cb => {
            if (cb !== checkbox) cb.checked = false;
        });
        document.querySelectorAll('.tf-karte').forEach(c => {
            if (c !== card) c.classList.remove('selected');
        });
        card.classList.add('selected');
    } else {
        card.classList.remove('selected');
    }
}
```

- [ ] **Step 4: Verify TrueFalse nutzt bestehenden `OnPostCheckAnswer()` ohne Änderung**

TrueFalse speichert Wahr als `IstAntwort1Richtig=true`, Falsch als `IstAntwort2Richtig=true`. Die bestehende Prüfung vergleicht alle 4 Boolean-Felder — funktioniert korrekt für TrueFalse, da Antwort3/4 in DB und UserAnswer beide `false` sind. Keine Code-Änderung nötig.

- [ ] **Step 5: Build**
```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```
Erwartet: 0 Errors.

- [ ] **Step 6: Commit**
```bash
git add Smart2Lose/Pages/1Viewer/Playground.cshtml Smart2Lose/wwwroot/css/Playground/Playground.css
git commit -m "feat: add TrueFalse rendering to Playground"
```

---

### Task 8: Playground — Passwort Rendering und Answer-Check-Logik

**Files:**
- Modify: `Smart2Lose/Pages/1Viewer/Playground.cshtml.cs`
- Modify: `Smart2Lose/Pages/1Viewer/Playground.cshtml`
- Modify: `Smart2Lose/wwwroot/css/Playground/Playground.css`

- [ ] **Step 1: `QuestionState` um `SelectedAnswerText` erweitern**

In `Playground.cshtml.cs` die Klasse `QuestionState` ersetzen:
```csharp
public class QuestionState
{
    public bool Correct { get; set; }
    public int SelectedAnswer { get; set; }       // 1–4 für MC/TrueFalse
    public string SelectedAnswerText { get; set; } = ""; // Passwort-Eingabe
}
```

- [ ] **Step 2: `ReviewPasswordAnswer` Property zu `PlaygroundModel` hinzufügen**

Direkt nach `public string ErrorMessage ...`:
```csharp
public string ReviewPasswordAnswer { get; set; } = "";
```

- [ ] **Step 3: Review-Modus in `OnGet()` für Passwort erweitern**

Den State-Review-Block ersetzen:
```csharp
var state = AllQuestionStates[currentOffset];
if (state != null)
{
    fp.AnswerChecked = true;
    fp.AnswerCorrect = state.Correct;
    if (QuizTyp == "Passwort")
    {
        ReviewPasswordAnswer = state.SelectedAnswerText;
    }
    else
    {
        UserAnswer = new Fragen
        {
            IstAntwort1Richtig = state.SelectedAnswer == 1,
            IstAntwort2Richtig = state.SelectedAnswer == 2,
            IstAntwort3Richtig = state.SelectedAnswer == 3,
            IstAntwort4Richtig = state.SelectedAnswer == 4,
        };
    }
    IsReview = true;
}
```

- [ ] **Step 4: Passwort-Logik in `OnPostCheckAnswer()` einfügen**

Den `isCorrect`-Berechnungsblock ersetzen. Vorher:
```csharp
var currentQuestion = FragenDB[0];

bool isCorrect = UserAnswer.IstAntwort1Richtig == currentQuestion.IstAntwort1Richtig &&
                 UserAnswer.IstAntwort2Richtig == currentQuestion.IstAntwort2Richtig &&
                 UserAnswer.IstAntwort3Richtig == currentQuestion.IstAntwort3Richtig &&
                 UserAnswer.IstAntwort4Richtig == currentQuestion.IstAntwort4Richtig;
```
Nachher:
```csharp
var currentQuestion = FragenDB[0];
bool isCorrect;
string passwordInput = "";

if (QuizTyp == "Passwort")
{
    passwordInput = Request.Form["PasswordInput"].ToString().Trim();
    isCorrect = string.Equals(
        passwordInput,
        currentQuestion.Antwort1.Trim(),
        StringComparison.OrdinalIgnoreCase);
}
else
{
    isCorrect = UserAnswer.IstAntwort1Richtig == currentQuestion.IstAntwort1Richtig &&
                UserAnswer.IstAntwort2Richtig == currentQuestion.IstAntwort2Richtig &&
                UserAnswer.IstAntwort3Richtig == currentQuestion.IstAntwort3Richtig &&
                UserAnswer.IstAntwort4Richtig == currentQuestion.IstAntwort4Richtig;
}
```

Den `AllQuestionStates[CurrentOffset]`-Assign ersetzen:
```csharp
int selectedAnswer = 0;
if (QuizTyp != "Passwort")
{
    selectedAnswer = UserAnswer.IstAntwort1Richtig ? 1 :
                     UserAnswer.IstAntwort2Richtig ? 2 :
                     UserAnswer.IstAntwort3Richtig ? 3 :
                     UserAnswer.IstAntwort4Richtig ? 4 : 0;
}

AllQuestionStates[CurrentOffset] = new QuestionState
{
    Correct            = isCorrect,
    SelectedAnswer     = selectedAnswer,
    SelectedAnswerText = QuizTyp == "Passwort" ? passwordInput : ""
};
```

- [ ] **Step 5: Passwort CSS an `Playground.css` anhängen**

Am Ende von `Smart2Lose/wwwroot/css/Playground/Playground.css`:
```css
/* ─── Passwort-Eingabe ───────────────────────────────── */
.pw-eingabe-wrapper {
    margin-top: 28px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 12px;
}

.pw-eingabe {
    width: 100%;
    max-width: 420px;
    padding: 14px 18px;
    border: 2px solid var(--border);
    border-radius: var(--radius-sm);
    font-family: var(--font-body);
    font-size: 1rem;
    font-weight: 500;
    color: var(--text);
    background: #fff;
    outline: none;
    transition: border-color 0.2s, box-shadow 0.2s;
    text-align: center;
}

.pw-eingabe:focus {
    border-color: var(--accent);
    box-shadow: 0 0 0 3px rgba(4,198,138,0.2);
}

.pw-hint { font-size: 0.75rem; color: var(--muted); }

.pw-review {
    width: 100%;
    max-width: 420px;
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.pw-review-zeile {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 14px;
    border-radius: var(--radius-sm);
    font-size: 0.9rem;
    font-weight: 600;
}

.pw-review-label {
    font-size: 0.7rem;
    font-weight: 700;
    color: var(--muted);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    min-width: 90px;
}

.pw-user-zeile  { background: rgba(217,48,37,0.06); border: 1px solid rgba(217,48,37,0.2); }
.pw-user-zeile.richtig { background: rgba(4,198,138,0.08); border-color: rgba(4,198,138,0.3); }
.pw-richtig-zeile { background: rgba(4,198,138,0.08); border: 1px solid rgba(4,198,138,0.3); }
```

- [ ] **Step 6: Passwort UI in `Playground.cshtml` einfügen**

Den äußeren Typ-Block aus Task 7 (`@if (Model.QuizTyp == "TrueFalse") ... else { ... }`) erweitern, sodass Passwort zuerst geprüft wird:

```cshtml
@if (Model.QuizTyp == "Passwort")
{
    <div class="pw-eingabe-wrapper">
        @if (!Model.fp.AnswerChecked)
        {
            <input type="text"
                   name="PasswordInput"
                   class="pw-eingabe"
                   placeholder="Antwort eingeben..."
                   autocomplete="off" />
            <span class="pw-hint">Groß-/Kleinschreibung wird ignoriert</span>
        }
        else
        {
            <div class="pw-review">
                <div class="pw-review-zeile pw-user-zeile @(Model.fp.AnswerCorrect ? "richtig" : "")">
                    <span class="pw-review-label">Deine Antwort</span>
                    <span>@(string.IsNullOrEmpty(Model.ReviewPasswordAnswer) ? "–" : Model.ReviewPasswordAnswer)</span>
                </div>
                <div class="pw-review-zeile pw-richtig-zeile">
                    <span class="pw-review-label">Richtig wäre</span>
                    <span>@frage.Antwort1</span>
                </div>
            </div>
        }
    </div>
}
else if (Model.QuizTyp == "TrueFalse")
{
    @* TrueFalse Block aus Task 7, Step 2 — identisch übernehmen *@
    <div class="tf-grid">
        <div class="tf-karte tf-wahr @(Model.fp.AnswerChecked
                ? (frage.IstAntwort1Richtig ? "correct-answer"
                : (Model.UserAnswer.IstAntwort1Richtig ? "wrong-answer" : ""))
                : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectTf(this, 'tf1')")">
            <input type="checkbox" id="tf1" class="checkmark"
                   asp-for="UserAnswer.IstAntwort1Richtig"
                   onchange="updateSelectionTf(this)"
                   disabled="@Model.fp.AnswerChecked" />
            ✓ Wahr
        </div>
        <div class="tf-karte tf-falsch @(Model.fp.AnswerChecked
                ? (frage.IstAntwort2Richtig ? "correct-answer"
                : (Model.UserAnswer.IstAntwort2Richtig ? "wrong-answer" : ""))
                : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectTf(this, 'tf2')")">
            <input type="checkbox" id="tf2" class="checkmark"
                   asp-for="UserAnswer.IstAntwort2Richtig"
                   onchange="updateSelectionTf(this)"
                   disabled="@Model.fp.AnswerChecked" />
            ✗ Falsch
        </div>
    </div>
}
else
{
    @* MC Block — identisch wie aktuell *@
    <div class="antwort-grid">
        <div class="antwort-card bg-1 @(Model.fp.AnswerChecked
                                 ? (frage.IstAntwort1Richtig ? "correct-answer"
                                 : (Model.UserAnswer.IstAntwort1Richtig ? "wrong-answer" : ""))
                                 : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectAnswer(this)")">
            <input type="checkbox" class="checkmark"
                   asp-for="UserAnswer.IstAntwort1Richtig"
                   onchange="updateSelection(this)"
                   disabled="@Model.fp.AnswerChecked" />
            <label>@frage.Antwort1</label>
        </div>
        <div class="antwort-card bg-2 @(Model.fp.AnswerChecked
                             ? (frage.IstAntwort2Richtig ? "correct-answer"
                             : (Model.UserAnswer.IstAntwort2Richtig ? "wrong-answer" : ""))
                             : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectAnswer(this)")">
            <input type="checkbox" class="checkmark"
                   asp-for="UserAnswer.IstAntwort2Richtig"
                   onchange="updateSelection(this)"
                   disabled="@Model.fp.AnswerChecked" />
            <label>@frage.Antwort2</label>
        </div>
        <div class="antwort-card bg-3 @(Model.fp.AnswerChecked
                             ? (frage.IstAntwort3Richtig ? "correct-answer"
                             : (Model.UserAnswer.IstAntwort3Richtig ? "wrong-answer" : ""))
                             : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectAnswer(this)")">
            <input type="checkbox" class="checkmark"
                   asp-for="UserAnswer.IstAntwort3Richtig"
                   onchange="updateSelection(this)"
                   disabled="@Model.fp.AnswerChecked" />
            <label>@frage.Antwort3</label>
        </div>
        <div class="antwort-card bg-4 @(Model.fp.AnswerChecked
                             ? (frage.IstAntwort4Richtig ? "correct-answer"
                             : (Model.UserAnswer.IstAntwort4Richtig ? "wrong-answer" : ""))
                             : "")"
             onclick="@(Model.fp.AnswerChecked ? "" : "selectAnswer(this)")">
            <input type="checkbox" class="checkmark"
                   asp-for="UserAnswer.IstAntwort4Richtig"
                   onchange="updateSelection(this)"
                   disabled="@Model.fp.AnswerChecked" />
            <label>@frage.Antwort4</label>
        </div>
    </div>
}
```

**Hinweis:** Den vollständigen bestehenden `<div class="antwort-grid">` Block aus `Playground.cshtml` (alle 4 `.antwort-card` Divs) vollständig durch den obigen Block ersetzen. Nichts vom alten Block soll verbleiben.

- [ ] **Step 7: Build**
```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```
Erwartet: 0 Errors.

- [ ] **Step 8: Commit**
```bash
git add Smart2Lose/Pages/1Viewer/Playground.cshtml Smart2Lose/Pages/1Viewer/Playground.cshtml.cs Smart2Lose/wwwroot/css/Playground/Playground.css
git commit -m "feat: add Passwort quiz type with case-insensitive answer check and review mode"
```

---

### Task 9: `FrageboegenErstellen` — Typ-Selector UI + Server-Logik

**Files:**
- Modify: `Smart2Lose/Pages/Admin/FrageboegenErstellen.cshtml`
- Modify: `Smart2Lose/Pages/Admin/FrageboegenErstellen.cshtml.cs`

- [ ] **Step 1: CSS-Styles für Typ-Selector einfügen**

Direkt nach den `<link>`-Tags oben in `FrageboegenErstellen.cshtml` (vor dem `<header>`):
```html
<style>
.typ-selector {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;
    margin-bottom: 24px;
}
.typ-karte-erstellen {
    background: #fff;
    border: 2px solid #DDE3DF;
    border-radius: 10px;
    padding: 16px 12px;
    text-align: center;
    cursor: pointer;
    transition: border-color 0.2s, background 0.2s;
}
.typ-karte-erstellen.aktiv {
    border-color: #024651;
    background: rgba(2,70,81,0.05);
}
.typ-karte-erstellen .typ-icon { font-size: 1.6rem; display: block; margin-bottom: 6px; }
.typ-karte-erstellen .typ-name { font-size: 0.85rem; font-weight: 700; color: #024651; }
.typ-karte-erstellen .typ-desc { font-size: 0.72rem; color: #6E7D78; margin-top: 2px; }
.tf-felder, .pw-felder { margin-top: 12px; }
</style>
```

- [ ] **Step 2: Typ-Selector HTML nach `<h2>Neues Quiz</h2>` einfügen**

Nach `<h2>Neues Quiz</h2>` (Zeile 80) und **innerhalb** des `<form>` Tags (also nach Zeile 82 `<form method="post" ...>`), direkt vor `<!-- META -->` einfügen:

```cshtml
<input type="hidden" name="fb.Typ" id="selectedTyp" value="MC" />

<div class="typ-selector">
    <div class="typ-karte-erstellen aktiv" onclick="setTyp('MC', this)">
        <span class="typ-icon">🔲</span>
        <div class="typ-name">Multiple Choice</div>
        <div class="typ-desc">4 Antwortmöglichkeiten</div>
    </div>
    <div class="typ-karte-erstellen" onclick="setTyp('TrueFalse', this)">
        <span class="typ-icon">✅</span>
        <div class="typ-name">Wahr / Falsch</div>
        <div class="typ-desc">True oder False</div>
    </div>
    <div class="typ-karte-erstellen" onclick="setTyp('Passwort', this)">
        <span class="typ-icon">🔑</span>
        <div class="typ-name">Passwort</div>
        <div class="typ-desc">Texteingabe als Antwort</div>
    </div>
</div>
```

- [ ] **Step 3: MC-Felder in `.mc-felder` wrappen und TF/PW-Blöcke einfügen**

In `FrageboegenErstellen.cshtml` den Block `<h3>Antwortoptionen</h3>` und das `<div class="antwort-grid">` (Zeilen 141–165) ersetzen mit:

```cshtml
<h3>Antwortoptionen</h3>

<div class="mc-felder">
    <div class="antwort-grid">
        @for (int i = 1; i <= 4; i++)
        {
            <div class="antwort-option">
                <textarea name="fb.Fragen[0].Antwort@(i)"
                          placeholder="Antwort @i"></textarea>
                <label>
                    <input type="checkbox"
                           name="fb.Fragen[0].IstAntwort@(i)Richtig"
                           value="true" />
                    Richtig
                </label>
                <input type="hidden"
                       name="fb.Fragen[0].IstAntwort@(i)Richtig"
                       value="false" />
            </div>
        }
    </div>
</div>

<div class="tf-felder" style="display:none;">
    <p style="font-size:0.85rem;color:#6E7D78;margin-bottom:10px;">Welche Antwort ist korrekt?</p>
    <div style="display:flex;gap:20px;">
        <label style="display:flex;align-items:center;gap:8px;cursor:pointer;font-weight:600;">
            <input type="radio" name="tf_richtig_0" value="1"
                   onchange="setTfRichtig(0, true, false)" />
            ✓ Wahr ist richtig
        </label>
        <label style="display:flex;align-items:center;gap:8px;cursor:pointer;font-weight:600;">
            <input type="radio" name="tf_richtig_0" value="2"
                   onchange="setTfRichtig(0, false, true)" />
            ✗ Falsch ist richtig
        </label>
    </div>
    <input type="hidden" id="tf_r1_0" name="tf_istAntwort1Richtig_0" value="true" />
    <input type="hidden" id="tf_r2_0" name="tf_istAntwort2Richtig_0" value="false" />
</div>

<div class="pw-felder" style="display:none;">
    <label style="font-size:0.85rem;font-weight:700;color:#6E7D78;">Richtige Antwort</label>
    <input type="text"
           class="pw-erstellen-input"
           name="pw_antwort_0"
           placeholder="Korrekte Antwort eingeben"
           style="display:block;width:100%;margin-top:6px;padding:10px 14px;border:1.5px solid #DDE3DF;border-radius:8px;font-size:0.95rem;" />
</div>
```

**Wichtig:** Die TF-Felder verwenden separate Input-Namen (`tf_istAntwort1Richtig_0`, `pw_antwort_0`) die nicht mit dem MC Model-Binding kollidieren. Die Serverseite normalisiert die Werte (Task 9 Step 5).

- [ ] **Step 4: JS-Funktionen für Typ-Selector und `addFrage()` Update**

Den gesamten `<script>` Block am Ende der Datei ersetzen:

```javascript
<script>
    let frageIndex = 1;

    function setTyp(typ, clickedEl) {
        document.getElementById('selectedTyp').value = typ;
        document.querySelectorAll('.typ-karte-erstellen').forEach(k => k.classList.remove('aktiv'));
        clickedEl.classList.add('aktiv');
        applyTypToFragen(typ);
    }

    function applyTypToFragen(typ) {
        document.querySelectorAll('.quiz-frage').forEach(function(frage) {
            const mc = frage.querySelector('.mc-felder');
            const tf = frage.querySelector('.tf-felder');
            const pw = frage.querySelector('.pw-felder');
            if (mc) mc.style.display = typ === 'MC'        ? '' : 'none';
            if (tf) tf.style.display = typ === 'TrueFalse' ? '' : 'none';
            if (pw) pw.style.display = typ === 'Passwort'  ? '' : 'none';
        });
    }

    function setTfRichtig(i, wahr, falsch) {
        const r1 = document.getElementById('tf_r1_' + i);
        const r2 = document.getElementById('tf_r2_' + i);
        if (r1) r1.value = wahr  ? 'true' : 'false';
        if (r2) r2.value = falsch ? 'true' : 'false';
    }

    function addFrage() {
        const container = document.getElementById("fragenContainer");
        const last = container.querySelector(".quiz-frage:last-child");
        const neu = last.cloneNode(true);

        neu.querySelector("h3").textContent = `Frage ${frageIndex + 1}`;

        // Fragestellung
        const frage = neu.querySelector('textarea[name*="Fragestellung"]');
        frage.name = `fb.Fragen[${frageIndex}].Fragestellung`;
        frage.value = "";

        // Optionale Felder
        const bildInput = neu.querySelector('input[type="file"][name^="bild_"]');
        if (bildInput) { bildInput.name = `bild_${frageIndex}`; bildInput.value = ""; }
        const linkUrl = neu.querySelector('input[name*="LinkUrl"]');
        if (linkUrl) { linkUrl.name = `fb.Fragen[${frageIndex}].LinkUrl`; linkUrl.value = ""; }

        // MC: Antworten + Booleans umbenennen
        for (let i = 1; i <= 4; i++) {
            const ta = neu.querySelector(`textarea[name*="Antwort${i}"]`);
            if (ta) { ta.name = `fb.Fragen[${frageIndex}].Antwort${i}`; ta.value = ""; }
            const inputs = neu.querySelectorAll(`input[name*="IstAntwort${i}Richtig"]`);
            inputs.forEach(x => {
                x.name = `fb.Fragen[${frageIndex}].IstAntwort${i}Richtig`;
                if (x.type === "checkbox") x.checked = false;
                if (x.type === "hidden")   x.value   = "false";
            });
        }

        // TrueFalse: Radio-Inputs umbenennen
        neu.querySelectorAll('.tf-felder input[type="radio"]').forEach(r => {
            r.name = `tf_richtig_${frageIndex}`;
            r.checked = false;
            const isWahr = r.value === '1';
            r.setAttribute('onchange',
                `setTfRichtig(${frageIndex}, ${isWahr}, ${!isWahr})`);
        });
        const tfR1 = neu.querySelector('.tf-felder [id^="tf_r1_"]');
        const tfR2 = neu.querySelector('.tf-felder [id^="tf_r2_"]');
        if (tfR1) { tfR1.id = `tf_r1_${frageIndex}`; tfR1.name = `tf_istAntwort1Richtig_${frageIndex}`; tfR1.value = 'true'; }
        if (tfR2) { tfR2.id = `tf_r2_${frageIndex}`; tfR2.name = `tf_istAntwort2Richtig_${frageIndex}`; tfR2.value = 'false'; }

        // Passwort: Input umbenennen
        const pwInput = neu.querySelector('.pw-felder input[type="text"]');
        if (pwInput) { pwInput.name = `pw_antwort_${frageIndex}`; pwInput.value = ''; }

        applyTypToFragen(document.getElementById('selectedTyp').value);
        container.appendChild(neu);
        frageIndex++;
    }

    // Radio-Verhalten für MC-Checkboxen
    document.addEventListener("change", function (e) {
        if (e.target.type === "checkbox" && e.target.name.includes("IstAntwort")) {
            const container = e.target.closest(".quiz-frage");
            const boxes = container.querySelectorAll('input[type="checkbox"][name*="IstAntwort"]');
            boxes.forEach(b => { if (b !== e.target) b.checked = false; });
        }
    });

    // Client-seitige Validierung
    document.getElementById("fragenForm").addEventListener("submit", function (e) {
        const typ = document.getElementById('selectedTyp').value;
        const fragen = document.querySelectorAll(".quiz-frage");

        for (let i = 0; i < fragen.length; i++) {
            if (typ === 'MC') {
                const boxes = fragen[i].querySelectorAll('input[type="checkbox"][name*="IstAntwort"]');
                let checked = false;
                boxes.forEach(b => { if (b.checked) checked = true; });
                if (!checked) {
                    alert(`Frage ${i + 1}: Bitte eine richtige Antwort wählen.`);
                    e.preventDefault();
                    return;
                }
            } else if (typ === 'TrueFalse') {
                const radios = fragen[i].querySelectorAll('.tf-felder input[type="radio"]');
                let checked = false;
                radios.forEach(r => { if (r.checked) checked = true; });
                if (!checked) {
                    alert(`Frage ${i + 1}: Bitte Wahr oder Falsch als richtige Antwort wählen.`);
                    e.preventDefault();
                    return;
                }
            } else if (typ === 'Passwort') {
                const pwInput = fragen[i].querySelector('.pw-felder input[type="text"]');
                if (!pwInput || !pwInput.value.trim()) {
                    alert(`Frage ${i + 1}: Bitte eine Antwort eingeben.`);
                    e.preventDefault();
                    return;
                }
            }
        }
    });

    document.addEventListener('DOMContentLoaded', function() {
        applyTypToFragen(document.getElementById('selectedTyp').value);
    });
</script>
```

- [ ] **Step 5: Server-Normalisierung in `OnPostSpeichern()` — Typ-Felder lesen und normalisieren**

In `FrageboegenErstellen.cshtml.cs` direkt **nach** dem `if (fb.JoinId <= 0)` Block und **vor** dem Titel-Check, einfügen:

```csharp
// TrueFalse und Passwort: Fragen-Daten aus separaten Form-Feldern lesen und normalisieren
if (fb.Typ == "TrueFalse")
{
    for (int i = 0; i < (fb.Fragen?.Count ?? 0); i++)
    {
        fb.Fragen[i].Antwort1 = "Wahr";
        fb.Fragen[i].Antwort2 = "Falsch";
        fb.Fragen[i].Antwort3 = "";
        fb.Fragen[i].Antwort4 = "";
        bool wahrIstRichtig = Request.Form[$"tf_istAntwort1Richtig_{i}"] == "true";
        fb.Fragen[i].IstAntwort1Richtig = wahrIstRichtig;
        fb.Fragen[i].IstAntwort2Richtig = !wahrIstRichtig;
        fb.Fragen[i].IstAntwort3Richtig = false;
        fb.Fragen[i].IstAntwort4Richtig = false;
    }
}
else if (fb.Typ == "Passwort")
{
    for (int i = 0; i < (fb.Fragen?.Count ?? 0); i++)
    {
        fb.Fragen[i].Antwort1 = Request.Form[$"pw_antwort_{i}"].ToString().Trim();
        fb.Fragen[i].Antwort2 = "";
        fb.Fragen[i].Antwort3 = "";
        fb.Fragen[i].Antwort4 = "";
        fb.Fragen[i].IstAntwort1Richtig = true;
        fb.Fragen[i].IstAntwort2Richtig = false;
        fb.Fragen[i].IstAntwort3Richtig = false;
        fb.Fragen[i].IstAntwort4Richtig = false;
    }
}
```

- [ ] **Step 6: Validierungsschleife in `OnPostSpeichern()` typ-abhängig machen**

Den bestehenden Validierungs-For-Loop ersetzen:
```csharp
for (int i = 0; i < fb.Fragen.Count; i++)
{
    var f = fb.Fragen[i];

    if (string.IsNullOrWhiteSpace(f.Fragestellung))
    {
        FragenError = $"Frage {i + 1}: Fragestellung darf nicht leer sein.";
        return Page();
    }

    if (fb.Typ == "Passwort")
    {
        if (string.IsNullOrWhiteSpace(f.Antwort1))
        {
            FragenError = $"Frage {i + 1}: Richtige Antwort darf nicht leer sein.";
            return Page();
        }
    }
    else
    {
        int richtig =
            (f.IstAntwort1Richtig ? 1 : 0) +
            (f.IstAntwort2Richtig ? 1 : 0) +
            (f.IstAntwort3Richtig ? 1 : 0) +
            (f.IstAntwort4Richtig ? 1 : 0);

        if (richtig != 1)
        {
            FragenError = $"Frage {i + 1}: Bitte genau eine richtige Antwort markieren.";
            return Page();
        }
    }
}
```

- [ ] **Step 7: INSERT in `OnPostSpeichern()` — `Typ` Spalte hinzufügen**

Den Fragebogen-INSERT ersetzen:
```csharp
using (var cmd = new MySqlCommand(
    @"INSERT INTO Fragebogen (Titel, Join_ID, Autor, Kategorie, Typ)
      VALUES (@t, @j, @a, @k, @typ);",
    con, tx))
{
    cmd.Parameters.AddWithValue("@t",   fb.Titel);
    cmd.Parameters.AddWithValue("@j",   fb.JoinId);
    cmd.Parameters.AddWithValue("@a",   fb.Autor);
    cmd.Parameters.AddWithValue("@k",   fb.Kategorie);
    cmd.Parameters.AddWithValue("@typ", fb.Typ ?? "MC");
    cmd.ExecuteNonQuery();
    fid = cmd.LastInsertedId;
}
```

- [ ] **Step 8: Build**
```bash
dotnet build Smart2Lose/Smart2Lose.csproj
```
Erwartet: 0 Errors.

- [ ] **Step 9: End-to-End manuell testen**

App starten:
```bash
dotnet run --project Smart2Lose/Smart2Lose.csproj
```

**MC (bestehend):**
- Login als Admin → "Neue anlegen"
- Typ-Selector zeigt MC aktiv, 4 Antwortfelder sichtbar
- TF- und PW-Felder nicht sichtbar
- Quiz speichern, spielen → Playground ohne Punkte, nur "Frage X von N"
- Quiz beenden → FinalResult ohne Punkte-Spalte, sortiert nach Richtig/Gesamt

**TrueFalse:**
- "Neue anlegen" → "Wahr / Falsch" klicken → MC-Felder verschwinden, TF-Radio erscheint
- "Wahr ist richtig" wählen → Frage speichern
- Playground → 2 große Karten (Wahr/Falsch), grün/rot Feedback korrekt
- Review-Modus → Karten zeigen Auswahl und richtiges Ergebnis

**Passwort:**
- "Neue anlegen" → "Passwort" → "Richtige Antwort" Feld erscheint
- Antwort eingeben → speichern
- Playground → Textfeld, case-insensitive Prüfung (z. B. "Berlin" = "berlin")
- Feedback → zeigt eingegebene Antwort vs. richtige Antwort
- Review-Modus → gleiche Ansicht

- [ ] **Step 10: Commit**
```bash
git add Smart2Lose/Pages/Admin/FrageboegenErstellen.cshtml Smart2Lose/Pages/Admin/FrageboegenErstellen.cshtml.cs
git commit -m "feat: add quiz type selector (MC/TrueFalse/Passwort) to FrageboegenErstellen"
```
