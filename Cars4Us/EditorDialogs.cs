using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static Cars4Us.DialogHelpers;

namespace Cars4Us;

public sealed class VehicleEditorDialog : Form
{
    private readonly TextBox _vin = CreateTextBox("VIN");
    private readonly TextBox _brand = CreateTextBox("Marka");
    private readonly TextBox _model = CreateTextBox("Model");
    private readonly NumericUpDown _mileage = CreateNumber(0, 2_000_000, 0);
    private readonly NumericUpDown _price = CreateNumber(0, 10_000_000, 100_000);
    private readonly ComboBox _engine = CreateCombo(typeof(EngineType));
    private readonly ComboBox _gearbox = CreateCombo(typeof(Gearbox));
    private readonly ComboBox _availability = CreateCombo(typeof(VehicleAvailability));
    private readonly CheckBox _testDrive = new() { Text = "Auto testowe", AutoSize = true };

    public VehicleEditorDialog()
    {
        Text = "Nowe auto old time";
        ConfigureDialogWindow(this);
        LocalizeCombo(_engine, LocalizeEnumValue);
        LocalizeCombo(_gearbox, LocalizeEnumValue);
        LocalizeCombo(_availability, LocalizeEnumValue);
        BuildForm("Dodaj pojazd");
        _brand.Text = "Porsche";
        _model.Text = "911 Classic";
        _engine.SelectedItem = EngineType.Petrol;
        _gearbox.SelectedItem = Gearbox.Manual;
        _availability.SelectedItem = VehicleAvailability.InShowroom;
    }

    public VehicleEditorDialog(Vehicle vehicle)
    {
        Text = "Modyfikacja pojazdu";
        ConfigureDialogWindow(this);
        BuildForm("Zapisz zmiany");
        _vin.Text = vehicle.Vin;
        _brand.Text = vehicle.Brand;
        _model.Text = vehicle.Model;
        SelectEnumValue(_engine, vehicle.Engine);
        SelectEnumValue(_gearbox, vehicle.Gearbox);
        _mileage.Value = Math.Clamp(vehicle.Mileage, (int)_mileage.Minimum, (int)_mileage.Maximum);
        _price.Value = Math.Clamp(vehicle.BasePrice, _price.Minimum, _price.Maximum);
        SelectEnumValue(_availability, vehicle.Availability);
        _testDrive.Checked = vehicle.IsTestDriveCar;
    }

