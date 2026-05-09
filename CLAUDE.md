# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build Smart2Lose/Smart2Lose.csproj

# Run (dev)
dotnet run --project Smart2Lose/Smart2Lose.csproj

# Publish
dotnet publish Smart2Lose/Smart2Lose.csproj -c Release -o publish_output

# EF Core migrations (Identity tables only)
dotnet ef database update --project Smart2Lose/Smart2Lose.csproj
```

No test suite exists yet (no `dotnet test`).

## Architecture

**2Smart2Lose** is a Kahoot-style quiz web app: players join via PIN, answer questions, see a leaderboard.

**Stack:** ASP.NET Core 8 Razor Pages · ASP.NET Identity · MySQL (`KahootDatabase`) · Bootstrap 5 + jQuery

### Dual database access pattern (critical to understand)

The codebase uses **two separate DB access layers**:
- **EF Core + Pomelo** — only for ASP.NET Identity tables (`aspnetusers`, etc.). Managed via `ApplicationDbContext` and EF migrations.
- **Raw `MySql.Data`** — all game logic tables (`Fragebogen`, `Fragen`, `PlayerPoints`). Accessed through `Helper/SQLconnection.cs`.

The two layers never mix. Do not use EF Core for game tables or raw SQL for Identity tables.

### DB schema (game tables, manual SQL — no EF migrations)

| Table | Key columns |
|---|---|
| `Fragebogen` | `Join_ID`, `Titel`, `Autor` (email), `Kategorie`, `ErstelltAm` |
| `Fragen` | `ID`, `FragebogenID`, `Fragestellung`, `Antwort1-4`, `IstAntwort1-4Richtig` |
| `PlayerPoints` | `User_Nickname`, `SessionPints`, `GamePin`, `CorrectAnswered`, `PossibleAnswers`, `saveTime` |

### Game flow

1. `Index` — player enters PIN → validated against `Fragebogen.Join_ID`
2. `1Viewer/NameConfirmation` — nickname stored in session (`Name` key)
3. `1Viewer/Playground` — questions loaded one at a time via OFFSET pagination; 100 pts correct / −5 pts wrong. Fixed bottom nav bar shows all question numbers; green = correct, red = wrong, grey = not yet answered (locked). Clicking an answered number opens it in read-only review mode showing the player's original answer and the correct answer.
4. `1Viewer/FinalResult` — score written to `PlayerPoints`; leaderboard filtered all-time or last 24 h

Session keys: `GameNumber`, `Name`, `PlayerPoints`, `RightAnswer`, `QStates` (JSON array of `QuestionState?` per question — `null` = unanswered, `{Correct, SelectedAnswer}` = answered), `QStatesGameId` (resets `QStates` when a new game starts)

**Playground review mode:** `PlaygroundModel.IsReview = true` when a player navigates back to an already-answered question via GET. In review mode the controls show "← Weiter zu Frage N" instead of "Antwort Prüfen". `CurrentProgressOffset` always points to the first unanswered question (or `QuestionCount` when all are done).

### Page structure

```
Pages/
  Index          — PIN entry; debug shortcuts: 111=DB check, 2=Erstellen, 3=ManageUser, 123=Register, 6=CreateUser, 7=ManageUsers
  Account/       — Login, Register (auto-role: ReadOnly), CreateUser (Admin only)
  Admin/         — Dashboard, Frageboegen (list/edit/delete), FrageboegenErstellen, ManageUser
  1Viewer/       — NameConfirmation, Playground, FinalResult
