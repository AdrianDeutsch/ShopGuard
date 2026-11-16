# FLAKY-TESTS.md — Umgang mit instabilen Tests

## Wie erkenne ich Flaky Tests?

| Signal | Werkzeug |
|---|---|
| Test ist rot, der Wiederholungslauf grün — ohne Codeänderung | CI-Log des `api`-Jobs: jede Wiederholung der Suite hinterlässt eine Warnung „API suite failed on attempt n“ |
| Fehlerbild ist ein Timeout, kein Assertion-Fehler | JUnit-Report: `TimeoutException` vs. `Should().Be(...)` |
| Fehler tritt nur im Gesamtlauf auf, nicht einzeln | Kompletter Lauf (Gate, wöchentlicher CI-Lauf) vs. gezielter Einzellauf |
| Trace zeigt korrektes App-Verhalten, nur später als erwartet | `playwright show-trace artifacts/<szenario>.trace.zip` |

**Regel:** Jeder rote E2E-Test wird zuerst klassifiziert (Produktfehler / Testfehler / flaky),
bevor er erneut gestartet wird — sonst verschleiert der Retry echte Bugs.
Beispiel aus diesem Projekt: der Checkout-Timeout sah flaky aus, war aber DEFECT-003 (Produktfehler).

## Ursachen & Gegenmaßnahmen in diesem Framework

### 1. Warten (häufigste Ursache)

- **Keine fixen Sleeps.** Statt `WaitForTimeout` nutzen alle Page Objects Playwrights Auto-Waiting
  plus explizite Zustands-Waits (`Locator.WaitForAsync`).
- **Auf Ergebnis warten, nicht auf Aktivität:** Die Suche wartet zweistufig — erst auf die
  "Searched for:"-Caption (Suche wurde angewendet), dann auf Produktkarten *oder* die
  No-Results-Meldung ([BasePage.cs](ShopGuard/ShopGuard.E2ETests/Pages/BasePage.cs)).
  So matchen keine veralteten DOM-Knoten der vorherigen Ansicht.
- **UI-Zustandswechsel abwarten:** Der Finish-Button im Checkout wird erst geklickt, wenn er zu
  "Confirm" umbeschriftet wurde — vorher träfe der Klick den alten Handler
  ([CheckoutPage.cs](ShopGuard/ShopGuard.E2ETests/Pages/CheckoutPage.cs)).

### 2. Selektoren

- Bevorzugt `data-test`-Attribute. Wo der Shop keines anbietet (Produktkarte `a.card`,
  Entfernen-Button im Warenkorb, Hinweistexte), steht ein möglichst kurzer CSS- oder
  Textselektor. XPath-Indizes und lange Klassenketten gibt es nicht.
- Selektoren leben ausschließlich in Page Objects — eine Änderung der App trifft genau eine Datei.

### 3. Testdaten-Isolation

- Jedes Szenario, das ein Kundenkonto braucht, bekommt ein **eigenes**
  (`TestDataGenerator.NewCustomer`) — keine geteilten User, keine Reihenfolge-Abhängigkeiten.
- Das Konto wird **über die Shop-API** angelegt, nicht über das Registrierungsformular
  ([ShopApiClient.cs](ShopGuard/ShopGuard.Core/Api/ShopApiClient.cs)). Das spart pro Szenario
  mehrere Sekunden Formular-Klicks, und ein kaputtes Formular lässt nur das Szenario scheitern,
  das das Formular prüft.
- Jedes UI-Szenario läuft in einem **frischen BrowserContext** (eigene Cookies/LocalStorage, eigener Warenkorb).
- @db-Szenarien **lesen nur** aus der Shop-Datenbank. Geschrieben wird ausschließlich über den
  Shop selbst, deshalb gibt es nichts aufzuräumen und keinen Zustand, den der Shop nicht auch
  selbst erzeugen könnte.

### 4. Erwartungswerte

- **Erwartet wird, was die Anwendung abschickt, nicht was der Test eingetippt hat.** Das
  Registrierungsformular schlägt die Adresse nach, sobald Land, Postleitzahl und Hausnummer
  feststehen, und überschreibt Straße, Ort und Bundesland. Ein Vergleich der Datenbank mit den
  eingetippten Werten schlug deshalb fehl, obwohl der Shop korrekt speicherte. Die Felder vor
  dem Absenden auszulesen wäre ein Wettlauf mit der Antwort des Nachschlagens. Das Page Object
  schneidet stattdessen den Request mit, den das Formular beim Absenden schickt, und gibt
  dessen Adresse zurück ([RegisterPage.cs](ShopGuard/ShopGuard.E2ETests/Pages/RegisterPage.cs)).
- **Datenbankprüfungen fragen wiederholt ab statt zu schlafen.** Die Oberfläche bestätigt eine
  Bestellung, sobald die API geantwortet hat. `ShopDatabaseContext.EventuallyAsync` wiederholt
  die Abfrage bis zu fünf Sekunden lang und bricht ab, sobald die Zeile da ist.

### 5. Externe Abhängigkeiten

- restful-booker ist ein öffentliches Demo-System und gelegentlich langsam. Dafür gilt:
  großzügige, zentral konfigurierte Timeouts (`DefaultTimeoutMs` in `appsettings.test.json`)
  und eine Wiederholung der ganzen Suite in der CI — aber **kein** Retry auf Szenario-Ebene,
  damit Instabilität sichtbar bleibt.
- Der Toolshop läuft für die UI-Szenarien selbst gehostet, mit festgelegten Image-Versionen
  ([toolshop.compose.yml](docker/toolshop.compose.yml)). Damit ändert sich das System unter
  Test nur, wenn die Version im Repository geändert wird.

## Workflow bei Verdacht auf Flakiness

1. Trace + Screenshot ansehen: bei UI-Szenarien im Ordner `artifacts/` neben dem Testlauf,
   bei API-Szenarien den Report aus den Artefakten des CI-Jobs.
2. Lokal 5× gezielt ausführen: `dotnet test --filter "Name~<Szenario>"`.
3. Klassifizieren: Produktfehler → Defect Report; Testfehler → fixen; flaky → Wait/Selektor/Datenisolation prüfen.
4. Bis zur Behebung: Szenario mit `@wip` taggen (läuft in keinem Gate), Ticket verlinken — niemals stillschweigend löschen.

Dasselbe Tag trägt ein Szenario, das einen **bekannten, noch offenen Produktfehler** beschreibt:
`Registrierung.feature` enthält das Soll-Verhalten zu DEFECT-003 als `@wip @defect-003`. Es ist
absichtlich rot, blockiert keinen Push und wird ohne Änderung grün, sobald der Fehler behoben ist.
