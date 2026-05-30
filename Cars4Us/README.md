# Cars4Us

System GUI w C# WinForms do obsługi salonu samochodowego: auta nowe, używane, klasyczne "old time", CRM, jazdy próbne, konfigurator opcji, wycena i sprzedaż.

## Uruchomienie

W katalogu projektu:

```powershell
dotnet build
dotnet run
```

Dane zapisują się lokalnie do darmowej bazy SQLite `cars4us.db` obok pliku wykonywalnego.

## Zakres funkcjonalny

- Ewidencja pojazdów: VIN, marka, model, silnik, przebieg, cena, dostępność w salonie lub na zamówienie.
- Kartoteka klientów i CRM: dane klientów, historia zakupów, historia jazd próbnych.
- Katalog opcji i akcesoriów: pakiety, multimedia, felgi, komfort, ceny.
- Kadra sprzedażowa: role pracowników i prowizje przypisane do transakcji.
- Moduł jazd próbnych: rezerwacje auta testowego dla klienta i handlowca z kontrolą kolizji.
- Dependency Engine: mediator reguł wymagań i wykluczeń opcji.
- Pricing Pipeline: strategia finansowania oraz dekoratory ceny.
- Finalizacja i wycofanie transakcji: Command + Memento przywraca status auta i prowizje.
- Warstwa danych: baza SQLite z automatycznym zapisem przy zamknięciu programu.

## Zastosowane wzorce projektowe

- `Builder`: `CarBuilder` tworzy egzemplarze pojazdów z wieloma opcjami.
- `Composite`: `OptionPackage` i `OptionLeaf` modelują pakiety wyposażenia.
- `Mediator`: `OptionDependencyMediator` koordynuje zależności między opcjami.
- `Decorator`: `MarginDecorator`, `InsuranceDecorator`, `ExtendedWarrantyDecorator`, promocje i rabaty.
- `Facade`: `SalesFacade` upraszcza proces sprzedaży i wycofania.
- `State`: klasy stanu pojazdu obsługują cykl życia auta.
- `Observer`: `InventoryNotifier` powiadamia handlowców o dostawach i zwolnieniach rezerwacji.
- `Strategy`: strategie gotówki, leasingu i kredytu.
- `Command + Memento`: `AdvanceTransactionCommand` oraz `TransactionSnapshot`.