```

Roles: `Admin`, `User`, `ReadOnly`. Questionnaire authors (role `User`) can only edit/delete their own quizzes; `Admin` can edit all.

### Startup

`RoleSeeder` seeds the three roles and creates a default admin (`admin@smart2lose.com` / `Admin123!`) on first run. Identity lockout: 5 failed attempts → 5-minute lockout.

## Critical pitfalls

### `SQLconnection.CreateCommand()` is broken — never call it

`DatenbankZugriff.CreateCommand()` closes the connection in its `finally` block before the command can execute. **Always use this pattern instead:**

```csharp
using var connection = DatenbankZugriff.GetConnection();
connection.Open();
var cmd = new MySqlCommand("SELECT ...", connection);
cmd.Parameters.AddWithValue("@param", value);
using var reader = cmd.ExecuteReader();
```

### `fHelper.activeUser` must be set in every handler that returns `Page()`

`FragenHelper.activeUser` is not auto-populated. Missing it causes authorization checks (`CheckIfPlayerIsAutor`) to fail silently. Set it at the top of every `OnGet`/`OnPost` that returns `Page()`:

```csharp
fHelper.activeUser = User.FindFirstValue(ClaimTypes.Email);
```

### Post-merge checklist (merges have repeatedly introduced broken code)

After any GitHub merge, verify:
- Column name is `Join_ID`, not `Join_ID3`
- No SQL strings with missing spaces (e.g. `"SELECTFeld,..."`)
- Parameters added **before** `ExecuteReader()` / `ExecuteNonQuery()`, never after
- `using MySql.Data.MySqlClient`, not `using Mysqlx.Crud`
- Authorization conditions not accidentally inverted

### Frageboegen edit/delete button condition

```cshtml
@if (User.IsInRole("Admin") || (User.IsInRole("User") && Model.fHelper.CheckIfPlayerIsAutor(fragebogen.JoinId)))
```

The parentheses around the `&&` clause are required. `!CheckIfPlayerIsAutor` (inverted) is wrong.

### Answer validation

All four boolean flags (`IstAntwort1Richtig`…`IstAntwort4Richtig`) must match the submitted answer to count as correct. A partial match is scored as wrong.

## Konventionen

- **Domain-Bezeichner auf Deutsch** — Klassen, Tabellen und Felder der Spiellogik folgen deutschen Namen (`Fragebogen`, `Fragen`, `Fragestellung`, `Antwort1`–`4`, `IstAntwort1Richtig`, `SpielDurchlauf`). Neue Entitäten in diesem Bereich ebenfalls deutsch benennen.
- **Kommentare auf Deutsch**
- **Standard-.NET-Namenskonventionen** — PascalCase für Klassen/Methoden/Properties, camelCase für lokale Variablen
- **Einrückung: 4 Spaces** (→ `.editorconfig` im Root)
- **Genau eine richtige Antwort pro Frage** — `FrageboegenErstellen` erzwingt dies; beim manuellen SQL-Insert darauf achten
- **Join_ID** ist immer vierstellig (1000–9999), generiert via `AdminHelper.RandomNum()` mit DB-Eindeutigkeitsprüfung

## Offene TODOs

- [ ] Kein Test-Projekt vorhanden — xUnit oder MSTest ergänzen
- [ ] Connection-String ist hardcoded in `Helper/SQLconnection.cs` und `appsettings.json` — in User Secrets / Umgebungsvariable auslagern
- [ ] `MySql.Data` (deprecated) durch `MySqlConnector` ersetzen (`MySqlConnector` ist bereits als NuGet-Paket vorhanden)
- [ ] `SQLconnection.CreateCommand()` entfernen oder reparieren (wirft aktuell immer einen Fehler, siehe Critical pitfalls)
- [ ] Spiellogik-Tabellen (`Fragebogen`, `Fragen`, `PlayerPoints`) in EF Core migrieren, damit keine zwei parallelen DB-Zugriffsschichten nötig sind

## Zuletzt gearbeitet an

2026-05-08 — Playground: Fragen-Navigationsleiste unten (grün/rot/grau), Read-only-Review für bereits beantwortete Fragen, Session-Reset bei neuem Spiel

## CI/CD

`.github/workflows/dotnet.yml` — builds on push to `main` on a self-hosted Ubuntu runner (restore → build → publish → upload artifact). `deploy.yml` handles deployment.
