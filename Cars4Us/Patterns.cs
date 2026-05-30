namespace Cars4Us;

public sealed class CarBuilder
{
    private readonly Vehicle _vehicle = new();
    public CarBuilder Identity(string vin, string brand, string model) { _vehicle.Vin = vin; _vehicle.Brand = brand; _vehicle.Model = model; return this; }
    public CarBuilder Technical(EngineType engine, Gearbox gearbox, int mileage) { _vehicle.Engine = engine; _vehicle.Gearbox = gearbox; _vehicle.Mileage = mileage; return this; }
    public CarBuilder Price(decimal price) { _vehicle.BasePrice = price; return this; }
    public CarBuilder Availability(VehicleAvailability availability) { _vehicle.Availability = availability; _vehicle.StateName = availability == VehicleAvailability.OnOrder ? "W transporcie" : "Na ekspozycji"; return this; }
    public CarBuilder Options(params string[] ids) { _vehicle.SelectedOptionIds.AddRange(ids); return this; }
    public CarBuilder TestDriveCar(bool value = true) { _vehicle.IsTestDriveCar = value; return this; }
    public Vehicle Build() => _vehicle;
}

public interface IOptionComposite
{
    string Name { get; }
    decimal Price { get; }
    IEnumerable<string> OptionIds { get; }
}

public sealed class OptionLeaf : IOptionComposite
{
    private readonly CarOption _option;
    public OptionLeaf(CarOption option) => _option = option;
    public string Name => _option.Name;
    public decimal Price => _option.Price;
    public IEnumerable<string> OptionIds => new[] { _option.Id };
}

public sealed class OptionPackage : IOptionComposite
{
    private readonly List<IOptionComposite> _items = new();
    public OptionPackage(string name) => Name = name;
    public string Name { get; }
    public decimal Price => _items.Sum(i => i.Price) * 0.92m;
    public IEnumerable<string> OptionIds => _items.SelectMany(i => i.OptionIds);
    public OptionPackage Add(IOptionComposite item) { _items.Add(item); return this; }
}

public sealed class OptionDependencyMediator
{
    private readonly IReadOnlyList<CarOption> _options;
    public OptionDependencyMediator(IReadOnlyList<CarOption> options) => _options = options;

    public OptionResult Normalize(Vehicle vehicle, IEnumerable<string> selectedIds)
    {
        var selected = selectedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var messages = new List<string>();
        if (vehicle.Engine == EngineType.Electric && vehicle.Gearbox == Gearbox.Manual)
            messages.Add("Silnik elektryczny wyklucza manualną skrzynię biegów. Zmień skrzynię na automatyczną.");

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var option in _options.Where(o => selected.Contains(o.Id)))
            {
                foreach (var required in option.Requires.Where(r => selected.Add(r)))
                {
                    messages.Add($"{option.Name} wymaga opcji: {NameOf(required)}.");
                    changed = true;
                }
                foreach (var excluded in option.Excludes.Where(selected.Contains).ToList())
                {
                    selected.Remove(excluded);
                    messages.Add($"{option.Name} wyklucza opcję: {NameOf(excluded)}.");
                    changed = true;
                }
            }
        }
        return new OptionResult(selected.ToList(), messages);
    }

    private string NameOf(string id) => _options.FirstOrDefault(o => o.Id == id)?.Name ?? id;
}

public sealed record OptionResult(List<string> SelectedIds, List<string> Messages);

public interface IFinancingStrategy
{
    string Name { get; }
    PricingResult Calculate(decimal amount);
}

public sealed class CashStrategy : IFinancingStrategy
{
    public string Name => "Gotówka";
    public PricingResult Calculate(decimal amount) => new(amount * 0.985m, "Rabat 1,5% za płatność gotówką.");
}

public sealed class LeasingStrategy : IFinancingStrategy
{
    public string Name => "Leasing";
    public PricingResult Calculate(decimal amount) => new(amount * 1.035m, "Leasing: opłata przygotowawcza i preferencyjne ubezpieczenie.");
}

public sealed class CreditStrategy : IFinancingStrategy
{
    public string Name => "Kredyt";
    public PricingResult Calculate(decimal amount) => new(amount * 1.055m, "Kredyt: prowizja bankowa i rozszerzone ubezpieczenie.");
}

