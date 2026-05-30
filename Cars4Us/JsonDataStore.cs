using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Cars4Us;

public sealed class JsonDataStore
{
    public string FilePath { get; }
    public DealershipData Data { get; private set; }

    private JsonDataStore(string filePath, DealershipData data)
    {
        FilePath = filePath;
        Data = data;
    }

    public static JsonDataStore LoadOrSeed(string filePath)
    {
        var store = new JsonDataStore(filePath, new DealershipData());
        store.EnsureDatabase();
        store.Data = store.HasVehicles() ? store.Load() : Seed();
        if (!store.HasVehicles()) store.Save();
        return store;
    }

    public void Save()
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        ClearTables(connection, transaction);
        SaveVehicles(connection, transaction);
        SaveCustomers(connection, transaction);
        SaveEmployees(connection, transaction);
        SaveOptions(connection, transaction);
        SaveTestDrives(connection, transaction);
        SaveTransactions(connection, transaction);
        SaveNotifications(connection, transaction);
        transaction.Commit();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={FilePath}");
        connection.Open();
        return connection;
    }

    private void EnsureDatabase()
    {
        using var connection = OpenConnection();
        Execute(connection, null, """
            CREATE TABLE IF NOT EXISTS Vehicles (
                Vin TEXT PRIMARY KEY,
                Brand TEXT NOT NULL,
                Model TEXT NOT NULL,
                Engine TEXT NOT NULL,
                Gearbox TEXT NOT NULL,
                Mileage INTEGER NOT NULL,
                BasePrice REAL NOT NULL,
                Availability TEXT NOT NULL,
                StateName TEXT NOT NULL,
                SelectedOptionIds TEXT NOT NULL,
                IsTestDriveCar INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Customers (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Phone TEXT NOT NULL,
                Email TEXT NOT NULL,
                PurchaseHistory TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Employees (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Role TEXT NOT NULL,
                CommissionBalance REAL NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Options (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Category TEXT NOT NULL,
                Price REAL NOT NULL,
                RequiresJson TEXT NOT NULL,
                ExcludesJson TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS TestDrives (
                Id TEXT PRIMARY KEY,
                VehicleVin TEXT NOT NULL,
                CustomerId TEXT NOT NULL,
                SalespersonId TEXT NOT NULL,
                Start TEXT NOT NULL,
                End TEXT NOT NULL,
                Notes TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS TransactionsTable (
                Id TEXT PRIMARY KEY,
                VehicleVin TEXT NOT NULL,
                CustomerId TEXT NOT NULL,
                SalespersonId TEXT NOT NULL,
                Stage TEXT NOT NULL,
                Financing TEXT NOT NULL,
                FinalPrice REAL NOT NULL,
                CreatedAt TEXT NOT NULL,
                HistoryJson TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Notifications (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SortOrder INTEGER NOT NULL,
                Message TEXT NOT NULL
            );
            """);
    }

    private bool HasVehicles()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Vehicles";
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private DealershipData Load()
    {
        using var connection = OpenConnection();
        var data = new DealershipData();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM Vehicles ORDER BY Brand, Model";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                data.Vehicles.Add(new Vehicle
                {
                    Vin = reader.GetString(reader.GetOrdinal("Vin")),
                    Brand = reader.GetString(reader.GetOrdinal("Brand")),
                    Model = reader.GetString(reader.GetOrdinal("Model")),
                    Engine = Enum.Parse<EngineType>(reader.GetString(reader.GetOrdinal("Engine"))),
                    Gearbox = Enum.Parse<Gearbox>(reader.GetString(reader.GetOrdinal("Gearbox"))),
                    Mileage = reader.GetInt32(reader.GetOrdinal("Mileage")),
                    BasePrice = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("BasePrice"))),
                    Availability = Enum.Parse<VehicleAvailability>(reader.GetString(reader.GetOrdinal("Availability"))),
                    StateName = reader.GetString(reader.GetOrdinal("StateName")),
                    SelectedOptionIds = ReadList<string>(reader.GetString(reader.GetOrdinal("SelectedOptionIds"))),
                    IsTestDriveCar = reader.GetInt32(reader.GetOrdinal("IsTestDriveCar")) == 1
                });
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM Customers ORDER BY Name";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                data.Customers.Add(new Customer
                {
                    Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Phone = reader.GetString(reader.GetOrdinal("Phone")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    PurchaseHistory = ReadList<string>(reader.GetString(reader.GetOrdinal("PurchaseHistory")))
                });
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM Employees ORDER BY Name";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                data.Employees.Add(new Employee
                {
                    Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Role = Enum.Parse<EmployeeRole>(reader.GetString(reader.GetOrdinal("Role"))),
                    CommissionBalance = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("CommissionBalance")))
                });
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM Options ORDER BY Category, Name";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                data.Options.Add(new CarOption
                {
                    Id = reader.GetString(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Category = reader.GetString(reader.GetOrdinal("Category")),
                    Price = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("Price"))),
                    Requires = ReadList<string>(reader.GetString(reader.GetOrdinal("RequiresJson"))),
                    Excludes = ReadList<string>(reader.GetString(reader.GetOrdinal("ExcludesJson")))
                });
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM TestDrives ORDER BY Start";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                data.TestDrives.Add(new TestDrive
                {
                    Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                    VehicleVin = reader.GetString(reader.GetOrdinal("VehicleVin")),
                    CustomerId = Guid.Parse(reader.GetString(reader.GetOrdinal("CustomerId"))),
                    SalespersonId = Guid.Parse(reader.GetString(reader.GetOrdinal("SalespersonId"))),
                    Start = DateTime.Parse(reader.GetString(reader.GetOrdinal("Start"))),
                    End = DateTime.Parse(reader.GetString(reader.GetOrdinal("End"))),
                    Notes = reader.GetString(reader.GetOrdinal("Notes"))
                });
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM TransactionsTable ORDER BY CreatedAt";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                data.Transactions.Add(new SaleTransaction
                {
                    Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                    VehicleVin = reader.GetString(reader.GetOrdinal("VehicleVin")),
                    CustomerId = Guid.Parse(reader.GetString(reader.GetOrdinal("CustomerId"))),
                    SalespersonId = Guid.Parse(reader.GetString(reader.GetOrdinal("SalespersonId"))),
                    Stage = Enum.Parse<TransactionStage>(reader.GetString(reader.GetOrdinal("Stage"))),
                    Financing = Enum.Parse<FinancingKind>(reader.GetString(reader.GetOrdinal("Financing"))),
                    FinalPrice = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("FinalPrice"))),
                    CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                    History = ReadList<TransactionSnapshot>(reader.GetString(reader.GetOrdinal("HistoryJson")))
                });
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT Message FROM Notifications ORDER BY SortOrder";
            using var reader = command.ExecuteReader();
            while (reader.Read()) data.Notifications.Add(reader.GetString(0));
        }

        return data;
    }

    private void ClearTables(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var table in new[] { "Vehicles", "Customers", "Employees", "Options", "TestDrives", "TransactionsTable", "Notifications" })
            Execute(connection, transaction, $"DELETE FROM {table}");
    }

    private void SaveVehicles(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var vehicle in Data.Vehicles)
        {
            Execute(connection, transaction, """
                INSERT INTO Vehicles VALUES ($vin, $brand, $model, $engine, $gearbox, $mileage, $basePrice, $availability, $stateName, $selectedOptionIds, $isTestDriveCar)
                """,
                ("$vin", vehicle.Vin), ("$brand", vehicle.Brand), ("$model", vehicle.Model), ("$engine", vehicle.Engine.ToString()),
                ("$gearbox", vehicle.Gearbox.ToString()), ("$mileage", vehicle.Mileage), ("$basePrice", vehicle.BasePrice),
                ("$availability", vehicle.Availability.ToString()), ("$stateName", vehicle.StateName),
                ("$selectedOptionIds", WriteList(vehicle.SelectedOptionIds)), ("$isTestDriveCar", vehicle.IsTestDriveCar ? 1 : 0));
        }
    }

    private void SaveCustomers(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var customer in Data.Customers)
        {
            Execute(connection, transaction, "INSERT INTO Customers VALUES ($id, $name, $phone, $email, $purchaseHistory)",
                ("$id", customer.Id.ToString()), ("$name", customer.Name), ("$phone", customer.Phone), ("$email", customer.Email),
                ("$purchaseHistory", WriteList(customer.PurchaseHistory)));
        }
    }

    private void SaveEmployees(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var employee in Data.Employees)
        {
            Execute(connection, transaction, "INSERT INTO Employees VALUES ($id, $name, $role, $commissionBalance)",
                ("$id", employee.Id.ToString()), ("$name", employee.Name), ("$role", employee.Role.ToString()),
                ("$commissionBalance", employee.CommissionBalance));
        }
    }

    private void SaveOptions(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var option in Data.Options)
        {
            Execute(connection, transaction, "INSERT INTO Options VALUES ($id, $name, $category, $price, $requires, $excludes)",
                ("$id", option.Id), ("$name", option.Name), ("$category", option.Category), ("$price", option.Price),
                ("$requires", WriteList(option.Requires)), ("$excludes", WriteList(option.Excludes)));
        }
    }

    private void SaveTestDrives(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var drive in Data.TestDrives)
        {
            Execute(connection, transaction, "INSERT INTO TestDrives VALUES ($id, $vehicleVin, $customerId, $salespersonId, $start, $end, $notes)",
                ("$id", drive.Id.ToString()), ("$vehicleVin", drive.VehicleVin), ("$customerId", drive.CustomerId.ToString()),
                ("$salespersonId", drive.SalespersonId.ToString()), ("$start", drive.Start.ToString("O")),
                ("$end", drive.End.ToString("O")), ("$notes", drive.Notes));
        }
    }

    private void SaveTransactions(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var sale in Data.Transactions)
        {
            Execute(connection, transaction, """
                INSERT INTO TransactionsTable VALUES ($id, $vehicleVin, $customerId, $salespersonId, $stage, $financing, $finalPrice, $createdAt, $history)
                """,
                ("$id", sale.Id.ToString()), ("$vehicleVin", sale.VehicleVin), ("$customerId", sale.CustomerId.ToString()),
                ("$salespersonId", sale.SalespersonId.ToString()), ("$stage", sale.Stage.ToString()),
                ("$financing", sale.Financing.ToString()), ("$finalPrice", sale.FinalPrice),
                ("$createdAt", sale.CreatedAt.ToString("O")), ("$history", WriteList(sale.History)));
        }
    }

    private void SaveNotifications(SqliteConnection connection, SqliteTransaction transaction)
    {
        for (var i = 0; i < Data.Notifications.Count; i++)
            Execute(connection, transaction, "INSERT INTO Notifications (SortOrder, Message) VALUES ($sortOrder, $message)", ("$sortOrder", i), ("$message", Data.Notifications[i]));
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction? transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    private static string WriteList<T>(List<T> values) => JsonSerializer.Serialize(values);
    private static List<T> ReadList<T>(string json) => JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();

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
        data.Notifications.Add($"{DateTime.Now:g}: Utworzono przykładową bazę Cars4Us w SQLite.");
        return data;
    }
}
