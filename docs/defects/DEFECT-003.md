# DEFECT-003 — [UI] Registrierungsformular sendet die Hausnummer nicht an die API

| Feld | Inhalt |
|---|---|
| **Summary** | [UI] Die im Registrierungsformular eingegebene Hausnummer fehlt im Kundenkonto; im Checkout bleibt deshalb "Proceed" im Billing-Schritt deaktiviert |
| **Issue Type** | Bug |
| **Severity** | Minor |
| **Priority** | Medium |
| **Environment** | Toolshop sprint5, Images `sprint5-api:2.3` und `sprint5-ui:2.3`, selbst gehostet (`docker/toolshop.compose.yml`), Chromium headless, macOS. Erstbefund 2026-06-12, Ursachenanalyse 2026-11-16 |
| **Affected Component** | UI / Registrierung (Folgefehler im Checkout) |
| **Found in** | UI-Szenario "Kompletter Bestellprozess von Login bis Bestellbestätigung" (`@ui @e2e`) |

## Steps to Reproduce

1. `/auth/register` öffnen und alle Felder ausfüllen, **House number** = `1`.
2. Formular absenden; der Shop leitet zur Anmeldung weiter.
3. Anmelden, ein Produkt in den Warenkorb legen, `/checkout` öffnen.
4. Schritte "Cart" und "Sign in" bestätigen, im Schritt "Billing Address" das Feld **House number** betrachten.

## Expected Result

Das Kundenkonto enthält alle Adressdaten des Formulars. Die Rechnungsadresse im Checkout ist
vollständig vorbefüllt, "Proceed to checkout" ist aktiv.

## Actual Result

Alle Adressfelder sind vorbefüllt, **House number ist leer**. Der Button `proceed-3` bleibt
`disabled`, bis die Hausnummer von Hand nachgetragen wird.

## Analyse

Zuerst als Testfehler vermutet (Timeout beim Klick auf `proceed-3`). Der Playwright-Trace zeigte
den deaktivierten Button, der Feld-Dump die leere Hausnummer. Damit war klar, dass der Wert
fehlt, aber noch nicht, wo er verloren geht. Drei Belege grenzen das ein:

| Schicht | Beobachtung | Beleg |
|---|---|---|
| Formular | Das Feld enthält beim Absenden `1`. | Feldwert direkt vor dem Klick auf "Register" |
| Request | `POST /users/register` enthält im Objekt `address` nur `street`, `city`, `state`, `country`, `postal_code`. **`house_number` fehlt.** | Mitschnitt des Requests im Browser |
| Datenbank | `users.house_number` ist `NULL`. | `SELECT house_number FROM users WHERE email = …` |

Der Request des Formulars (E-Mail und Passwort gekürzt):

```json
{
  "first_name": "Shop", "last_name": "Guard", "dob": "1990-01-15", "phone": "03012345678",
  "email": "…", "password": "…",
  "address": { "street": "Grit-Hirsch-Gasse", "city": "Pinneberg", "state": "Bayern", "country": "DE", "postal_code": "10115" }
}
```

Gegenprobe: Derselbe Request mit `"house_number": "1"` im Objekt `address`, direkt an die API
geschickt, liefert `201`, und der Wert steht danach in `users.house_number`. API und Datenbank
arbeiten also korrekt.

## Root-Cause-Klassifikation

- [x] **Produktfehler im Frontend** — das Registrierungsformular nimmt die Hausnummer nicht in
  den Request auf. Backend und Datenbank sind nicht betroffen.
- [ ] Testfehler
- [ ] Flaky

## Umgang im Testframework

- **Soll-Szenario:** `Registrierung.feature`, "Die im Formular eingegebene Hausnummer wird
  gespeichert", getaggt mit `@wip @defect-003`. Es prüft das erwartete Verhalten gegen die
  Datenbank, ist bis zur Behebung rot und läuft in keinem Gate.
- **Testkonten** der übrigen Szenarien entstehen über die API und haben eine Hausnummer. Der
  Fehler löst dort keinen Folgefehler mehr aus.
- **Kein Workaround mehr im Checkout.** Bis 2026-11-16 trug das Page Object die Hausnummer im
  Billing-Schritt nach, wenn das Feld leer war. Das hätte auch einen echten Fehler beim
  Vorbefüllen verdeckt. `CheckoutPage.ConfirmBillingAddressAsync` tippt jetzt nichts mehr: Fehlt
  ein Pflichtfeld, bleibt der Button deaktiviert und das Szenario schlägt fehl.

## Nebenbefund (kein Defect)

Sobald Land, Postleitzahl und Hausnummer feststehen, ruft das Formular `GET /postcode-lookup`
auf und überschreibt Straße, Ort und Bundesland mit dem Ergebnis. Die Demo-API liefert dafür
erfundene Daten: Postleitzahl 10115 ergab im Test den Ort „Pinneberg“ im Bundesland „Bayern“.
Die eingetippten Werte taugen deshalb nicht als Erwartungswert. Erwartet wird, was das Formular
an die API schickt (siehe [FLAKY-TESTS.md](../../FLAKY-TESTS.md), Abschnitt "Erwartungswerte").

## Attachments

- [x] Request-Body von `POST /users/register` ohne `house_number` (oben zitiert)
- [x] Feld-Dump des Billing-Schritts beim Erstbefund: `house_number = ''`, alle übrigen Felder gefüllt
- [ ] Playwright-Trace des Erstlaufs: lokal ausgewertet, nicht eingecheckt. Ein neuer entsteht
      mit `--filter "TestCategory=defect-003"` unter `artifacts/`.

## Retest

| Datum | Version/Commit | Ergebnis | Tester |
|---|---|---|---|
| 2026-06-12 | Workaround im Page Object | ✅ E2E-Szenario grün | Lokaler Lauf |
| 2026-11-16 | `sprint5-ui:2.3`, selbst gehostet | ❌ Fehler besteht weiter; Soll-Szenario `@defect-003` rot wie erwartet | Lokaler Lauf |
