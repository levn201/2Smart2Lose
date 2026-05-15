# Workshop Live Dashboard – Design Spec

**Datum:** 2026-05-09  
**Status:** Approved  
**Scope:** Live-Fortschrittsübersicht für Admins während eines laufenden Quiz-Workshops

---

## Ziel

Admins sollen während eines Workshops in Echtzeit sehen können, welche Teilnehmer bei welcher Frage sind, wie viele Punkte sie haben und ob jemand inaktiv ist. Der Zugang erfolgt direkt über die bestehende Frageboegen-Liste per Join_ID – kein separates Session-Konzept.

---

## Nicht im Scope

- Workshop-Sessions starten/beenden
- Hinweise oder Erklärungen pro Frage
- Bonusfragen
- CSV-Export
- Live-Freischaltung von Fragen

---

## Datenbankschema

### Neue Tabelle: `WorkshopTeilnehmer`

```sql
CREATE TABLE WorkshopTeilnehmer (
    GamePin       VARCHAR(10)  NOT NULL,
    Nickname      VARCHAR(100) NOT NULL,
    AktuelleOffset INT         NOT NULL DEFAULT 0,
    QuestionCount  INT         NOT NULL DEFAULT 0,
    Punkte        INT          NOT NULL DEFAULT 0,
    LetztesUpdate DATETIME     NOT NULL,
    PRIMARY KEY (GamePin, Nickname)
);
```

- `GamePin` entspricht `Fragebogen.Join_ID` (vierstellig, als String gespeichert)
- `AktuelleOffset` = Anzahl bereits beantworteter Fragen (= `currentOffset + 1` nach jeder Antwort). Wertebereich 0–QuestionCount. Nicht der SQL-OFFSET, sondern ein Zähler.
- `QuestionCount` wird beim ersten Insert gesetzt (aus `PlaygroundModel.QuestionCount`)
- Keine FK-Constraint auf `Fragebogen` – raw SQL, kein EF Core
- Zeilen bleiben nach dem Spiel erhalten (dienen auch als Historie)

---

## Betroffene Komponenten

### 1. `Playground.cshtml.cs` — OnPost erweitern

Nach jeder Antwortauswertung (am Ende von `OnPost`, bevor `RedirectToPage` oder `Page()` zurückgegeben wird):

```csharp
// Upsert WorkshopTeilnehmer
using var con = DatenbankZugriff.GetConnection();
con.Open();
var cmd = new MySqlCommand(@"
    INSERT INTO WorkshopTeilnehmer (GamePin, Nickname, AktuelleOffset, QuestionCount, Punkte, LetztesUpdate)
    VALUES (@pin, @nick, @offset, @count, @pts, NOW())
    ON DUPLICATE KEY UPDATE
        AktuelleOffset = @offset,
        QuestionCount  = @count,
        Punkte         = @pts,
        LetztesUpdate  = NOW()
", con);
cmd.Parameters.AddWithValue("@pin",    fp.GameNumber);
cmd.Parameters.AddWithValue("@nick",   fp.Name);
cmd.Parameters.AddWithValue("@offset", CurrentOffset + 1);  // +1 = Frage wurde beantwortet
cmd.Parameters.AddWithValue("@count",  QuestionCount);
cmd.Parameters.AddWithValue("@pts",    fp.PlayerPoints);
cmd.ExecuteNonQuery();
```

Kein Try-Catch nötig – Fehler beim Tracking sollen nicht das Spielerlebnis unterbrechen (optional: stilles Catch mit Log).

### 2. `Admin/Frageboegen.cshtml` — Dashboard-Button

Pro Tabellenzeile ein neuer Button neben den bestehenden Edit/Delete-Buttons:

```cshtml
<a asp-page="/Admin/WorkshopDashboard" asp-route-joinId="@fragebogen.JoinId"
   class="btn btn-sm btn-outline-info">📊 Dashboard</a>
```

Nur sichtbar für Admins und den jeweiligen Autor (gleiche Bedingung wie Edit/Delete).

### 3. Neue Seite `Pages/Admin/WorkshopDashboard.cshtml` + `.cshtml.cs`

#### OnGet (initiales Laden)
- Liest `joinId` aus der Route
- Prüft Autorisierung: Admin oder Autor des Fragebogens
- Lädt `Fragebogen`-Titel für den Header
- Lädt initiale `WorkshopTeilnehmer`-Zeilen

#### OnGet mit `?handler=Data` (AJAX-Polling)
- Gibt JSON zurück: Array von `{ Nickname, AktuelleOffset, QuestionCount, Punkte, LetztesUpdate }`
- Wird vom Frontend alle 5 Sekunden aufgerufen

#### SQL für Teilnehmer-Abfrage

```sql
SELECT Nickname, AktuelleOffset, QuestionCount, Punkte, LetztesUpdate
FROM WorkshopTeilnehmer
WHERE GamePin = @pin
ORDER BY AktuelleOffset DESC, Punkte DESC
```

#### UI-Elemente
- Header: Fragebogen-Titel, Join_ID, "Aktualisiert vor Xs"
- Statistik-Karten: Fertig / Aktiv / Inaktiv / Gesamt
- Tabelle pro Teilnehmer:
  - Nickname
  - Fortschrittsbalken (`AktuelleOffset / QuestionCount`)
  - "Frage X von Y"
  - Punkte
  - Status-Badge: **Fertig** (grün, `AktuelleOffset == QuestionCount`), **Aktiv** (blau, `LetztesUpdate` < 2 min), **Inaktiv** (grau, `LetztesUpdate` >= 2 min)
- JS: `setInterval(() => fetchAndUpdate(), 5000)` – aktualisiert nur den Tabelleninhalt, kein Seitenreload

#### Autorisierung
```csharp
[Authorize(Roles = "Admin,User")]
```
Im Handler zusätzlich prüfen: `User`-Rolle darf nur eigene Frageboegen sehen (via `CheckIfPlayerIsAutor`).

---

## Datenfluss

```
Teilnehmer antwortet
  → Playground.OnPost
    → Punkte berechnen
    → WorkshopTeilnehmer UPSERT (GamePin, Nickname, Offset, Count, Punkte)
    → RedirectToPage / Page()

Admin-Dashboard (alle 5s)
  → GET /Admin/WorkshopDashboard/{joinId}?handler=Data
    → SELECT aus WorkshopTeilnehmer WHERE GamePin = joinId
    → JSON zurück
    → JS aktualisiert Tabellenzeilen
```

---

## Wichtige Randbedingungen

- `DatenbankZugriff.CreateCommand()` ist kaputt – immer `GetConnection()` + manuelles `MySqlCommand` verwenden (siehe CLAUDE.md)
- `fHelper.activeUser` muss in jedem Handler der `Page()` zurückgibt gesetzt werden
- Autorisierung: `User`-Rolle darf nur eigene Frageboegen im Dashboard sehen
- Bootstrap 5 + jQuery sind bereits eingebunden – kein zusätzliches Framework nötig
- Polling via `fetch()` (vanilla JS), kein SignalR
