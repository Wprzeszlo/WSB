using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cars4Us;

public sealed class JsonDataStore
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string FilePath { get; }
    public DealershipData Data { get; private set; }

    private JsonDataStore(string filePath, DealershipData data)
    {
        FilePath = filePath;
        Data = data;
    }

    public static JsonDataStore LoadOrSeed(string filePath)
    {
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
            var data = JsonSerializer.Deserialize<DealershipData>(json, options) ?? Seed();
            return new JsonDataStore(filePath, data);
        }
        var store = new JsonDataStore(filePath, Seed());
        store.Save();
        return store;
    }

    public void Save() => File.WriteAllText(FilePath, JsonSerializer.Serialize(Data, _options));

    private static DealershipData Seed()
    {
        var data = new DealershipData();
        data.Options.AddRange(new[]
        {
            new CarOption { Id = "safety", Name = "Pakiet bezpieczeństwa", Category = "Pakiety", Price = 8200 },
            new CarOption { Id = "multimedia", Name = "Multimedia premium", Category = "Multimedia", Price = 6500 },
            new CarOption { Id = "leather", Name = "Skórzana tapicerka", Category = "Komfort", Price = 9400, Requires = new() { "heated" } },
            new CarOption { Id = "heated", Name = "Podgrzewane fotele", Category = "Komfort", Price = 2800 },
            new CarOption { Id = "manual", Name = "Manualna skrzynia sportowa", Category = "Napęd", Price = 1800, Excludes = new() { "ev-pack" } },
            new CarOption { Id = "ev-pack", Name = "Pakiet elektryczny", Category = "Napęd", Price = 12000, Excludes = new() { "manual" } },
            new CarOption { Id = "wheels", Name = "Felgi szprychowe 19 cali", Category = "Wygląd", Price = 5100 },
            new CarOption { Id = "ceramic", Name = "Powłoka ceramiczna", Category = "Usługi", Price = 3300 }
        });

        data.Vehicles.AddRange(new[]
        {
            new CarBuilder().Identity("OLD001VIN1970", "Ford", "Mustang Fastback").Technical(EngineType.Petrol, Gearbox.Manual, 84000).Price(245000).Availability(VehicleAvailability.InShowroom).Options("manual", "wheels").TestDriveCar().Build(),
            new CarBuilder().Identity("OLD002VIN1963", "Chevrolet", "Corvette C2").Technical(EngineType.Petrol, Gearbox.Manual, 62000).Price(389000).Availability(VehicleAvailability.InShowroom).Options("leather").Build(),
            new CarBuilder().Identity("NEW001VIN2026", "Volvo", "EX30").Technical(EngineType.Electric, Gearbox.Automatic, 0).Price(189900).Availability(VehicleAvailability.OnOrder).Options("ev-pack", "safety").Build(),
            new CarBuilder().Identity("USE001VIN2018", "Mercedes", "E Coupe").Technical(EngineType.Diesel, Gearbox.Automatic, 93000).Price(142000).Availability(VehicleAvailability.InShowroom).Options("multimedia", "heated").TestDriveCar().Build()
        });

        data.Customers.AddRange(new[]
        {
            new Customer { Name = "Anna Kowalska", Phone = "501 222 333", Email = "anna@example.com" },
            new Customer { Name = "Marek Nowak", Phone = "601 444 555", Email = "marek@example.com" }
        });

        data.Employees.AddRange(new[]
        {
            new Employee { Name = "Karolina Wójcik", Role = EmployeeRole.Salesperson },
            new Employee { Name = "Tomasz Zieliński", Role = EmployeeRole.Manager },
            new Employee { Name = "Piotr Maj", Role = EmployeeRole.ServiceTechnician }
        });
        data.Notifications.Add($"{DateTime.Now:g}: Utworzono przykładową bazę Cars4Us.");
        return data;
    }
}
