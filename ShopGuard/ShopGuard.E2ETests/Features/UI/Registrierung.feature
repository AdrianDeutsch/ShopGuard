Feature: Registrierung
  Als Besucher möchte ich ein Kundenkonto anlegen,
  damit ich bestellen und meine Bestellungen einsehen kann.

  Akzeptanzkriterien:
  - AK1: Nach dem Absenden ist das Konto mit den Daten gespeichert, die das Formular abgeschickt hat.
  - AK2: Mit dem neuen Konto ist eine Anmeldung möglich.
  - AK3: Alle Adressfelder des Formulars werden gespeichert, auch die Hausnummer.

  Hinweis: Sobald Land, Postleitzahl und Hausnummer feststehen, schlägt das Formular die Adresse
  nach und ersetzt Straße, Ort und Bundesland. Erwartet wird deshalb, was das Formular beim
  Absenden an die API geschickt hat.

  @ui @db @regression
  Scenario: Ein über das Formular registrierter Kunde wird gespeichert und kann sich anmelden
    When ich mich über das Registrierungsformular als neuer Kunde registriere
    Then ist das Kundenkonto mit den abgeschickten Daten in der Shop-Datenbank gespeichert
    And kann sich der neue Kunde über die Shop-API anmelden

  # Bekannter Produktfehler DEFECT-003: Das Formular zeigt die Hausnummer an, sendet sie aber
  # nicht an die API. Das Szenario beschreibt das Soll-Verhalten und bleibt @wip, bis der Fehler
  # behoben ist. Dann wird es ohne Änderung grün und wandert in die Regression.
  @ui @db @wip @defect-003
  Scenario: Die im Formular eingegebene Hausnummer wird gespeichert
    When ich mich über das Registrierungsformular als neuer Kunde registriere
    Then ist die im Formular angezeigte Hausnummer in der Shop-Datenbank gespeichert
