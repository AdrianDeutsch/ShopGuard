Feature: Bestellvalidierung in der Shop-Datenbank
  Als QA möchte ich nach einer Bestellung über die Oberfläche den Datenbankzustand des Shops prüfen,
  damit ich sicher bin, dass das Backend genau das speichert, was dem Kunden bestätigt wurde.

  Akzeptanzkriterien:
  - AK1: Zu jeder bestätigten Rechnungsnummer existiert genau eine Rechnung des angemeldeten Kunden.
  - AK2: Rechnungsbetrag und Positionen entsprechen dem Warenkorb zum Zeitpunkt der Bestellung.
  - AK3: Status und Zahlungsart entsprechen der Auswahl im Checkout.

  Background:
    Given ich besitze ein registriertes Kundenkonto
    And ich bin als Kunde angemeldet

  @ui @db @e2e @regression
  Scenario: Eine Bestellung mit einem Produkt wird vollständig und korrekt gespeichert
    When ich das Produkt "Combination Pliers" über die Suche in den Warenkorb lege
    And ich zur Kasse gehe
    And ich den Checkout mit Standardadresse und Zahlung per Nachnahme abschließe
    Then erhalte ich eine Bestellbestätigung mit Rechnungsnummer
    And ist zur Rechnungsnummer genau eine Rechnung in der Shop-Datenbank gespeichert
    And gehört die Rechnung zum angemeldeten Kunden
    And entspricht der gespeicherte Rechnungsbetrag der Summe der bestellten Produkte
    And enthält die Rechnung genau die bestellten Produkte mit Menge und Preis
    And hat die Rechnung in der Datenbank den Status "AWAITING_FULFILLMENT"
    And ist als Zahlungsart "cash-on-delivery" gespeichert

  @ui @db @e2e @regression
  Scenario: Eine Bestellung mit zwei Produkten ergibt zwei Rechnungspositionen
    When ich das Produkt "Combination Pliers" über die Suche in den Warenkorb lege
    And ich das Produkt "Claw Hammer with Shock Reduction Grip" über die Suche in den Warenkorb lege
    And ich zur Kasse gehe
    And ich den Checkout mit Standardadresse und Zahlung per Nachnahme abschließe
    Then erhalte ich eine Bestellbestätigung mit Rechnungsnummer
    And ist zur Rechnungsnummer genau eine Rechnung in der Shop-Datenbank gespeichert
    And entspricht der gespeicherte Rechnungsbetrag der Summe der bestellten Produkte
    And enthält die Rechnung genau die bestellten Produkte mit Menge und Preis
