using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Cars4Us;

public sealed class MainForm : Form
{
    private readonly JsonDataStore _store;
    private readonly InventoryNotifier _notifier = new();
    private readonly SalesFacade _sales;
    private readonly BindingSource _vehicles = new();
    private readonly BindingSource _customers = new();
    private readonly BindingSource _employees = new();
    private readonly BindingSource _testDrives = new();
    private readonly BindingSource _transactions = new();
    private readonly BindingSource _notifications = new();
    private DataGridView _vehicleGrid = null!;
    private DataGridView _customerGrid = null!;
    private DataGridView _testDriveGrid = null!;
    private DataGridView _transactionGrid = null!;
    private DataGridView _employeeGrid = null!;
    private DataGridView _notificationGrid = null!;
    private CheckedListBox _optionList = null!;
    private TextBox _pricingBox = null!;
    private ComboBox _financeBox = null!;
    private CheckBox _fleetBox = null!;
    private CheckBox _insuranceBox = null!;
    private CheckBox _warrantyBox = null!;

    public MainForm(JsonDataStore store)
    {
        _store = store;
        _notifier.Subscribe(new NotificationLogObserver(_store.Data));
        _sales = new SalesFacade(_store.Data, _notifier);
        Text = "Cars4Us - salon samochodowy";
        Width = 1180;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BuildUi();
        ConfigurePolishTables();
        RefreshBindings();
    }

    private void ConfigurePolishTables()
    {
        LocalizeGrid(_vehicleGrid, new()
        {
            ["Vin"] = "VIN",
            ["Brand"] = "Marka",
            ["Model"] = "Model",
            ["Engine"] = "Typ silnika",
            ["Gearbox"] = "Skrzynia biegów",
            ["Mileage"] = "Przebieg",
            ["BasePrice"] = "Cena bazowa",
            ["Availability"] = "Dostępność",
            ["StateName"] = "Status",
            ["IsTestDriveCar"] = "Testowe"
        }, "SelectedOptionIds");
        SetColumnWidths(_vehicleGrid, new()
        {
            ["Vin"] = 130,
            ["Brand"] = 120,
            ["Model"] = 130,
            ["Engine"] = 110,
            ["Gearbox"] = 120,
            ["Mileage"] = 95,
            ["BasePrice"] = 120,
            ["Availability"] = 130,
            ["StateName"] = 120,
            ["IsTestDriveCar"] = 85
        });

        LocalizeGrid(_customerGrid, new()
        {
            ["Name"] = "Imię i nazwisko",
            ["Phone"] = "Telefon",
            ["Email"] = "E-mail"
        }, "Id", "PurchaseHistory");

        LocalizeGrid(_testDriveGrid, new()
        {
            ["VehicleVin"] = "VIN pojazdu",
            ["CustomerId"] = "Klient",
            ["SalespersonId"] = "Handlowiec",
            ["Start"] = "Początek",
            ["End"] = "Koniec",
            ["Notes"] = "Notatki"
        }, "Id");

        LocalizeGrid(_transactionGrid, new()
        {
            ["VehicleVin"] = "VIN pojazdu",
            ["CustomerId"] = "Klient",
            ["SalespersonId"] = "Handlowiec",
            ["Stage"] = "Etap",
            ["Financing"] = "Finansowanie",
            ["FinalPrice"] = "Cena końcowa",
            ["CreatedAt"] = "Utworzono"
        }, "Id", "History");

        LocalizeGrid(_employeeGrid, new()
        {
            ["Name"] = "Imię i nazwisko",
            ["Role"] = "Rola",
            ["CommissionBalance"] = "Prowizje"
        }, "Id");

        LocalizeGrid(_notificationGrid, new()
        {
            ["Message"] = "Powiadomienie"
        });
    }

