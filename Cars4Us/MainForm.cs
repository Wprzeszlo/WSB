using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Windows.Forms;

namespace Cars4Us;

public sealed class MainForm : Form
{
    private static readonly Color ThemeInk = Color.FromArgb(25, 47, 56);
    private static readonly Color ThemeInkLight = Color.FromArgb(34, 62, 72);
    private static readonly Color ThemeCream = Color.FromArgb(247, 238, 219);
    private static readonly Color ThemeGold = Color.FromArgb(174, 148, 103);
    private static readonly Color ThemeGoldSoft = Color.FromArgb(218, 199, 160);
    private static readonly Color ThemeGridLine = Color.FromArgb(82, 101, 107);
    private static readonly Color ThemeSelection = Color.FromArgb(117, 99, 64);
    private static readonly Size ToolbarButtonSize = new(180, 52);

    private readonly JsonDataStore _store;
    private readonly InventoryNotifier _notifier = new();
    private readonly SalesFacade _sales;
    private readonly BindingSource _vehicles = new();
    private readonly BindingSource _customers = new();
    private readonly BindingSource _employees = new();
    private readonly BindingSource _testDrives = new();
    private readonly BindingSource _transactions = new();
    private readonly BindingSource _notifications = new();
    private readonly BindingSource _deletedRecords = new();
    private DataGridView _vehicleGrid = null!;
    private DataGridView _customerGrid = null!;
    private DataGridView _testDriveGrid = null!;
    private DataGridView _transactionGrid = null!;
    private DataGridView _employeeGrid = null!;
    private DataGridView _notificationGrid = null!;
    private DataGridView _deletedRecordGrid = null!;
    private CheckedListBox _optionCatalog = null!;
    private TextBox _pricingBox = null!;
    private ComboBox _catalogTransportRoute = null!;
    private NumericUpDown _catalogTransportDistance = null!;

