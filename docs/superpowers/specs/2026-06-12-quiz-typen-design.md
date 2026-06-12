# Design: Quiz-Typen & Punkte-Entfernung

**Datum:** 2026-06-12
**Status:** Genehmigt

## Überblick

Erweiterung von Smart2Lose um zwei neue Fragetypen (Wahr/Falsch, Passwort) sowie vollständige Entfernung des Punktesystems aus allen Quiz-Typen. Auswertung erfolgt künftig nur noch über Richtig/Gesamt-Verhältnis.

## Entscheidungen

| Frage | Entscheidung |
|---|---|
| Typ-Granularität | Pro Fragebogen (nicht pro Frage) |
| Passwort-Vergleich | Case-insensitive (`OrdinalIgnoreCase`) |
| Tippfehler-Toleranz | Keine — exakter Match (nach Normalisierung) |
| Punkte | Komplett entfernt aus allen Typen |

---

## 1. Datenbankänderungen

### 1.1 `Fragebogen` — neue Spalte `Typ`

```sql
ALTER TABLE Fragebogen
  ADD COLUMN Typ VARCHAR(20) NOT NULL DEFAULT 'MC';
```

Gültige Werte: `'MC'` | `'TrueFalse'` | `'Passwort'`

Bestehende Zeilen erhalten `DEFAULT 'MC'` — kein Datenverlust.

### 1.2 `Fragen` — keine Schemaänderung

Bestehende Spalten werden für alle Typen wiederverwendet:

| Typ | Antwort1 | Antwort2 | Antwort3 | Antwort4 | IstAntwortXRichtig |
|---|---|---|---|---|---|
| MC | Antwort A | Antwort B | Antwort C | Antwort D | genau eine = true |
| TrueFalse | `"Wahr"` | `"Falsch"` | _(leer)_ | _(leer)_ | `IstAntwort1Richtig` oder `IstAntwort2Richtig` |
| Passwort | korrekte Antwort | _(leer)_ | _(leer)_ | _(leer)_ | `IstAntwort1Richtig = true` |

### 1.3 `PlayerPoints` — `SessionPints` bleibt, wird immer `0`

Kein Schema-Breaking-Change. `CorrectAnswered` und `PossibleAnswers` bleiben die primären Felder.

---

## 2. Punkte-Entfernung

### Zu entfernen / Nullen:

| Datei | Was entfernen |
|---|---|
| `Playground.cshtml` | Punkte-Anzeige in `.quiz-info` Bar |
| `Playground.cshtml.cs` | `+100`/`−5` Berechnungslogik, `PlayerPoints` Session-Key schreiben/lesen |
| `PlaygroundModel` | Property `PlayerPoints` (oder auf `int` 0 einfrieren) |
| `FinalResult.cshtml` | Spalte `Punkte` aus Tabelle, aus Podium-Anzeige |
| `FinalResult.cshtml.cs` | Sortierung auf `CorrectAnswered DESC` umstellen |
| `PlayerPoints` DB-Write | `SessionPints = 0` schreiben (Spalte bleibt) |

Session-Key `PlayerPoints` wird nicht mehr gesetzt und nicht mehr gelesen.

---

## 3. FrageboegenErstellen — Typ-Auswahl

### 3.1 UI

Oben in der Erstellungsmaske vor den Fragen: **3-Karten Typ-Selector**.

```
[ 🔲 Multiple Choice ]  [ ✅ Wahr / Falsch ]  [ 🔑 Passwort ]
```

Aktiver Typ wird per CSS hervorgehoben. Auswahl steuert per JavaScript welche Antwort-Felder sichtbar sind.

### 3.2 Antwort-Felder je Typ

**MC (Standard):** Alle 4 Antwort-Inputs + Checkbox „Richtig" — unverändert.

**TrueFalse:** Nur 2 Felder, vorbefüllt und readonly:
- `Antwort1 = "Wahr"`, `Antwort2 = "Falsch"`
- Radio-Button: welche ist die korrekte Antwort

**Passwort:** Nur 1 Freitextfeld:
- `Antwort1` = korrekte Antwort (Klartext)
- `IstAntwort1Richtig = true` implizit

### 3.3 Server-Seite