    private void BuildUi()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildVehiclesTab());
        tabs.TabPages.Add(BuildCrmTab());
        tabs.TabPages.Add(BuildOptionsTab());
        tabs.TabPages.Add(BuildTestDriveTab());
        tabs.TabPages.Add(BuildSalesTab());
        tabs.TabPages.Add(BuildStaffTab());
        Controls.Add(tabs);
    }

    private TabPage BuildVehiclesTab()
    {
        var page = new TabPage("Pojazdy");
        _vehicleGrid = Grid();
        _vehicleGrid.DataSource = _vehicles;
        var panel = TopPanel();
        panel.Controls.Add(Button("Dodaj auto old time", AddVehicle));
        panel.Controls.Add(Button("Zmień status", AdvanceVehicleState));
        panel.Controls.Add(Button("Zapisz", Save));
        page.Controls.Add(_vehicleGrid);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildCrmTab()
    {
        var page = new TabPage("Klienci i CRM");
        _customerGrid = Grid();
        _customerGrid.DataSource = _customers;
        var panel = TopPanel();
        panel.Controls.Add(Button("Dodaj klienta", AddCustomer));
        panel.Controls.Add(Button("Historia klienta", ShowCustomerHistory));
        panel.Controls.Add(Button("Zapisz", Save));
        page.Controls.Add(_customerGrid);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildOptionsTab()
    {
        var page = new TabPage("Konfigurator");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 380 };
        _optionList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
        _optionList.ItemCheck += (_, _) => BeginInvoke((Action)RecalculateConfiguration);
        split.Panel1.Controls.Add(_optionList);

        _financeBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        _financeBox.DataSource = Enum.GetValues(typeof(FinancingKind));
        _financeBox.SelectedIndexChanged += (_, _) => RecalculateConfiguration();
        _fleetBox = Check("Klient flotowy", RecalculateConfiguration);
        _insuranceBox = Check("Ubezpieczenie", RecalculateConfiguration, true);
        _warrantyBox = Check("Gwarancja rozszerzona", RecalculateConfiguration);
        _pricingBox = new TextBox { Multiline = true, Dock = DockStyle.Fill, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        var top = TopPanel();
        top.Controls.Add(new Label { Text = "Finansowanie:", AutoSize = true, Padding = new Padding(8, 8, 0, 0) });
        top.Controls.Add(_financeBox);
        top.Controls.Add(_fleetBox);
        top.Controls.Add(_insuranceBox);
        top.Controls.Add(_warrantyBox);
        top.Controls.Add(Button("Zastosuj do auta", ApplyConfiguration));
        split.Panel2.Controls.Add(_pricingBox);
        split.Panel2.Controls.Add(top);
        page.Controls.Add(split);
        return page;
    }

    private TabPage BuildTestDriveTab()
    {
        var page = new TabPage("Jazdy próbne");
        _testDriveGrid = Grid();
        _testDriveGrid.DataSource = _testDrives;
        var panel = TopPanel();
        panel.Controls.Add(Button("Zarezerwuj jazdę", AddTestDrive));
        panel.Controls.Add(Button("Usuń rezerwację", DeleteTestDrive));
        panel.Controls.Add(Button("Zapisz", Save));
        page.Controls.Add(_testDriveGrid);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildSalesTab()
    {
        var page = new TabPage("Sprzedaż");
        _transactionGrid = Grid();
        _transactionGrid.DataSource = _transactions;
        var panel = TopPanel();
        panel.Controls.Add(Button("Rozpocznij sprzedaż", StartSale));
        panel.Controls.Add(Button("Następny etap", AdvanceSale));
        panel.Controls.Add(Button("Wycofaj", WithdrawSale));
        panel.Controls.Add(Button("Zapisz", Save));
        page.Controls.Add(_transactionGrid);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildStaffTab()
    {
        var page = new TabPage("Kadra i powiadomienia");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 580 };
        _employeeGrid = Grid();
        _employeeGrid.DataSource = _employees;
        _notificationGrid = Grid();
        _notificationGrid.DataSource = _notifications;
        split.Panel1.Controls.Add(_employeeGrid);
        split.Panel2.Controls.Add(_notificationGrid);
        var panel = TopPanel();
        panel.Controls.Add(Button("Dodaj pracownika", AddEmployee));
        panel.Controls.Add(Button("Symuluj dostawę auta", SimulateDelivery));
        panel.Controls.Add(Button("Zapisz", Save));
        page.Controls.Add(split);
        page.Controls.Add(panel);
        return page;
    }

    private void RefreshBindings()
    {
        _vehicles.DataSource = new BindingList<Vehicle>(_store.Data.Vehicles);
        _customers.DataSource = new BindingList<Customer>(_store.Data.Customers);
        _employees.DataSource = new BindingList<Employee>(_store.Data.Employees);
        _testDrives.DataSource = new BindingList<TestDrive>(_store.Data.TestDrives);
        _transactions.DataSource = new BindingList<SaleTransaction>(_store.Data.Transactions);
        _notifications.DataSource = new BindingList<NotificationRow>(_store.Data.Notifications.Select(message => new NotificationRow { Message = message }).ToList());
        _optionList.Items.Clear();
        foreach (var option in _store.Data.Options) _optionList.Items.Add(option, false);
        RecalculateConfiguration();
    }

    private Vehicle? SelectedVehicle() => _vehicleGrid.CurrentRow?.DataBoundItem as Vehicle ?? _store.Data.Vehicles.FirstOrDefault(v => VehicleStateFactory.From(v.StateName).CanReserve);
    private SaleTransaction? SelectedTransaction() => (_transactions.Current as SaleTransaction) ?? _store.Data.Transactions.LastOrDefault();
    private Customer? FirstCustomer() => _store.Data.Customers.FirstOrDefault();
    private Employee? FirstSalesperson() => _store.Data.Employees.FirstOrDefault(e => e.Role == EmployeeRole.Salesperson);

    private void AddVehicle(object? sender, EventArgs e)
    {
        var number = _store.Data.Vehicles.Count + 1;
        _store.Data.Vehicles.Add(new CarBuilder()
            .Identity($"OLD{number:000}VIN19{60 + number}", "Porsche", "911 Classic")
            .Technical(EngineType.Petrol, Gearbox.Manual, 75000 + number * 1000)
            .Price(310000 + number * 5000)
            .Availability(VehicleAvailability.InShowroom)
            .Options("leather", "wheels")
            .Build());
        RefreshBindings();
    }

    private void AdvanceVehicleState(object? sender, EventArgs e)
    {
        var vehicle = SelectedVehicle();
        if (vehicle is null) return;
        var state = VehicleStateFactory.From(vehicle.StateName);
        vehicle.StateName = state.Next();
        _notifier.Publish($"Status VIN {vehicle.Vin}: {vehicle.StateName}.");
        RefreshBindings();
    }

    private void AddCustomer(object? sender, EventArgs e)
    {
        var number = _store.Data.Customers.Count + 1;
        _store.Data.Customers.Add(new Customer { Name = $"Klient {number}", Phone = $"500 000 {number:000}", Email = $"klient{number}@cars4us.local" });
        RefreshBindings();
    }

    private void ShowCustomerHistory(object? sender, EventArgs e)
    {
        var customer = _customers.Current as Customer ?? FirstCustomer();
        if (customer is null) return;
        var purchases = string.Join("\r\n", customer.PurchaseHistory.DefaultIfEmpty("Brak zakupów."));
        var drives = _store.Data.TestDrives.Where(d => d.CustomerId == customer.Id).Select(d => $"{d.Start:g} - VIN {d.VehicleVin}");
        MessageBox.Show($"Zakupy:\r\n{purchases}\r\n\r\nJazdy próbne:\r\n{string.Join("\r\n", drives.DefaultIfEmpty("Brak jazd."))}", customer.Name);
    }

    private void AddEmployee(object? sender, EventArgs e)
    {
        var number = _store.Data.Employees.Count + 1;
        _store.Data.Employees.Add(new Employee { Name = $"Handlowiec {number}", Role = EmployeeRole.Salesperson });
        RefreshBindings();
    }

    private void AddTestDrive(object? sender, EventArgs e)
    {
        var vehicle = _store.Data.Vehicles.FirstOrDefault(v => v.IsTestDriveCar);
        var customer = FirstCustomer();
        var salesperson = FirstSalesperson();
        if (vehicle is null || customer is null || salesperson is null) { MessageBox.Show("Brakuje auta testowego, klienta lub handlowca."); return; }
        var start = DateTime.Today.AddDays(1).AddHours(10 + _store.Data.TestDrives.Count % 6);
        var end = start.AddHours(1);
        var conflict = _store.Data.TestDrives.Any(d => d.VehicleVin == vehicle.Vin && start < d.End && end > d.Start);
        if (conflict) { MessageBox.Show("Wybrany termin koliduje z istniejącą jazdą próbną."); return; }
        _store.Data.TestDrives.Add(new TestDrive { VehicleVin = vehicle.Vin, CustomerId = customer.Id, SalespersonId = salesperson.Id, Start = start, End = end, Notes = "Rezerwacja z kalendarza Cars4Us" });
        _notifier.Publish($"Zaplanowano jazdę próbną VIN {vehicle.Vin} dla {customer.Name}.");
        RefreshBindings();
    }

    private void DeleteTestDrive(object? sender, EventArgs e)
    {
        if (_testDrives.Current is TestDrive drive)
        {
            _store.Data.TestDrives.Remove(drive);
            _notifier.Publish($"Anulowano jazdę próbną VIN {drive.VehicleVin}.");
            RefreshBindings();
        }
    }

    private void ApplyConfiguration(object? sender, EventArgs e)
    {
        var vehicle = SelectedVehicle();
        if (vehicle is null) return;
        var result = ValidateSelectedOptions(vehicle);
        vehicle.SelectedOptionIds = result.SelectedIds;
        _notifier.Publish($"Zastosowano konfigurację dla VIN {vehicle.Vin}: {string.Join(", ", result.SelectedIds)}.");
        RefreshBindings();
    }

    private void StartSale(object? sender, EventArgs e)
    {
        var vehicle = SelectedVehicle();
        var customer = FirstCustomer();
        var salesperson = FirstSalesperson();
        if (vehicle is null || customer is null || salesperson is null) { MessageBox.Show("Brakuje danych do sprzedaży."); return; }
        var result = ValidateSelectedOptions(vehicle);
        var optionCost = result.SelectedIds.Select(id => _store.Data.Options.First(o => o.Id == id).Price).Sum();
        var finalPrice = CalculatePrice(vehicle).Amount + optionCost;
        try
        {
            _sales.ReserveAndStartSale(vehicle, customer, salesperson, SelectedFinancing(), finalPrice);
            RefreshBindings();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private void AdvanceSale(object? sender, EventArgs e)
    {
        var transaction = SelectedTransaction();
        if (transaction is null) return;
        var vehicle = _store.Data.Vehicles.First(v => v.Vin == transaction.VehicleVin);
        var employee = _store.Data.Employees.First(e => e.Id == transaction.SalespersonId);
        var command = new AdvanceTransactionCommand(transaction, vehicle, employee, "Przejście do następnego etapu");
        command.Execute();
        if (transaction.Stage == TransactionStage.Released)
        {
            var customer = _store.Data.Customers.First(c => c.Id == transaction.CustomerId);
            customer.PurchaseHistory.Add($"{DateTime.Now:d}: {vehicle.Brand} {vehicle.Model}, VIN {vehicle.Vin}, {transaction.FinalPrice:C0}");
        }
        RefreshBindings();
    }

    private void WithdrawSale(object? sender, EventArgs e)
    {
        var transaction = SelectedTransaction();
        if (transaction is null) return;
        _sales.Withdraw(transaction);
        RefreshBindings();
    }

    private void SimulateDelivery(object? sender, EventArgs e)
    {
        var vehicle = _store.Data.Vehicles.FirstOrDefault(v => v.StateName == "W transporcie");
        if (vehicle is null) { MessageBox.Show("Brak auta w transporcie."); return; }
        vehicle.Availability = VehicleAvailability.InShowroom;
        vehicle.StateName = "Na ekspozycji";
        _notifier.Publish($"Dostawa do salonu: {vehicle.Brand} {vehicle.Model}, VIN {vehicle.Vin}.");
        RefreshBindings();
    }

    private void RecalculateConfiguration()
    {
        var vehicle = SelectedVehicle();
        if (vehicle is null || _pricingBox is null) return;
        var result = ValidateSelectedOptions(vehicle);
        var pricing = CalculatePrice(vehicle);
        var optionCost = result.SelectedIds.Select(id => _store.Data.Options.First(o => o.Id == id).Price).Sum();
        _pricingBox.Text =
            $"Auto: {vehicle.Brand} {vehicle.Model}, VIN {vehicle.Vin}\r\n" +
            $"Opcje po walidacji: {string.Join(", ", result.SelectedIds.DefaultIfEmpty("brak"))}\r\n" +
            $"Koszt opcji: {optionCost:C0}\r\n\r\n" +
            $"{pricing.Description}\r\n\r\nCena końcowa: {pricing.Amount + optionCost:C0}\r\n\r\n" +
            $"Reguły konfiguratora:\r\n{string.Join("\r\n", result.Messages.DefaultIfEmpty("Brak konfliktów."))}";
    }

    private OptionResult ValidateSelectedOptions(Vehicle vehicle)
    {
        var selected = _optionList?.CheckedItems.Cast<CarOption>().Select(o => o.Id) ?? vehicle.SelectedOptionIds;
        return new OptionDependencyMediator(_store.Data.Options).Normalize(vehicle, selected);
    }

    private PricingResult CalculatePrice(Vehicle vehicle)
    {
        IPriceComponent price = new BaseVehiclePrice(vehicle.BasePrice);
        price = new MarginDecorator(price);
        price = new SeasonalPromotionDecorator(price);
        price = new FleetDiscountDecorator(price, _fleetBox?.Checked == true);
        if (_insuranceBox?.Checked == true) price = new InsuranceDecorator(price);
        if (_warrantyBox?.Checked == true) price = new ExtendedWarrantyDecorator(price);
        IFinancingStrategy strategy = SelectedFinancing() switch
        {
            FinancingKind.Leasing => new LeasingStrategy(),
            FinancingKind.Credit => new CreditStrategy(),
            _ => new CashStrategy()
        };
        var result = strategy.Calculate(price.Calculate());
        return new PricingResult(result.Amount, $"{price.Describe()}\r\n{strategy.Name}: {result.Description}");
    }

    private static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false
    };

    private void LocalizeGrid(DataGridView grid, Dictionary<string, string> headers, params string[] hiddenColumns)
    {
        grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (headers.TryGetValue(column.DataPropertyName, out var header)) column.HeaderText = header;
                if (hiddenColumns.Contains(column.DataPropertyName)) column.Visible = false;
            }
        };
        grid.CellFormatting += (_, e) =>
        {
            if (e.Value is null || e.ColumnIndex < 0) return;
            var propertyName = grid.Columns[e.ColumnIndex].DataPropertyName;
            var localized = LocalizeCellValue(propertyName, e.Value);
            if (localized is null) return;
            e.Value = localized;
            e.FormattingApplied = true;
        };
    }

    private static void SetColumnWidths(DataGridView grid, Dictionary<string, int> widths)
    {
        grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!widths.TryGetValue(column.DataPropertyName, out var width)) continue;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                column.Width = width;
            }
        };
    }

    private string? LocalizeCellValue(string propertyName, object value) => propertyName switch
    {
        "Engine" when value is EngineType engine => engine switch
        {
            EngineType.Petrol => "Benzynowy",
            EngineType.Diesel => "Diesel",
            EngineType.Hybrid => "Hybrydowy",
            EngineType.Electric => "Elektryczny",
            _ => value.ToString()
        },
        "Gearbox" when value is Gearbox gearbox => gearbox switch
        {
            Gearbox.Manual => "Manualna",
            Gearbox.Automatic => "Automatyczna",
            _ => value.ToString()
        },
        "Availability" when value is VehicleAvailability availability => availability switch
        {
            VehicleAvailability.InShowroom => "W salonie",
            VehicleAvailability.OnOrder => "Na zamówienie",
            _ => value.ToString()
        },
        "Role" when value is EmployeeRole role => role switch
        {
            EmployeeRole.Salesperson => "Handlowiec",
            EmployeeRole.Manager => "Manager",
            EmployeeRole.ServiceTechnician => "Serwisant",
            _ => value.ToString()
        },
        "Financing" when value is FinancingKind financing => financing switch
        {
            FinancingKind.Cash => "Gotówka",
            FinancingKind.Leasing => "Leasing",
            FinancingKind.Credit => "Kredyt",
            _ => value.ToString()
        },
        "Stage" when value is TransactionStage stage => stage switch
        {
            TransactionStage.Reserved => "Rezerwacja",
            TransactionStage.CreditVerification => "Weryfikacja kredytowa",
            TransactionStage.Insurance => "Ubezpieczenie",
            TransactionStage.ReadyToRelease => "Gotowe do wydania",
            TransactionStage.Released => "Wydane",
            TransactionStage.Withdrawn => "Wycofane",
            _ => value.ToString()
        },
        "CustomerId" when value is Guid customerId => _store.Data.Customers.FirstOrDefault(c => c.Id == customerId)?.Name ?? "Nieznany klient",
        "SalespersonId" when value is Guid salespersonId => _store.Data.Employees.FirstOrDefault(e => e.Id == salespersonId)?.Name ?? "Nieznany handlowiec",
        _ => null
    };

    private static FlowLayoutPanel TopPanel() => new()
    {
        Dock = DockStyle.Top,
        Height = 46,
        Padding = new Padding(8),
        FlowDirection = FlowDirection.LeftToRight
    };

    private static Button Button(string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 30, Margin = new Padding(4) };
        button.Click += click;
        return button;
    }

    private static CheckBox Check(string text, Action changed, bool value = false)
    {
        var box = new CheckBox { Text = text, Checked = value, AutoSize = true, Padding = new Padding(8, 5, 0, 0) };
        box.CheckedChanged += (_, _) => changed();
        return box;
    }

    private FinancingKind SelectedFinancing() =>
        _financeBox?.SelectedItem is FinancingKind kind ? kind : FinancingKind.Cash;

    private void Save(object? sender, EventArgs e)
    {
        _store.Save();
        MessageBox.Show($"Zapisano dane do pliku:\r\n{_store.FilePath}", "Cars4Us");
    }

    private sealed class NotificationRow
    {
        public string Message { get; set; } = "";
    }
}