    public MainForm(JsonDataStore store)
    {
        _store = store;
        _notifier.Subscribe(new NotificationLogObserver(_store.Data));
        _sales = new SalesFacade(_store.Data, _notifier);
        Text = "Cars4Us - salon samochodowy";
        Width = 1180;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        BackColor = ThemeCream;
        ForeColor = ThemeInk;
        BuildUi();
        ApplyTheme(this);
        ConfigurePolishTables();
        RefreshBindings();
        Shown += (_, _) => ResizeWindowToContent();
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
            ["IsTestDriveCar"] = "Auto testowe"
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
            ["IsTestDriveCar"] = 120
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
            ["SelectedOptionIds"] = "Usługi",
            ["TransportDistanceKm"] = "Dystans lawety",
            ["TransportRouteKind"] = "Trasa lawety",
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
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(210, 32),
            SizeMode = TabSizeMode.Fixed
        };
        tabs.DrawItem += DrawThemeTab;
        tabs.TabPages.Add(BuildVehiclesTab());
        tabs.TabPages.Add(BuildCrmTab());
        tabs.TabPages.Add(BuildOptionsTab());
        tabs.TabPages.Add(BuildTestDriveTab());
        tabs.TabPages.Add(BuildSalesTab());
        tabs.TabPages.Add(BuildStaffTab());
        tabs.TabPages.Add(BuildRecycleBinTab());
        Controls.Add(tabs);
    }

    private static void DrawThemeTab(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tabs || e.Index < 0) return;
        var selected = tabs.SelectedIndex == e.Index;
        using var background = new SolidBrush(selected ? ThemeCream : ThemeInk);
        e.Graphics.FillRectangle(background, e.Bounds);
        var textColor = selected ? ThemeInk : ThemeCream;
        TextRenderer.DrawText(
            e.Graphics,
            tabs.TabPages[e.Index].Text,
            tabs.Font,
            e.Bounds,
            textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private TabPage BuildVehiclesTab()
    {
        var page = new TabPage("Pojazdy");
        _vehicleGrid = Grid();
        _vehicleGrid.DataSource = _vehicles;
        var panel = TopPanel();
        panel.Controls.Add(Button("Dodaj auto", AddVehicle));
        panel.Controls.Add(Button("Modyfikuj", EditVehicle));
        panel.Controls.Add(Button("Usuń", DeleteVehicle));
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
        panel.Controls.Add(Button("Dodaj", AddCustomer));
        panel.Controls.Add(Button("Modyfikuj", EditCustomer));
        panel.Controls.Add(Button("Usuń", DeleteCustomer));
        panel.Controls.Add(Button("Historia", ShowCustomerHistory));
        panel.Controls.Add(Button("Zapisz", Save));
        page.Controls.Add(_customerGrid);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildOptionsTab()
    {
        var page = new TabPage("Pakiet usług");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 380 };
        _optionCatalog = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
        _optionCatalog.ItemCheck += (_, _) => BeginInvoke((Action)ShowServiceCatalogInfo);
        _pricingBox = new TextBox { Multiline = true, Dock = DockStyle.Fill, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        _catalogTransportRoute = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230, DataSource = Enum.GetValues(typeof(TransportRouteKind)), FormattingEnabled = true };
        _catalogTransportRoute.Format += (_, e) => { if (e.ListItem is TransportRouteKind route) e.Value = TransportPricing.Describe(route); };
        _catalogTransportRoute.SelectedIndexChanged += (_, _) => ShowServiceCatalogInfo();
        _catalogTransportDistance = new NumericUpDown { Minimum = 1, Maximum = 5000, Value = 100, Width = 90 };
        _catalogTransportDistance.ValueChanged += (_, _) =>
        {
            EnsureCatalogTransportRouteMatchesDistance();
            ShowServiceCatalogInfo();
        };
        var transportPanel = TopPanel();
        transportPanel.Controls.Add(new Label { Text = "Trasa lawety:", AutoSize = true, Padding = new Padding(8, 8, 0, 0) });
        transportPanel.Controls.Add(_catalogTransportRoute);
        transportPanel.Controls.Add(new Label { Text = "Dystans (km):", AutoSize = true, Padding = new Padding(8, 8, 0, 0) });
        transportPanel.Controls.Add(_catalogTransportDistance);
        split.Panel1.Controls.Add(_optionCatalog);
        split.Panel1.Controls.Add(transportPanel);
        split.Panel2.Controls.Add(_pricingBox);
        page.Controls.Add(split);
        return page;
    }

    private TabPage BuildTestDriveTab()
    {
        var page = new TabPage("Jazdy próbne");
        _testDriveGrid = Grid();
        _testDriveGrid.DataSource = _testDrives;
        var panel = TopPanel();
        panel.Controls.Add(Button("Zarezerwuj", AddTestDrive));
        panel.Controls.Add(Button("Modyfikuj", EditTestDrive));
        panel.Controls.Add(Button("Usuń", DeleteTestDrive));
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
        panel.Controls.Add(Button("Rozpocznij", StartSale));
        panel.Controls.Add(Button("Modyfikuj", EditSale));
        panel.Controls.Add(Button("Etap", AdvanceSale));
        panel.Controls.Add(Button("Dokument", GenerateReleaseDocument));
        panel.Controls.Add(Button("Wycofaj", WithdrawSale));
        panel.Controls.Add(Button("Usuń", DeleteSale));
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
        panel.Controls.Add(Button("Dodaj", AddEmployee));
        panel.Controls.Add(Button("Modyfikuj", EditEmployee));
        panel.Controls.Add(Button("Usuń", DeleteEmployee));
        panel.Controls.Add(Button("Zapisz", Save));
        page.Controls.Add(split);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildRecycleBinTab()
    {
        var page = new TabPage("Kosz");
        _deletedRecordGrid = Grid();
        _deletedRecordGrid.DataSource = _deletedRecords;
        LocalizeGrid(_deletedRecordGrid, new()
        {
            ["EntityType"] = "Typ danych",
            ["DisplayName"] = "Nazwa",
            ["DeletedAt"] = "Usunięto",
            ["RestoreUntil"] = "Możliwe przywrócenie do",
            ["DependenciesInfo"] = "Powiązania"
        }, "Id", "PayloadJson");
        var panel = TopPanel();
        panel.Controls.Add(Button("Przywróć", RestoreDeletedRecord));
        panel.Controls.Add(Button("Wyczyść", PurgeExpiredDeletedRecords));
        panel.Controls.Add(Button("Zapisz", Save));
        page.Controls.Add(_deletedRecordGrid);
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
        _deletedRecords.DataSource = new BindingList<DeletedRecord>(_store.Data.DeletedRecords);
        _optionCatalog.Items.Clear();
        foreach (var option in _store.Data.Options) _optionCatalog.Items.Add(option, false);
        ShowServiceCatalogInfo();
    }

    private void ResizeWindowToContent()
    {
        var grids = new[] { _vehicleGrid, _customerGrid, _testDriveGrid, _transactionGrid, _employeeGrid, _notificationGrid, _deletedRecordGrid };
        var widestGrid = grids.Where(grid => grid is not null).Select(PreferredGridWidth).DefaultIfEmpty(Width).Max();
        var widestToolbar = Controls.OfType<TabControl>()
            .SelectMany(tab => tab.TabPages.Cast<TabPage>())
            .SelectMany(page => page.Controls.OfType<FlowLayoutPanel>())
            .Select(panel => panel.PreferredSize.Width + 48)
            .DefaultIfEmpty(Width)
            .Max();
        var tabHeadersWidth = Controls.OfType<TabControl>().FirstOrDefault()?.TabPages.Cast<TabPage>().Sum(page => TextRenderer.MeasureText(page.Text, Font).Width + 32) ?? 0;

        var workingArea = Screen.FromControl(this).WorkingArea;
        var desiredWidth = Math.Max(1180, Math.Max(widestGrid + 48, Math.Max(widestToolbar + 24, tabHeadersWidth + 72)));
        var desiredHeight = Math.Max(760, PreferredGridHeight(_vehicleGrid) + 150);
        Width = Math.Min(desiredWidth, workingArea.Width - 40);
        Height = Math.Min(desiredHeight, workingArea.Height - 40);
        CenterToScreen();
    }

    private static int PreferredGridWidth(DataGridView grid)
    {
        var columnsWidth = grid.Columns.Cast<DataGridViewColumn>().Where(column => column.Visible).Sum(column => column.Width);
        return grid.RowHeadersWidth + columnsWidth + SystemInformation.VerticalScrollBarWidth + 24;
    }

    private static int PreferredGridHeight(DataGridView grid)
    {
        var visibleRows = Math.Min(Math.Max(grid.Rows.Count, 4), 12);
        return grid.ColumnHeadersHeight + visibleRows * grid.RowTemplate.Height + SystemInformation.HorizontalScrollBarHeight + 24;
    }

    private Vehicle? SelectedVehicle() => _vehicleGrid.CurrentRow?.DataBoundItem as Vehicle ?? _store.Data.Vehicles.FirstOrDefault(v => VehicleStateFactory.From(v.StateName).CanReserve);
    private SaleTransaction? SelectedTransaction() => (_transactions.Current as SaleTransaction) ?? _store.Data.Transactions.LastOrDefault();
    private DeletedRecord? SelectedDeletedRecord() => _deletedRecords.Current as DeletedRecord;
    private Customer? FirstCustomer() => _store.Data.Customers.FirstOrDefault();
    private Employee? FirstSalesperson() => _store.Data.Employees.FirstOrDefault(e => e.Role == EmployeeRole.Salesperson);

    private void AddVehicle(object? sender, EventArgs e)
    {
        using var dialog = new VehicleEditorDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (_store.Data.Vehicles.Any(v => v.Vin.Equals(dialog.Vehicle.Vin, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Pojazd o podanym VIN już istnieje.", "Cars4Us");
            return;
        }
        _store.Data.Vehicles.Add(dialog.Vehicle);
        _notifier.Publish($"Dodano pojazd: {dialog.Vehicle.Brand} {dialog.Vehicle.Model}, VIN {dialog.Vehicle.Vin}.");
        RefreshBindings();
    }

    private void EditVehicle(object? sender, EventArgs e)
    {
        var vehicle = SelectedVehicle();
        if (vehicle is null) return;
        using var dialog = new VehicleEditorDialog(vehicle);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var updated = dialog.Vehicle;
        if (_store.Data.Vehicles.Any(v => !ReferenceEquals(v, vehicle) && v.Vin.Equals(updated.Vin, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Pojazd o podanym VIN już istnieje.", "Cars4Us");
            return;
        }
        if (!ConfirmModification("Modyfikacja pojazdu", vehicle.ToString())) return;

        var oldVin = vehicle.Vin;
        vehicle.Vin = updated.Vin;
        vehicle.Brand = updated.Brand;
        vehicle.Model = updated.Model;
        vehicle.Engine = updated.Engine;
        vehicle.Gearbox = updated.Gearbox;
        vehicle.Mileage = updated.Mileage;
        vehicle.BasePrice = updated.BasePrice;
        vehicle.Availability = updated.Availability;
        vehicle.IsTestDriveCar = updated.IsTestDriveCar;
        foreach (var drive in _store.Data.TestDrives.Where(d => d.VehicleVin == oldVin)) drive.VehicleVin = vehicle.Vin;
        foreach (var sale in _store.Data.Transactions.Where(t => t.VehicleVin == oldVin)) sale.VehicleVin = vehicle.Vin;
        _notifier.Publish($"Zmodyfikowano pojazd: {vehicle.Brand} {vehicle.Model}, VIN {vehicle.Vin}.");
        RefreshBindings();
    }

    private void DeleteVehicle(object? sender, EventArgs e)
    {
        var vehicle = SelectedVehicle();
        if (vehicle is null) return;
        var relatedDrives = _store.Data.TestDrives.Where(d => d.VehicleVin == vehicle.Vin).ToList();
        var relatedSales = _store.Data.Transactions.Where(t => t.VehicleVin == vehicle.Vin).ToList();
        var dependencies = new List<string>();
        dependencies.AddRange(relatedDrives.Select(d => $"Jazda próbna: {d.Start:g}"));
        dependencies.AddRange(relatedSales.Select(t => $"Transakcja: {t.Stage}, {t.FinalPrice:C0}"));
        if (!ConfirmDelete("Usuwanie pojazdu", vehicle.ToString(), dependencies)) return;

        foreach (var drive in relatedDrives) MoveToRecycleBin("Jazda próbna", $"VIN {drive.VehicleVin}, {drive.Start:g}", drive, "Usunięto razem z pojazdem.");
        foreach (var sale in relatedSales) MoveToRecycleBin("Transakcja", $"VIN {sale.VehicleVin}, {sale.CreatedAt:g}", sale, "Usunięto razem z pojazdem.");
        MoveToRecycleBin("Pojazd", vehicle.ToString(), vehicle, string.Join("; ", dependencies));
        _store.Data.TestDrives.RemoveAll(d => d.VehicleVin == vehicle.Vin);
        _store.Data.Transactions.RemoveAll(t => t.VehicleVin == vehicle.Vin);
        _store.Data.Vehicles.Remove(vehicle);
        RefreshBindings();
    }

    private void AddCustomer(object? sender, EventArgs e)
    {
        using var dialog = new CustomerEditorDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _store.Data.Customers.Add(dialog.Customer);
        _notifier.Publish($"Dodano klienta: {dialog.Customer.Name}.");
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

    private void EditCustomer(object? sender, EventArgs e)
    {
        if (_customers.Current is not Customer customer) return;
        using var dialog = new CustomerEditorDialog(customer);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!ConfirmModification("Modyfikacja klienta", customer.ToString())) return;
        var updated = dialog.Customer;
        customer.Name = updated.Name;
        customer.Phone = updated.Phone;
        customer.Email = updated.Email;
        _notifier.Publish($"Zmodyfikowano klienta: {customer.Name}.");
        RefreshBindings();
    }

    private void DeleteCustomer(object? sender, EventArgs e)
    {
        if (_customers.Current is not Customer customer) return;
        var relatedDrives = _store.Data.TestDrives.Where(d => d.CustomerId == customer.Id).ToList();
        var relatedSales = _store.Data.Transactions.Where(t => t.CustomerId == customer.Id).ToList();
        var dependencies = new List<string>();
        dependencies.AddRange(relatedDrives.Select(d => $"Jazda próbna: VIN {d.VehicleVin}, {d.Start:g}"));
        dependencies.AddRange(relatedSales.Select(t => $"Transakcja: VIN {t.VehicleVin}, {t.FinalPrice:C0}"));
        if (!ConfirmDelete("Usuwanie klienta", customer.ToString(), dependencies)) return;

        foreach (var drive in relatedDrives) MoveToRecycleBin("Jazda próbna", $"VIN {drive.VehicleVin}, {drive.Start:g}", drive, "Usunięto razem z klientem.");
        foreach (var sale in relatedSales) MoveToRecycleBin("Transakcja", $"VIN {sale.VehicleVin}, {sale.CreatedAt:g}", sale, "Usunięto razem z klientem.");
        MoveToRecycleBin("Klient", customer.ToString(), customer, string.Join("; ", dependencies));
        _store.Data.TestDrives.RemoveAll(d => d.CustomerId == customer.Id);
        _store.Data.Transactions.RemoveAll(t => t.CustomerId == customer.Id);
        _store.Data.Customers.Remove(customer);
        RefreshBindings();
    }

    private void AddEmployee(object? sender, EventArgs e)
    {
        using var dialog = new EmployeeEditorDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _store.Data.Employees.Add(dialog.Employee);
        _notifier.Publish($"Dodano pracownika: {dialog.Employee.Name}.");
        RefreshBindings();
    }

    private void EditEmployee(object? sender, EventArgs e)
    {
        if (_employees.Current is not Employee employee) return;
        using var dialog = new EmployeeEditorDialog(employee);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!ConfirmModification("Modyfikacja pracownika", employee.ToString())) return;
        var updated = dialog.Employee;
        employee.Name = updated.Name;
        employee.Role = updated.Role;
        _notifier.Publish($"Zmodyfikowano pracownika: {employee.Name}.");
        RefreshBindings();
    }

    private void DeleteEmployee(object? sender, EventArgs e)
    {
        if (_employees.Current is not Employee employee) return;
        var relatedDrives = _store.Data.TestDrives.Where(d => d.SalespersonId == employee.Id).ToList();
        var relatedSales = _store.Data.Transactions.Where(t => t.SalespersonId == employee.Id).ToList();
        var dependencies = new List<string>();
        dependencies.AddRange(relatedDrives.Select(d => $"Jazda próbna: VIN {d.VehicleVin}, {d.Start:g}"));
        dependencies.AddRange(relatedSales.Select(t => $"Transakcja: VIN {t.VehicleVin}, {t.FinalPrice:C0}"));
        if (!ConfirmDelete("Usuwanie pracownika", employee.ToString(), dependencies)) return;

        foreach (var drive in relatedDrives) MoveToRecycleBin("Jazda próbna", $"VIN {drive.VehicleVin}, {drive.Start:g}", drive, "Usunięto razem z pracownikiem.");
        foreach (var sale in relatedSales) MoveToRecycleBin("Transakcja", $"VIN {sale.VehicleVin}, {sale.CreatedAt:g}", sale, "Usunięto razem z pracownikiem.");
        MoveToRecycleBin("Pracownik", employee.ToString(), employee, string.Join("; ", dependencies));
        _store.Data.TestDrives.RemoveAll(d => d.SalespersonId == employee.Id);
        _store.Data.Transactions.RemoveAll(t => t.SalespersonId == employee.Id);
        _store.Data.Employees.Remove(employee);
        RefreshBindings();
    }

    private void AddTestDrive(object? sender, EventArgs e)
    {
        using var dialog = new TestDriveEditorDialog(_store.Data.Vehicles, _store.Data.Customers, _store.Data.Employees);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var drive = dialog.TestDrive;
        var conflict = _store.Data.TestDrives.Any(d => d.VehicleVin == drive.VehicleVin && drive.Start < d.End && drive.End > d.Start);
        if (conflict) { MessageBox.Show("Wybrany termin koliduje z istniejącą jazdą próbną."); return; }
        var customer = _store.Data.Customers.First(c => c.Id == drive.CustomerId);
        _store.Data.TestDrives.Add(drive);
        _notifier.Publish($"Zaplanowano jazdę próbną VIN {drive.VehicleVin} dla {customer.Name}.");
        RefreshBindings();
    }

    private void DeleteTestDrive(object? sender, EventArgs e)
    {
        if (_testDrives.Current is TestDrive drive)
        {
            var dependencies = new[] { $"Klient: {CustomerName(drive.CustomerId)}", $"Handlowiec: {EmployeeName(drive.SalespersonId)}", $"Pojazd: {drive.VehicleVin}" };
            if (!ConfirmDelete("Usuwanie jazdy próbnej", $"VIN {drive.VehicleVin}, {drive.Start:g}", dependencies)) return;
            MoveToRecycleBin("Jazda próbna", $"VIN {drive.VehicleVin}, {drive.Start:g}", drive, string.Join("; ", dependencies));
            _store.Data.TestDrives.Remove(drive);
            _notifier.Publish($"Anulowano jazdę próbną VIN {drive.VehicleVin}.");
            RefreshBindings();
        }
    }

    private void EditTestDrive(object? sender, EventArgs e)
    {
        if (_testDrives.Current is not TestDrive drive) return;
        using var dialog = new TestDriveEditorDialog(_store.Data.Vehicles, _store.Data.Customers, _store.Data.Employees, drive);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var updated = dialog.TestDrive;
        var conflict = _store.Data.TestDrives.Any(d => d.Id != drive.Id && d.VehicleVin == updated.VehicleVin && updated.Start < d.End && updated.End > d.Start);
        if (conflict)
        {
            MessageBox.Show("Wybrany termin koliduje z istniejącą jazdą próbną.", "Cars4Us");
            return;
        }
        if (!ConfirmModification("Modyfikacja jazdy próbnej", $"VIN {drive.VehicleVin}, {drive.Start:g}")) return;
        drive.VehicleVin = updated.VehicleVin;
        drive.CustomerId = updated.CustomerId;
        drive.SalespersonId = updated.SalespersonId;
        drive.Start = updated.Start;
        drive.End = updated.End;
        drive.Notes = updated.Notes;
        _notifier.Publish($"Zmodyfikowano jazdę próbną VIN {drive.VehicleVin}.");
        RefreshBindings();
    }

    private void StartSale(object? sender, EventArgs e)
    {
        using var dialog = new SaleEditorDialog(_store.Data.Vehicles, _store.Data.Customers, _store.Data.Employees, _store.Data.Options);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var vehicle = dialog.Vehicle;
        var customer = dialog.Customer;
        var salesperson = dialog.Salesperson;
        var result = ValidateServiceOptions(vehicle, dialog.SelectedOptionIds);
        ShowServiceRuleMessages(result);
        var optionCost = CalculateServiceCost(result.SelectedIds, dialog.TransportDistanceKm, dialog.TransportRouteKind);
        var finalPrice = CalculatePrice(vehicle, dialog.Financing).Amount + optionCost;
        try
        {
            var transaction = _sales.ReserveAndStartSale(vehicle, customer, salesperson, dialog.Financing, result.SelectedIds, finalPrice);
            transaction.TransportDistanceKm = result.SelectedIds.Contains(TransportPricing.OptionId) ? dialog.TransportDistanceKm : 0;
            transaction.TransportRouteKind = dialog.TransportRouteKind;
            transaction.History.Add(new TransactionSnapshot
            {
                Stage = transaction.Stage,
                VehicleState = vehicle.StateName,
                Description = $"Wybrane usługi: {ServiceNames(result.SelectedIds)}. {TransportDescription(result.SelectedIds, transaction.TransportDistanceKm, transaction.TransportRouteKind)}"
            });
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

    private void GenerateReleaseDocument(object? sender, EventArgs e)
    {
        var transaction = SelectedTransaction();
        if (transaction is null) return;
        if (transaction.Stage != TransactionStage.Released)
        {
            MessageBox.Show("Dokument wydania można wygenerować dopiero dla transakcji na etapie „Wydane”.", "Cars4Us");
            return;
        }

        var vehicle = _store.Data.Vehicles.FirstOrDefault(v => v.Vin == transaction.VehicleVin);
        var customer = _store.Data.Customers.FirstOrDefault(c => c.Id == transaction.CustomerId);
        var salesperson = _store.Data.Employees.FirstOrDefault(emp => emp.Id == transaction.SalespersonId);
        if (vehicle is null || customer is null || salesperson is null)
        {
            MessageBox.Show("Brakuje danych pojazdu, klienta lub handlowca dla tej transakcji.", "Cars4Us");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Zapisz dokument wydania",
            Filter = "Dokument HTML (*.html)|*.html",
            FileName = $"Dokument_wydania_{vehicle.Vin}_{DateTime.Now:yyyyMMdd}.html"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        File.WriteAllText(dialog.FileName, BuildReleaseDocumentHtml(transaction, vehicle, customer, salesperson));
        _notifier.Publish($"Wygenerowano dokument wydania dla VIN {vehicle.Vin}.");
        RefreshBindings();

        if (MessageBox.Show("Dokument został zapisany. Otworzyć go teraz?", "Cars4Us", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
    }

    private void DeleteSale(object? sender, EventArgs e)
    {
        var transaction = SelectedTransaction();
        if (transaction is null) return;
        var dependencies = new[]
        {
            $"Pojazd: {transaction.VehicleVin}",
            $"Klient: {CustomerName(transaction.CustomerId)}",
            $"Handlowiec: {EmployeeName(transaction.SalespersonId)}"
        };
        if (!ConfirmDelete("Usuwanie transakcji", $"VIN {transaction.VehicleVin}, {transaction.CreatedAt:g}", dependencies)) return;
        MoveToRecycleBin("Transakcja", $"VIN {transaction.VehicleVin}, {transaction.CreatedAt:g}", transaction, string.Join("; ", dependencies));
        _store.Data.Transactions.Remove(transaction);
        RefreshBindings();
    }

    private void EditSale(object? sender, EventArgs e)
    {
        var transaction = SelectedTransaction();
        if (transaction is null) return;
        using var dialog = new SaleEditorDialog(_store.Data.Vehicles, _store.Data.Customers, _store.Data.Employees, _store.Data.Options, transaction);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!ConfirmModification("Modyfikacja transakcji", $"VIN {transaction.VehicleVin}, {transaction.CreatedAt:g}")) return;

        if (transaction.VehicleVin != dialog.Vehicle.Vin)
        {
            var oldVehicle = _store.Data.Vehicles.FirstOrDefault(v => v.Vin == transaction.VehicleVin);
            if (oldVehicle is not null) oldVehicle.StateName = "Na ekspozycji";
            dialog.Vehicle.StateName = "Zarezerwowane";
        }

        transaction.VehicleVin = dialog.Vehicle.Vin;
        transaction.CustomerId = dialog.Customer.Id;
        transaction.SalespersonId = dialog.Salesperson.Id;
        transaction.Financing = dialog.Financing;
        var result = ValidateServiceOptions(dialog.Vehicle, dialog.SelectedOptionIds);
        ShowServiceRuleMessages(result);
        var optionCost = CalculateServiceCost(result.SelectedIds, dialog.TransportDistanceKm, dialog.TransportRouteKind);
        transaction.SelectedOptionIds = result.SelectedIds;
        transaction.TransportDistanceKm = result.SelectedIds.Contains(TransportPricing.OptionId) ? dialog.TransportDistanceKm : 0;
        transaction.TransportRouteKind = dialog.TransportRouteKind;
        transaction.FinalPrice = CalculatePrice(dialog.Vehicle, dialog.Financing).Amount + optionCost;
        transaction.History.Add(new TransactionSnapshot
        {
            Stage = transaction.Stage,
            VehicleState = dialog.Vehicle.StateName,
            Description = $"Modyfikacja danych transakcji. Usługi: {ServiceNames(result.SelectedIds)}. {TransportDescription(result.SelectedIds, transaction.TransportDistanceKm, transaction.TransportRouteKind)}"
        });
        _notifier.Publish($"Zmodyfikowano transakcję VIN {transaction.VehicleVin}.");
        RefreshBindings();
    }

    private void RestoreDeletedRecord(object? sender, EventArgs e)
    {
        var record = SelectedDeletedRecord();
        if (record is null) return;
        if (record.RestoreUntil < DateTime.Now)
        {
            MessageBox.Show("Tego rekordu nie można już przywrócić, ponieważ minęło 31 dni.", "Cars4Us");
            return;
        }

        try
        {
            RestoreRecord(record);
            _store.Data.DeletedRecords.Remove(record);
            _notifier.Publish($"Przywrócono dane z kosza: {record.DisplayName}.");
            RefreshBindings();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie udało się przywrócić rekordu:\r\n{ex.Message}", "Cars4Us");
        }
    }

    private void PurgeExpiredDeletedRecords(object? sender, EventArgs e)
    {
        var removed = _store.Data.DeletedRecords.RemoveAll(record => record.RestoreUntil < DateTime.Now);
        MessageBox.Show($"Usunięto wygasłe rekordy z kosza: {removed}.", "Cars4Us");
        RefreshBindings();
    }

    private bool ConfirmDelete(string title, string displayName, IEnumerable<string> dependencies)
    {
        using var dialog = new DeleteConfirmationDialog(title, displayName, dependencies);
        return dialog.ShowDialog(this) == DialogResult.OK;
    }

    private bool ConfirmModification(string title, string displayName) =>
        MessageBox.Show(
            $"Wybrany rekord:\r\n{displayName}\r\n\r\nCzy na pewno zmodyfikować dane?",
            title,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;

    private void MoveToRecycleBin<T>(string entityType, string displayName, T payload, string dependenciesInfo)
    {
        _store.Data.DeletedRecords.Add(new DeletedRecord
        {
            EntityType = entityType,
            DisplayName = displayName,
            DeletedAt = DateTime.Now,
            PayloadJson = JsonSerializer.Serialize(payload, JsonDataStore.JsonOptions),
            DependenciesInfo = string.IsNullOrWhiteSpace(dependenciesInfo) ? "Brak powiązań" : dependenciesInfo
        });
    }

    private void RestoreRecord(DeletedRecord record)
    {
        switch (record.EntityType)
        {
            case "Pojazd":
                var vehicle = JsonSerializer.Deserialize<Vehicle>(record.PayloadJson, JsonDataStore.JsonOptions) ?? throw new InvalidOperationException("Brak danych pojazdu.");
                if (_store.Data.Vehicles.Any(v => v.Vin == vehicle.Vin)) throw new InvalidOperationException("Pojazd o tym VIN już istnieje.");
                _store.Data.Vehicles.Add(vehicle);
                break;
            case "Klient":
                var customer = JsonSerializer.Deserialize<Customer>(record.PayloadJson, JsonDataStore.JsonOptions) ?? throw new InvalidOperationException("Brak danych klienta.");
                if (_store.Data.Customers.Any(c => c.Id == customer.Id)) throw new InvalidOperationException("Klient już istnieje.");
                _store.Data.Customers.Add(customer);
                break;
            case "Pracownik":
                var employee = JsonSerializer.Deserialize<Employee>(record.PayloadJson, JsonDataStore.JsonOptions) ?? throw new InvalidOperationException("Brak danych pracownika.");
                if (_store.Data.Employees.Any(e => e.Id == employee.Id)) throw new InvalidOperationException("Pracownik już istnieje.");
                _store.Data.Employees.Add(employee);
                break;
            case "Jazda próbna":
                var drive = JsonSerializer.Deserialize<TestDrive>(record.PayloadJson, JsonDataStore.JsonOptions) ?? throw new InvalidOperationException("Brak danych jazdy próbnej.");
                if (!_store.Data.Vehicles.Any(v => v.Vin == drive.VehicleVin)) throw new InvalidOperationException("Najpierw przywróć powiązany pojazd.");
                if (!_store.Data.Customers.Any(c => c.Id == drive.CustomerId)) throw new InvalidOperationException("Najpierw przywróć powiązanego klienta.");
                if (!_store.Data.Employees.Any(e => e.Id == drive.SalespersonId)) throw new InvalidOperationException("Najpierw przywróć powiązanego handlowca.");
                if (_store.Data.TestDrives.Any(d => d.Id == drive.Id)) throw new InvalidOperationException("Jazda próbna już istnieje.");
                _store.Data.TestDrives.Add(drive);
                break;
            case "Transakcja":
                var sale = JsonSerializer.Deserialize<SaleTransaction>(record.PayloadJson, JsonDataStore.JsonOptions) ?? throw new InvalidOperationException("Brak danych transakcji.");
                if (!_store.Data.Vehicles.Any(v => v.Vin == sale.VehicleVin)) throw new InvalidOperationException("Najpierw przywróć powiązany pojazd.");
                if (!_store.Data.Customers.Any(c => c.Id == sale.CustomerId)) throw new InvalidOperationException("Najpierw przywróć powiązanego klienta.");
                if (!_store.Data.Employees.Any(e => e.Id == sale.SalespersonId)) throw new InvalidOperationException("Najpierw przywróć powiązanego handlowca.");
                if (_store.Data.Transactions.Any(t => t.Id == sale.Id)) throw new InvalidOperationException("Transakcja już istnieje.");
                _store.Data.Transactions.Add(sale);
                break;
            default:
                throw new InvalidOperationException("Nieznany typ danych w koszu.");
        }
    }

    private string CustomerName(Guid id) => _store.Data.Customers.FirstOrDefault(c => c.Id == id)?.Name ?? "brak";
    private string EmployeeName(Guid id) => _store.Data.Employees.FirstOrDefault(e => e.Id == id)?.Name ?? "brak";

    private void ShowServiceCatalogInfo()
    {
        if (_pricingBox is null) return;
        var selectedOptions = _optionCatalog?.CheckedItems.Cast<CarOption>().ToList() ?? new List<CarOption>();
        var vehicle = SelectedVehicle();
        var quote = vehicle is null
            ? "Wybierz pojazd w zakładce „Pojazdy”, aby zobaczyć poglądową cenę z usługą."
            : BuildInformationalServiceQuote(vehicle, selectedOptions);

        _pricingBox.Text =
            "Katalog usług old time\r\n\r\n" +
            "Ten widok jest wyłącznie informacyjny. Kwoty poniżej pomagają porównać usługi, ale nie są zapisywane i nie wpływają na proces sprzedaży.\r\n\r\n" +
            quote + "\r\n\r\n" +
            "Reguły przykładowe:\r\n" +
            "- Wymiana oleju i filtrów wymaga przeglądu klasyka.\r\n" +
            "- Przygotowanie do wystawy wymaga detailingu wnętrza i polerowania lakieru.\r\n" +
            "- Transport lawetą liczony jest według dystansu i typu trasy.";
    }

    private void EnsureCatalogTransportRouteMatchesDistance()
    {
        if (_catalogTransportRoute is null || _catalogTransportDistance is null) return;
        if (_catalogTransportDistance.Value > 300 && _catalogTransportRoute.SelectedItem is TransportRouteKind.PolandUpTo300Km)
            _catalogTransportRoute.SelectedItem = TransportRouteKind.PolandOver300Km;
    }

    private string BuildInformationalServiceQuote(Vehicle vehicle, List<CarOption> options)
    {
        var pricing = CalculatePrice(vehicle, FinancingKind.Cash);
        var selectedIds = options.Select(option => option.Id).ToList();
        var transportDistance = (int)(_catalogTransportDistance?.Value ?? 0);
        var transportRoute = _catalogTransportRoute?.SelectedItem is TransportRouteKind route ? route : TransportRouteKind.PolandUpTo300Km;
        var normalized = ValidateServiceOptions(vehicle, selectedIds);
        var optionCost = CalculateServiceCost(normalized.SelectedIds, transportDistance, transportRoute);
        var optionLine = options.Count == 0
            ? "Wybrana usługa: brak"
            : $"Wybrane usługi:\r\n- {string.Join("\r\n- ", options.Select(option => ServiceLine(option, transportDistance, transportRoute)))}\r\nKoszt usług: {optionCost:N2} zł";
        var dependencies = options.Count == 0
            ? ""
            : $"\r\nWymaga: {ServiceNames(options.SelectMany(option => option.Requires).Distinct())}\r\nWyklucza: {ServiceNames(options.SelectMany(option => option.Excludes).Distinct())}";
        var validation = options.Count == 0
            ? ""
            : $"\r\nUsługi po regułach: {ServiceNames(normalized.SelectedIds)}";

        return
            $"Auto: {vehicle.Brand} {vehicle.Model}, VIN {vehicle.Vin}\r\n" +
            optionLine +
            dependencies +
            validation + "\r\n\r\n" +
            $"Cena bazowa: {vehicle.BasePrice:N2} zł\r\n" +
            $"{pricing.Description}\r\n" +
            $"Poglądowa cena z usługami: {(pricing.Amount + optionCost):N2} zł";
    }

    private string BuildReleaseDocumentHtml(SaleTransaction transaction, Vehicle vehicle, Customer customer, Employee salesperson)
    {
        var documentNumber = $"DW/{transaction.CreatedAt:yyyy}/{transaction.Id.ToString()[..8].ToUpperInvariant()}";
        var servicesRows = BuildReleaseServiceRows(transaction);
        var html = """
            <!doctype html>
            <html lang="pl">
            <head>
              <meta charset="utf-8">
              <title>Dokument wydania @@DOCUMENT_NUMBER@@</title>
              <style>
                body { font-family: Segoe UI, Arial, sans-serif; margin: 38px; color: #193038; }
                h1 { margin: 0 0 4px; font-size: 28px; }
                .muted { color: #6f6758; }
                .header { display: flex; justify-content: space-between; border-bottom: 3px solid #ae9467; padding-bottom: 18px; margin-bottom: 24px; }
                .box { border: 1px solid #d2bd8e; padding: 14px; margin: 12px 0; }
                table { width: 100%; border-collapse: collapse; margin-top: 12px; }
                th, td { border: 1px solid #d2bd8e; padding: 9px; text-align: left; }
                th { background: #f7eedb; }
                .total { font-size: 22px; font-weight: 700; text-align: right; margin-top: 20px; }
                .signatures { display: grid; grid-template-columns: 1fr 1fr; gap: 80px; margin-top: 70px; }
                .signature { border-top: 1px solid #193038; text-align: center; padding-top: 8px; }
              </style>
            </head>
            <body>
              <div class="header">
                <div>
                  <h1>Dokument wydania pojazdu</h1>
                  <div class="muted">Numer: @@DOCUMENT_NUMBER@@</div>
                </div>
                <div>
                  <strong>Cars4Us</strong><br>
                  Salon samochodów old time<br>
                  Data dokumentu: @@DOCUMENT_DATE@@
                </div>
              </div>

              <div class="box">
                <strong>Firma wydająca:</strong><br>
                Cars4Us - salon samochodowy<br>
                Sprzedawca: @@SALESPERSON@@ (@@SALESPERSON_ROLE@@)
              </div>

              <div class="box">
                <strong>Kupujący:</strong><br>
                @@CUSTOMER_NAME@@<br>
                Telefon: @@CUSTOMER_PHONE@@<br>
                E-mail: @@CUSTOMER_EMAIL@@
              </div>

              <table>
                <tr><th colspan="2">Pojazd</th></tr>
                <tr><td>VIN</td><td>@@VIN@@</td></tr>
                <tr><td>Marka i model</td><td>@@VEHICLE_NAME@@</td></tr>
                <tr><td>Typ silnika</td><td>@@ENGINE@@</td></tr>
                <tr><td>Skrzynia biegów</td><td>@@GEARBOX@@</td></tr>
                <tr><td>Przebieg</td><td>@@MILEAGE@@ km</td></tr>
              </table>

              <table>
                <tr><th>Data sprzedaży</th><th>Data wydania</th><th>Finansowanie</th><th>Status</th></tr>
                <tr>
                  <td>@@SALE_DATE@@</td>
                  <td>@@RELEASE_DATE@@</td>
                  <td>@@FINANCING@@</td>
                  <td>Wydane</td>
                </tr>
              </table>

              <table>
                <tr><th>Usługi dodatkowe</th><th>Kwota</th></tr>
                @@SERVICES_ROWS@@
              </table>

              <div class="total">Kwota końcowa: @@FINAL_PRICE@@ zł</div>

              <div class="signatures">
                <div class="signature">Podpis kupującego</div>
                <div class="signature">Podpis handlowca</div>
              </div>
            </body>
            </html>
            """;
        return html
            .Replace("@@DOCUMENT_NUMBER@@", H(documentNumber))
            .Replace("@@DOCUMENT_DATE@@", DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
            .Replace("@@SALESPERSON@@", H(salesperson.Name))
            .Replace("@@SALESPERSON_ROLE@@", H(LocalizeCellValue("Role", salesperson.Role) ?? salesperson.Role.ToString()))
            .Replace("@@CUSTOMER_NAME@@", H(customer.Name))
            .Replace("@@CUSTOMER_PHONE@@", H(customer.Phone))
            .Replace("@@CUSTOMER_EMAIL@@", H(customer.Email))
            .Replace("@@VIN@@", H(vehicle.Vin))
            .Replace("@@VEHICLE_NAME@@", H($"{vehicle.Brand} {vehicle.Model}"))
            .Replace("@@ENGINE@@", H(LocalizeCellValue("Engine", vehicle.Engine) ?? vehicle.Engine.ToString()))
            .Replace("@@GEARBOX@@", H(LocalizeCellValue("Gearbox", vehicle.Gearbox) ?? vehicle.Gearbox.ToString()))
            .Replace("@@MILEAGE@@", vehicle.Mileage.ToString("N0"))
            .Replace("@@SALE_DATE@@", transaction.CreatedAt.ToString("dd.MM.yyyy HH:mm"))
            .Replace("@@RELEASE_DATE@@", DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
            .Replace("@@FINANCING@@", H(LocalizeCellValue("Financing", transaction.Financing) ?? transaction.Financing.ToString()))
            .Replace("@@SERVICES_ROWS@@", servicesRows)
            .Replace("@@FINAL_PRICE@@", transaction.FinalPrice.ToString("N2"));
    }

    private string BuildReleaseServiceRows(SaleTransaction transaction)
    {
        if (transaction.SelectedOptionIds.Count == 0) return "<tr><td>Brak usług dodatkowych</td><td>0,00 zł</td></tr>";
        var rows = new List<string>();
        foreach (var id in transaction.SelectedOptionIds)
        {
            var option = _store.Data.Options.FirstOrDefault(o => o.Id == id);
            if (option is null) continue;
            var price = option.Id == TransportPricing.OptionId
                ? TransportPricing.Calculate(transaction.TransportDistanceKm, transaction.TransportRouteKind)
                : option.Price;
            var name = option.Id == TransportPricing.OptionId
                ? $"{option.Name} - {TransportPricing.Describe(transaction.TransportRouteKind)}, {transaction.TransportDistanceKm} km"
                : option.Name;
            rows.Add($"<tr><td>{H(name)}</td><td>{price:N2} zł</td></tr>");
        }
        return rows.Count == 0 ? "<tr><td>Brak usług dodatkowych</td><td>0,00 zł</td></tr>" : string.Join(Environment.NewLine, rows);
    }

    private static string H(string value) => WebUtility.HtmlEncode(value);

    private static string ServiceLine(CarOption option, int transportDistance, TransportRouteKind transportRoute)
    {
        if (option.Id != TransportPricing.OptionId) return $"{option.Name} ({option.Price:N2} zł)";
        var rate = TransportPricing.RateFor(transportRoute);
        var cost = TransportPricing.Calculate(transportDistance, transportRoute);
        return $"{option.Name} ({TransportPricing.Describe(transportRoute)}, {transportDistance} km x {rate:N2} zł/km = {cost:N2} zł)";
    }

    private decimal CalculateServiceCost(IEnumerable<string> selectedIds, int transportDistance, TransportRouteKind transportRoute)
    {
        var ids = selectedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fixedCost = ids
            .Where(id => id != TransportPricing.OptionId)
            .Select(id => _store.Data.Options.First(o => o.Id == id).Price)
            .Sum();
        var transportCost = ids.Contains(TransportPricing.OptionId) ? TransportPricing.Calculate(transportDistance, transportRoute) : 0m;
        return fixedCost + transportCost;
    }

    private static string TransportDescription(IEnumerable<string> selectedIds, int transportDistance, TransportRouteKind transportRoute)
    {
        return selectedIds.Contains(TransportPricing.OptionId)
            ? $"Transport: {TransportPricing.Describe(transportRoute)}, {transportDistance} km, koszt {TransportPricing.Calculate(transportDistance, transportRoute):N2} zł."
            : "Transport: brak.";
    }

    private OptionResult ValidateServiceOptions(Vehicle vehicle, IEnumerable<string> selectedIds)
    {
        return new OptionDependencyMediator(_store.Data.Options).Normalize(vehicle, selectedIds);
    }

    private static void ShowServiceRuleMessages(OptionResult result)
    {
        if (result.Messages.Count == 0) return;
        MessageBox.Show(
            $"System automatycznie dostosował pakiet usług:\r\n\r\n{string.Join("\r\n", result.Messages)}",
            "Reguły pakietu usług");
    }

    private string ServiceNames(IEnumerable<string> selectedIds)
    {
        var names = selectedIds
            .Select(id => _store.Data.Options.FirstOrDefault(option => option.Id == id)?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
        return names.Count == 0 ? "brak" : string.Join(", ", names);
    }

    private PricingResult CalculatePrice(Vehicle vehicle, FinancingKind financing)
    {
        IPriceComponent price = new BaseVehiclePrice(vehicle.BasePrice);
        price = new MarginDecorator(price);
        price = new SeasonalPromotionDecorator(price);
        price = new FleetDiscountDecorator(price, false);
        price = new InsuranceDecorator(price);
        IFinancingStrategy strategy = financing switch
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
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
        ScrollBars = ScrollBars.Both,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        EditMode = DataGridViewEditMode.EditProgrammatically,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        BackgroundColor = ThemeInk,
        BorderStyle = BorderStyle.FixedSingle,
        EnableHeadersVisualStyles = false,
        GridColor = ThemeGridLine,
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ThemeInk,
            ForeColor = ThemeCream,
            SelectionBackColor = ThemeSelection,
            SelectionForeColor = ThemeCream
        },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ThemeInkLight,
            ForeColor = ThemeCream,
            SelectionBackColor = ThemeSelection,
            SelectionForeColor = ThemeCream
        },
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ThemeCream,
            ForeColor = ThemeInk,
            SelectionBackColor = ThemeGoldSoft,
            SelectionForeColor = ThemeInk
        },
        RowHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ThemeCream,
            ForeColor = ThemeInk,
            SelectionBackColor = ThemeGoldSoft,
            SelectionForeColor = ThemeInk
        }
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
            EnsureFullHeaderWidths(grid);
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
                column.Width = Math.Max(width, column.MinimumWidth);
            }
        };
    }

    private static void EnsureFullHeaderWidths(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (!column.Visible) continue;
            var headerSize = TextRenderer.MeasureText(column.HeaderText, grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font);
            column.MinimumWidth = headerSize.Width + 34;
            if (column.Width < column.MinimumWidth) column.Width = column.MinimumWidth;
        }
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
        "CommissionBalance" when value is decimal commission => $"{commission:N2} zł",
        "FinalPrice" when value is decimal finalPrice => $"{finalPrice:N2} zł",
        "BasePrice" when value is decimal basePrice => $"{basePrice:N2} zł",
        "Mileage" when value is int mileage => $"{mileage:N0} km",
        "SelectedOptionIds" when value is List<string> optionIds => ServiceNames(optionIds),
        "TransportRouteKind" when value is TransportRouteKind route => TransportPricing.Describe(route),
        "TransportDistanceKm" when value is int distance => distance > 0 ? $"{distance:N0} km" : "brak",
        _ => null
    };

    private static FlowLayoutPanel TopPanel() => new()
    {
        Dock = DockStyle.Top,
        Height = 76,
        Padding = new Padding(12),
        FlowDirection = FlowDirection.LeftToRight,
        BackColor = ThemeCream,
        ForeColor = ThemeInk
    };

    private static Button Button(string text, EventHandler click)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Size = ToolbarButtonSize,
            MinimumSize = ToolbarButtonSize,
            MaximumSize = ToolbarButtonSize,
            Margin = new Padding(4, 0, 4, 0),
            Padding = new Padding(8, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeInk,
            ForeColor = ThemeCream
        };
        button.FlatAppearance.BorderColor = ThemeGold;
        button.FlatAppearance.MouseOverBackColor = ThemeInkLight;
        button.FlatAppearance.MouseDownBackColor = ThemeSelection;
        button.Click += click;
        return button;
    }

    private static CheckBox Check(string text, Action changed, bool value = false)
    {
        var box = new CheckBox { Text = text, Checked = value, AutoSize = true, Padding = new Padding(8, 5, 0, 0), BackColor = ThemeCream, ForeColor = ThemeInk };
        box.CheckedChanged += (_, _) => changed();
        return box;
    }

    private static void ApplyTheme(Control root)
    {
        StyleControl(root);
        foreach (Control child in root.Controls) ApplyTheme(child);
    }

    private static void StyleControl(Control control)
    {
        control.ForeColor = ThemeInk;
        switch (control)
        {
            case Form form:
                form.BackColor = ThemeCream;
                break;
            case TabControl:
                control.BackColor = ThemeInk;
                control.ForeColor = ThemeCream;
                break;
            case TabPage:
            case FlowLayoutPanel:
            case TableLayoutPanel:
            case Panel:
                control.BackColor = ThemeCream;
                break;
            case SplitContainer split:
                split.BackColor = ThemeGold;
                split.Panel1.BackColor = ThemeCream;
                split.Panel2.BackColor = ThemeCream;
                break;
            case Label:
            case CheckBox:
                control.BackColor = ThemeCream;
                control.ForeColor = ThemeInk;
                break;
            case Button button:
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = ThemeInk;
                button.ForeColor = ThemeCream;
                button.FlatAppearance.BorderColor = ThemeGold;
                button.FlatAppearance.MouseOverBackColor = ThemeInkLight;
                button.FlatAppearance.MouseDownBackColor = ThemeSelection;
                break;
            case TextBox textBox:
                textBox.BackColor = ThemeInk;
                textBox.ForeColor = ThemeCream;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox comboBox:
                comboBox.BackColor = ThemeInk;
                comboBox.ForeColor = ThemeCream;
                comboBox.FlatStyle = FlatStyle.Flat;
                break;
            case CheckedListBox checkedListBox:
                checkedListBox.BackColor = ThemeInk;
                checkedListBox.ForeColor = ThemeCream;
                checkedListBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ListBox listBox:
                listBox.BackColor = ThemeInk;
                listBox.ForeColor = ThemeCream;
                listBox.BorderStyle = BorderStyle.FixedSingle;
                break;
        }
    }

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