- `Typ` als `<input type="hidden">` im Formular mitschicken
- `FrageboegenErstellen.cshtml.cs`: `Typ` beim INSERT in `Fragebogen` schreiben
- Validierung:
  - MC: wie bisher, genau eine richtige Antwort
  - TrueFalse: `IstAntwort1Richtig XOR IstAntwort2Richtig`
  - Passwort: `Antwort1` nicht leer

---

## 4. Playground — Typ-abhängige Darstellung

### 4.1 Typ laden

In `PlaygroundModel.OnGet` / `OnPost`: `Fragebogen.Typ` aus DB lesen beim ersten Laden, in Session speichern unter Key `QuizTyp`.

### 4.2 Rendering

`Playground.cshtml` prüft `Model.QuizTyp` und rendert:

**MC:** Bestehende 4-Karten-Grid (unverändert).

**TrueFalse:** 2 große Karten nebeneinander:
- Grüne Karte: `✓ Wahr`
- Rote Karte: `✗ Falsch`
- Gleiche `selectAnswer()`-Logik wie MC, nur 2 Karten

**Passwort:** Textfeld + Prüfen-Button:
- `<input type="text" name="UserPasswordAnswer" placeholder="Antwort eingeben..." />`
- Kein Karten-Grid
- Hinweis: „Groß-/Kleinschreibung egal"

### 4.3 Answer-Check-Logik (`OnPostCheckAnswer`)

```csharp
// Passwort
var userInput = Request.Form["UserPasswordAnswer"].ToString().Trim();
var correct = frage.Antwort1.Trim();
bool isCorrect = string.Equals(userInput, correct, StringComparison.OrdinalIgnoreCase);

// TrueFalse
// Gleiche Logik wie MC — IstAntwort1Richtig/IstAntwort2Richtig prüfen
```

Scoring: Kein `+100`/`−5` mehr. Nur `QStates` mit `{Correct: bool, SelectedAnswer: string}` aktualisieren.

### 4.4 Review-Modus (bereits beantwortete Fragen)

- MC + TrueFalse: Karten zeigen grün/rot wie bisher
- Passwort: Eingegebene Antwort und korrekte Antwort nebeneinander anzeigen (readonly)

---

## 5. FinalResult — Leaderboard

### Neue Spalten (Punkte-Spalte entfernt):

| Platz | Name | Richtig | Gesamt | Datum |
|---|---|---|---|---|
| 1 | Max | 8 | 10 | 12.06.2026 |

Sortierung: `CorrectAnswered DESC`, bei Gleichstand `saveTime ASC`.

Podium: bleibt bestehen, zeigt nur noch Namen (kein Punkte-Wert).

---

## 6. Betroffene Dateien (Übersicht)

| Datei | Art der Änderung |
|---|---|
| `Helper/SQLconnection.cs` | Kein SQL für `Typ` — in anderen Helpers |
| `Helper/FragenHelper.cs` | `Fragebogen`-Model um `Typ` erweitern |
| `Helper/Spiel.cs` | `QuizTyp` Session-Key setzen |
| `Pages/Admin/FrageboegenErstellen.cshtml` | Typ-Selector UI, JS für Formular-Umschaltung |
| `Pages/Admin/FrageboegenErstellen.cshtml.cs` | `Typ` speichern, Validierung je Typ |
| `Pages/1Viewer/Playground.cshtml` | Typ-abhängiges Rendering, Punkte raus |
| `Pages/1Viewer/Playground.cshtml.cs` | Check-Logik je Typ, Punkte-Session entfernen |
| `Pages/1Viewer/FinalResult.cshtml` | Punkte-Spalte entfernen, Sortierung |
| `Pages/1Viewer/FinalResult.cshtml.cs` | Sortierung auf CorrectAnswered |
| `wwwroot/css/Playground/Playground.css` | Styles für TrueFalse + Passwort UI |
| DB (manuell) | `ALTER TABLE Fragebogen ADD COLUMN Typ` |

---

## 7. Nicht in Scope

- Punktesystem reparieren oder migrieren (komplett entfernt)
- Typ nachträglich pro Frage änderbar machen
- Fuzzy-Matching bei Passwort-Fragen
- EF-Migration für neue Spalte (weiterhin Raw SQL)