    public Vehicle Vehicle => new CarBuilder()
        .Identity(_vin.Text.Trim(), _brand.Text.Trim(), _model.Text.Trim())
        .Technical((EngineType)_engine.SelectedItem!, (Gearbox)_gearbox.SelectedItem!, (int)_mileage.Value)
        .Price(_price.Value)
        .Availability((VehicleAvailability)_availability.SelectedItem!)
        .TestDriveCar(_testDrive.Checked)
        .Build();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK) return;
        if (HasEmptyText(_vin, _brand, _model))
        {
            MessageBox.Show("Uzupełnij wszystkie pola tekstowe pojazdu: VIN, markę i model.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (!IsValidVin(_vin.Text))
        {
            MessageBox.Show("VIN może zawierać tylko litery i cyfry oraz powinien mieć od 5 do 17 znaków.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (!IsReasonableText(_brand.Text) || !IsReasonableText(_model.Text))
        {
            MessageBox.Show("Marka i model mogą zawierać litery, cyfry, spacje oraz znaki: - . /", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (_engine.SelectedItem is null || _gearbox.SelectedItem is null || _availability.SelectedItem is null)
        {
            MessageBox.Show("Wybierz typ silnika, skrzynię biegów i dostępność.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (_price.Value <= 0)
        {
            MessageBox.Show("Cena bazowa musi być większa od zera.", "Cars4Us");
            e.Cancel = true;
        }
    }

    private void BuildForm(string okText)
    {
        var panel = DialogLayout();
        AddRow(panel, "VIN", _vin);
        AddRow(panel, "Marka", _brand);
        AddRow(panel, "Model", _model);
        AddRow(panel, "Typ silnika", _engine);
        AddRow(panel, "Skrzynia biegów", _gearbox);
        AddRow(panel, "Przebieg", _mileage);
        AddRow(panel, "Cena bazowa", _price);
        AddRow(panel, "Dostępność", _availability);
        AddRow(panel, "", _testDrive);
        AddButtons(panel, okText);
        Controls.Add(panel);
    }

    private void AddButtons(TableLayoutPanel panel, string okText)
    {
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var ok = new Button { Text = okText, DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Anuluj", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        panel.Controls.Add(buttons, 1, panel.RowCount++);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}

public sealed class CustomerEditorDialog : Form
{
    private readonly TextBox _name = CreateTextBox("Imię i nazwisko");
    private readonly TextBox _phone = CreateTextBox("Telefon");
    private readonly TextBox _email = CreateTextBox("E-mail");

    public CustomerEditorDialog()
    {
        Text = "Nowy klient";
        ConfigureDialogWindow(this);
        var panel = DialogLayout();
        AddRow(panel, "Imię i nazwisko", _name);
        AddRow(panel, "Telefon", _phone);
        AddRow(panel, "E-mail", _email);
        AddButtons(panel, "Dodaj klienta");
        Controls.Add(panel);
    }

    public CustomerEditorDialog(Customer customer) : this()
    {
        Text = "Modyfikacja klienta";
        SetPrimaryButtonText(this, "Zapisz zmiany");
        _name.Text = customer.Name;
        _phone.Text = customer.Phone;
        _email.Text = customer.Email;
    }

    public Customer Customer => new() { Name = _name.Text.Trim(), Phone = _phone.Text.Trim(), Email = _email.Text.Trim() };

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK) return;
        if (HasEmptyText(_name, _phone, _email))
        {
            MessageBox.Show("Uzupełnij wszystkie dane klienta: imię i nazwisko, telefon oraz e-mail.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (!IsPersonName(_name.Text))
        {
            MessageBox.Show("Imię i nazwisko może zawierać tylko litery, spacje, myślnik i apostrof.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (!IsDigitsOnly(_phone.Text) || _phone.Text.Trim().Length is < 7 or > 15)
        {
            MessageBox.Show("Telefon musi zawierać wyłącznie cyfry i mieć od 7 do 15 znaków.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (!IsValidEmail(_email.Text))
        {
            MessageBox.Show("Podaj poprawny adres e-mail, np. testowy@outlook.com.", "Cars4Us");
            e.Cancel = true;
        }
    }
}

public sealed class EmployeeEditorDialog : Form
{
    private readonly TextBox _name = CreateTextBox("Imię i nazwisko");
    private readonly ComboBox _role = CreateCombo(typeof(EmployeeRole));

    public EmployeeEditorDialog()
    {
        Text = "Nowy pracownik";
        ConfigureDialogWindow(this);
        LocalizeCombo(_role, LocalizeEnumValue);
        var panel = DialogLayout();
        AddRow(panel, "Imię i nazwisko", _name);
        AddRow(panel, "Rola", _role);
        AddButtons(panel, "Dodaj pracownika");
        Controls.Add(panel);
    }

    public EmployeeEditorDialog(Employee employee) : this()
    {
        Text = "Modyfikacja pracownika";
        SetPrimaryButtonText(this, "Zapisz zmiany");
        _name.Text = employee.Name;
        SelectEnumValue(_role, employee.Role);
    }

    public Employee Employee => new() { Name = _name.Text.Trim(), Role = (EmployeeRole)_role.SelectedItem! };

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            MessageBox.Show("Podaj imię i nazwisko pracownika.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (!IsPersonName(_name.Text))
        {
            MessageBox.Show("Imię i nazwisko pracownika może zawierać tylko litery, spacje, myślnik i apostrof.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (_role.SelectedItem is null)
        {
            MessageBox.Show("Wybierz rolę pracownika.", "Cars4Us");
            e.Cancel = true;
        }
    }
}

public sealed class TestDriveEditorDialog : Form
{
    private readonly ComboBox _vehicle = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _customer = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _salesperson = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _start = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm", Width = 220 };
    private readonly NumericUpDown _duration = CreateNumber(15, 240, 60);
    private readonly TextBox _notes = CreateTextBox("Notatki");

    public TestDriveEditorDialog(IEnumerable<Vehicle> vehicles, IEnumerable<Customer> customers, IEnumerable<Employee> employees, TestDrive? testDrive = null)
    {
        Text = "Rezerwacja jazdy próbnej";
        ConfigureDialogWindow(this);
        var vehicleList = vehicles.Where(v => v.IsTestDriveCar || v.Vin == testDrive?.VehicleVin).ToList();
        var customerList = customers.ToList();
        var salespersonList = employees.Where(e => e.Role == EmployeeRole.Salesperson || e.Id == testDrive?.SalespersonId).ToList();
        _vehicle.DataSource = vehicleList;
        _customer.DataSource = customerList;
        _salesperson.DataSource = salespersonList;
        _start.Value = DateTime.Now.AddDays(1).Date.AddHours(10);

        var panel = DialogLayout();
        AddRow(panel, "Auto testowe", _vehicle);
        AddRow(panel, "Klient", _customer);
        AddRow(panel, "Handlowiec", _salesperson);
        AddRow(panel, "Termin", _start);
        AddRow(panel, "Czas jazdy (min)", _duration);
        AddRow(panel, "Notatki", _notes);
        AddButtons(panel, "Zarezerwuj");
        Controls.Add(panel);

        if (testDrive is null) return;
        Text = "Modyfikacja jazdy próbnej";
        SetPrimaryButtonText(this, "Zapisz zmiany");
        SelectComboItem(_vehicle, vehicleList.FirstOrDefault(v => v.Vin == testDrive.VehicleVin));
        SelectComboItem(_customer, customerList.FirstOrDefault(c => c.Id == testDrive.CustomerId));
        SelectComboItem(_salesperson, salespersonList.FirstOrDefault(e => e.Id == testDrive.SalespersonId));
        _start.Value = testDrive.Start;
        _duration.Value = Math.Clamp((decimal)(testDrive.End - testDrive.Start).TotalMinutes, _duration.Minimum, _duration.Maximum);
        _notes.Text = testDrive.Notes;
    }

    public TestDrive TestDrive
    {
        get
        {
            var start = _start.Value;
            return new TestDrive
            {
                VehicleVin = ((Vehicle)_vehicle.SelectedItem!).Vin,
                CustomerId = ((Customer)_customer.SelectedItem!).Id,
                SalespersonId = ((Employee)_salesperson.SelectedItem!).Id,
                Start = start,
                End = start.AddMinutes((double)_duration.Value),
                Notes = _notes.Text.Trim()
            };
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK) return;
        if (_vehicle.SelectedItem is null || _customer.SelectedItem is null || _salesperson.SelectedItem is null)
        {
            MessageBox.Show("Wybierz auto testowe, klienta i handlowca.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (string.IsNullOrWhiteSpace(_notes.Text))
        {
            MessageBox.Show("Uzupełnij notatki do jazdy próbnej.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (!IsSafeFreeText(_notes.Text))
        {
            MessageBox.Show("Notatki nie mogą zawierać znaków < ani > i powinny mieć maksymalnie 250 znaków.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (_duration.Value <= 0)
        {
            MessageBox.Show("Czas jazdy musi być większy od zera.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (_start.Value <= DateTime.Now)
        {
            MessageBox.Show("Termin jazdy próbnej musi być w przyszłości.", "Cars4Us");
            e.Cancel = true;
        }
    }
}

public sealed class SaleEditorDialog : Form
{
    private readonly ComboBox _vehicle = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _customer = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _salesperson = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _financing = CreateCombo(typeof(FinancingKind));

    public SaleEditorDialog(IEnumerable<Vehicle> vehicles, IEnumerable<Customer> customers, IEnumerable<Employee> employees, SaleTransaction? transaction = null)
    {
        Text = "Rozpoczęcie sprzedaży";
        ConfigureDialogWindow(this);
        LocalizeCombo(_financing, LocalizeEnumValue);
        var vehicleList = vehicles.Where(v => VehicleStateFactory.From(v.StateName).CanReserve || v.Vin == transaction?.VehicleVin).ToList();
        var customerList = customers.ToList();
        var salespersonList = employees.Where(e => e.Role == EmployeeRole.Salesperson || e.Id == transaction?.SalespersonId).ToList();
        _vehicle.DataSource = vehicleList;
        _customer.DataSource = customerList;
        _salesperson.DataSource = salespersonList;

        var panel = DialogLayout();
        AddRow(panel, "Pojazd", _vehicle);
        AddRow(panel, "Klient", _customer);
        AddRow(panel, "Handlowiec", _salesperson);
        AddRow(panel, "Finansowanie", _financing);
        AddButtons(panel, "Rozpocznij sprzedaż");
        Controls.Add(panel);

        if (transaction is null) return;
        Text = "Modyfikacja transakcji";
        SetPrimaryButtonText(this, "Zapisz zmiany");
        SelectComboItem(_vehicle, vehicleList.FirstOrDefault(v => v.Vin == transaction.VehicleVin));
        SelectComboItem(_customer, customerList.FirstOrDefault(c => c.Id == transaction.CustomerId));
        SelectComboItem(_salesperson, salespersonList.FirstOrDefault(e => e.Id == transaction.SalespersonId));
        SelectEnumValue(_financing, transaction.Financing);
    }

    public Vehicle Vehicle => (Vehicle)_vehicle.SelectedItem!;
    public Customer Customer => (Customer)_customer.SelectedItem!;
    public Employee Salesperson => (Employee)_salesperson.SelectedItem!;
    public FinancingKind Financing => (FinancingKind)_financing.SelectedItem!;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK) return;
        if (_vehicle.SelectedItem is null || _customer.SelectedItem is null || _salesperson.SelectedItem is null)
        {
            MessageBox.Show("Wybierz pojazd, klienta i handlowca.", "Cars4Us");
            e.Cancel = true;
            return;
        }
        if (_financing.SelectedItem is null)
        {
            MessageBox.Show("Wybierz model finansowania.", "Cars4Us");
            e.Cancel = true;
        }
    }
}

public sealed class DeleteConfirmationDialog : Form
{
    public DeleteConfirmationDialog(string title, string displayName, IEnumerable<string> dependencies)
    {
        Text = title;
        ConfigureDialogWindow(this);
        Width = 560;

        var panel = DialogLayout();
        var message = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Width = 480,
            Height = 180,
            Text = BuildMessage(displayName, dependencies)
        };
        AddRow(panel, "Analiza", message);
        AddButtons(panel, "Usuń");
        Controls.Add(panel);
    }

    private static string BuildMessage(string displayName, IEnumerable<string> dependencies)
    {
        var dependencyList = dependencies.ToList();
        var text = $"Wybrany rekord:\r\n{displayName}\r\n\r\n";
        text += dependencyList.Count == 0
            ? "Nie znaleziono powiązań z innymi danymi.\r\n\r\n"
            : $"Znaleziono powiązania:\r\n- {string.Join("\r\n- ", dependencyList)}\r\n\r\n";
        text += "Po akceptacji rekord zostanie przeniesiony do kosza i będzie można go przywrócić przez 31 dni.";
        return text;
    }
}

internal static class DialogHelpers
{
    public static TableLayoutPanel DialogLayout()
    {
        return new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = 0,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
    }

    public static void ConfigureDialogWindow(Form form)
    {
        form.StartPosition = FormStartPosition.CenterParent;
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.AutoSize = true;
        form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    public static void AddRow(TableLayoutPanel panel, string label, Control control)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 8, 0) }, 0, panel.RowCount);
        control.Width = Math.Max(control.Width, 260);
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        panel.Controls.Add(control, 1, panel.RowCount);
        panel.RowCount++;
    }

    public static void AddButtons(TableLayoutPanel panel, string okText)
    {
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true };
        var ok = new Button { Text = okText, DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Anuluj", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        panel.Controls.Add(buttons, 1, panel.RowCount++);
    }

    public static TextBox CreateTextBox(string placeholder) => new() { PlaceholderText = placeholder, Width = 260 };

    public static NumericUpDown CreateNumber(decimal min, decimal max, decimal value) => new()
    {
        Minimum = min,
        Maximum = max,
        Value = value,
        Width = 260,
        ThousandsSeparator = true
    };

    public static ComboBox CreateCombo(Type enumType)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
        combo.DataSource = Enum.GetValues(enumType);
        return combo;
    }

    public static void LocalizeCombo(ComboBox combo, Func<object, string> formatter)
    {
        combo.Format += (_, e) =>
        {
            if (e.ListItem is not null) e.Value = formatter(e.ListItem);
        };
    }

    public static string LocalizeEnumValue(object value) => value switch
    {
        EngineType.Petrol => "Benzynowy",
        EngineType.Diesel => "Diesel",
        EngineType.Hybrid => "Hybrydowy",
        EngineType.Electric => "Elektryczny",
        Gearbox.Manual => "Manualna",
        Gearbox.Automatic => "Automatyczna",
        VehicleAvailability.InShowroom => "W salonie",
        VehicleAvailability.OnOrder => "Na zamówienie",
        EmployeeRole.Salesperson => "Handlowiec",
        EmployeeRole.Manager => "Manager",
        EmployeeRole.ServiceTechnician => "Serwisant",
        FinancingKind.Cash => "Gotówka",
        FinancingKind.Leasing => "Leasing",
        FinancingKind.Credit => "Kredyt",
        _ => value.ToString() ?? ""
    };

    public static bool HasEmptyText(params TextBox[] textBoxes) =>
        textBoxes.Any(textBox => string.IsNullOrWhiteSpace(textBox.Text));

    public static bool IsDigitsOnly(string value) =>
        Regex.IsMatch(value.Trim(), @"^\d+$");

    public static bool IsValidEmail(string value) =>
        Regex.IsMatch(value.Trim(), @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$");

    public static bool IsValidVin(string value) =>
        Regex.IsMatch(value.Trim(), @"^[A-Za-z0-9]{5,17}$");

    public static bool IsPersonName(string value) =>
        Regex.IsMatch(value.Trim(), @"^[\p{L}][\p{L}\s'-]{1,79}$");

    public static bool IsReasonableText(string value) =>
        Regex.IsMatch(value.Trim(), @"^[\p{L}0-9][\p{L}0-9\s.\-\/]{0,79}$");

    public static bool IsSafeFreeText(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length is >= 3 and <= 250 && !trimmed.Contains('<') && !trimmed.Contains('>');
    }

    public static void SetPrimaryButtonText(Control root, string text)
    {
        foreach (Control control in root.Controls)
        {
            if (control is Button { DialogResult: DialogResult.OK } button)
            {
                button.Text = text;
                return;
            }
            SetPrimaryButtonText(control, text);
        }
    }

    public static void SelectComboItem<T>(ComboBox combo, T? item) where T : class
    {
        if (item is null) return;
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (ReferenceEquals(combo.Items[i], item))
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    public static void SelectEnumValue<TEnum>(ComboBox combo, TEnum value) where TEnum : struct, Enum
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is TEnum item && EqualityComparer<TEnum>.Default.Equals(item, value))
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }
}
