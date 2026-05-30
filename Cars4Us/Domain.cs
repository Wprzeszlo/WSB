namespace Cars4Us;

public enum EngineType { Petrol, Diesel, Hybrid, Electric }
public enum Gearbox { Manual, Automatic }
public enum VehicleAvailability { InShowroom, OnOrder }
public enum EmployeeRole { Salesperson, Manager, ServiceTechnician }
public enum FinancingKind { Cash, Leasing, Credit }
public enum TransactionStage { Reserved, CreditVerification, Insurance, ReadyToRelease, Released, Withdrawn }

public sealed class Vehicle
{
    public string Vin { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public EngineType Engine { get; set; }
    public Gearbox Gearbox { get; set; }
    public int Mileage { get; set; }
    public decimal BasePrice { get; set; }
    public VehicleAvailability Availability { get; set; }
    public string StateName { get; set; } = "Na ekspozycji";
    public List<string> SelectedOptionIds { get; set; } = new();
    public bool IsTestDriveCar { get; set; }
    public override string ToString() => $"{Brand} {Model} ({Vin})";
}

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public List<string> PurchaseHistory { get; set; } = new();
    public override string ToString() => $"{Name} - {Phone}";
}

public sealed class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public EmployeeRole Role { get; set; }
    public decimal CommissionBalance { get; set; }
    public override string ToString() => $"{Name} ({Role})";
}

public sealed class CarOption
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public List<string> Requires { get; set; } = new();
    public List<string> Excludes { get; set; } = new();
    public override string ToString() => $"{Name} (+{Price:C0})";
}

public sealed class TestDrive
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string VehicleVin { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Guid SalespersonId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Notes { get; set; } = "";
}

public sealed class SaleTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string VehicleVin { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Guid SalespersonId { get; set; }
    public TransactionStage Stage { get; set; } = TransactionStage.Reserved;
    public FinancingKind Financing { get; set; }
    public List<string> SelectedOptionIds { get; set; } = new();
    public decimal FinalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<TransactionSnapshot> History { get; set; } = new();
}

public sealed class TransactionSnapshot
{
    public TransactionStage Stage { get; set; }
    public string VehicleState { get; set; } = "";
    public decimal EmployeeCommission { get; set; }
    public DateTime TakenAt { get; set; } = DateTime.Now;
    public string Description { get; set; } = "";
}

public sealed class DealershipData
{
    public List<Vehicle> Vehicles { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
    public List<CarOption> Options { get; set; } = new();
    public List<TestDrive> TestDrives { get; set; } = new();
    public List<SaleTransaction> Transactions { get; set; } = new();
    public List<string> Notifications { get; set; } = new();
    public List<DeletedRecord> DeletedRecords { get; set; } = new();
}

public sealed class DeletedRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateTime DeletedAt { get; set; } = DateTime.Now;
    public string PayloadJson { get; set; } = "";
    public string DependenciesInfo { get; set; } = "";
    public DateTime RestoreUntil => DeletedAt.AddDays(31);
}