public sealed record PricingResult(decimal Amount, string Description);

public interface IPriceComponent
{
    decimal Calculate();
    string Describe();
}

public sealed class BaseVehiclePrice : IPriceComponent
{
    private readonly decimal _basePrice;
    public BaseVehiclePrice(decimal basePrice) => _basePrice = basePrice;
    public decimal Calculate() => _basePrice;
    public string Describe() => $"Cena bazowa: {_basePrice:C0}";
}

public abstract class PriceDecorator : IPriceComponent
{
    protected readonly IPriceComponent Inner;
    protected PriceDecorator(IPriceComponent inner) => Inner = inner;
    public abstract decimal Calculate();
    public abstract string Describe();
}

public sealed class MarginDecorator : PriceDecorator
{
    public MarginDecorator(IPriceComponent inner) : base(inner) { }
    public override decimal Calculate() => Inner.Calculate() * 1.07m;
    public override string Describe() => $"{Inner.Describe()}\r\nMarża salonu: +7%";
}

public sealed class SeasonalPromotionDecorator : PriceDecorator
{
    public SeasonalPromotionDecorator(IPriceComponent inner) : base(inner) { }
    public override decimal Calculate() => Inner.Calculate() - 3500m;
    public override string Describe() => $"{Inner.Describe()}\r\nPromocja sezonowa: -3 500 zł";
}

public sealed class FleetDiscountDecorator : PriceDecorator
{
    private readonly bool _enabled;
    public FleetDiscountDecorator(IPriceComponent inner, bool enabled) : base(inner) => _enabled = enabled;
    public override decimal Calculate() => _enabled ? Inner.Calculate() * 0.96m : Inner.Calculate();
    public override string Describe() => _enabled ? $"{Inner.Describe()}\r\nZniżka flotowa: -4%" : Inner.Describe();
}

public sealed class InsuranceDecorator : PriceDecorator
{
    public InsuranceDecorator(IPriceComponent inner) : base(inner) { }
    public override decimal Calculate() => Inner.Calculate() + 4200m;
    public override string Describe() => $"{Inner.Describe()}\r\nUbezpieczenie: +4 200 zł";
}

public sealed class ExtendedWarrantyDecorator : PriceDecorator
{
    public ExtendedWarrantyDecorator(IPriceComponent inner) : base(inner) { }
    public override decimal Calculate() => Inner.Calculate() + 5900m;
    public override string Describe() => $"{Inner.Describe()}\r\nPrzedłużona gwarancja: +5 900 zł";
}

public interface IVehicleState
{
    string Name { get; }
    bool CanReserve { get; }
    string Next();
}

public sealed class InTransportState : IVehicleState { public string Name => "W transporcie"; public bool CanReserve => false; public string Next() => "Na ekspozycji"; }
public sealed class OnDisplayState : IVehicleState { public string Name => "Na ekspozycji"; public bool CanReserve => true; public string Next() => "Zarezerwowane"; }
public sealed class ReservedState : IVehicleState { public string Name => "Zarezerwowane"; public bool CanReserve => false; public string Next() => "Sprzedane"; }
public sealed class SoldState : IVehicleState { public string Name => "Sprzedane"; public bool CanReserve => false; public string Next() => "Wydane"; }
public sealed class ReleasedState : IVehicleState { public string Name => "Wydane"; public bool CanReserve => false; public string Next() => "Wydane"; }

public static class VehicleStateFactory
{
    public static IVehicleState From(string name) => name switch
    {
        "W transporcie" => new InTransportState(),
        "Zarezerwowane" => new ReservedState(),
        "Sprzedane" => new SoldState(),
        "Wydane" => new ReleasedState(),
        _ => new OnDisplayState()
    };
}

public interface IInventoryObserver { void Notify(string message); }

public sealed class InventoryNotifier
{
    private readonly List<IInventoryObserver> _observers = new();
    public void Subscribe(IInventoryObserver observer) => _observers.Add(observer);
    public void Publish(string message) { foreach (var observer in _observers) observer.Notify(message); }
}

public sealed class NotificationLogObserver : IInventoryObserver
{
    private readonly DealershipData _data;
    public NotificationLogObserver(DealershipData data) => _data = data;
    public void Notify(string message) => _data.Notifications.Insert(0, $"{DateTime.Now:g}: {message}");
}

public interface ITransactionCommand
{
    void Execute();
    void Undo();
}

public sealed class AdvanceTransactionCommand : ITransactionCommand
{
    private readonly SaleTransaction _transaction;
    private readonly Vehicle _vehicle;
    private readonly Employee _employee;
    private TransactionSnapshot? _snapshot;
    private readonly string _description;

    public AdvanceTransactionCommand(SaleTransaction transaction, Vehicle vehicle, Employee employee, string description)
    {
        _transaction = transaction;
        _vehicle = vehicle;
        _employee = employee;
        _description = description;
    }

    public void Execute()
    {
        _snapshot = new TransactionSnapshot
        {
            Stage = _transaction.Stage,
            VehicleState = _vehicle.StateName,
            EmployeeCommission = _employee.CommissionBalance,
            Description = _description
        };
        _transaction.History.Add(_snapshot);
        _transaction.Stage = _transaction.Stage switch
        {
            TransactionStage.Reserved => TransactionStage.CreditVerification,
            TransactionStage.CreditVerification => TransactionStage.Insurance,
            TransactionStage.Insurance => TransactionStage.ReadyToRelease,
            TransactionStage.ReadyToRelease => TransactionStage.Released,
            _ => _transaction.Stage
        };
        _vehicle.StateName = _transaction.Stage == TransactionStage.Released ? "Wydane" :
            _transaction.Stage == TransactionStage.ReadyToRelease ? "Sprzedane" : "Zarezerwowane";
        _employee.CommissionBalance += Math.Round(_transaction.FinalPrice * 0.004m, 2);
    }

    public void Undo()
    {
        if (_snapshot is null) return;
        _transaction.Stage = _snapshot.Stage;
        _vehicle.StateName = _snapshot.VehicleState;
        _employee.CommissionBalance = _snapshot.EmployeeCommission;
    }
}

public sealed class SalesFacade
{
    private readonly DealershipData _data;
    private readonly InventoryNotifier _notifier;
    public SalesFacade(DealershipData data, InventoryNotifier notifier) { _data = data; _notifier = notifier; }

    public SaleTransaction ReserveAndStartSale(Vehicle vehicle, Customer customer, Employee salesperson, FinancingKind financing, IEnumerable<string> selectedOptionIds, decimal finalPrice)
    {
        if (!VehicleStateFactory.From(vehicle.StateName).CanReserve) throw new InvalidOperationException("Auto nie jest dostępne do rezerwacji.");
        vehicle.StateName = "Zarezerwowane";
        var transaction = new SaleTransaction
        {
            VehicleVin = vehicle.Vin,
            CustomerId = customer.Id,
            SalespersonId = salesperson.Id,
            Financing = financing,
            SelectedOptionIds = selectedOptionIds.ToList(),
            FinalPrice = finalPrice
        };
        salesperson.CommissionBalance += Math.Round(finalPrice * 0.01m, 2);
        _data.Transactions.Add(transaction);
        _notifier.Publish($"VIN {vehicle.Vin} został zarezerwowany przez {customer.Name}.");
        return transaction;
    }

    public void Withdraw(SaleTransaction transaction)
    {
        var vehicle = _data.Vehicles.First(v => v.Vin == transaction.VehicleVin);
        var employee = _data.Employees.First(e => e.Id == transaction.SalespersonId);
        transaction.History.Add(new TransactionSnapshot
        {
            Stage = transaction.Stage,
            VehicleState = vehicle.StateName,
            EmployeeCommission = employee.CommissionBalance,
            Description = "Wycofanie transakcji"
        });
        transaction.Stage = TransactionStage.Withdrawn;
        vehicle.StateName = vehicle.Availability == VehicleAvailability.OnOrder ? "W transporcie" : "Na ekspozycji";
        employee.CommissionBalance = 0m;
        _notifier.Publish($"Rezerwacja VIN {vehicle.Vin} została wycofana. Auto wraca do statusu dostępnego.");
    }
}
