using System.Drawing;
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
        BuildForm("Dodaj pojazd");
        _brand.Text = "Porsche";
        _model.Text = "911 Classic";
        _engine.SelectedItem = EngineType.Petrol;
        _gearbox.SelectedItem = Gearbox.Manual;
        _availability.SelectedItem = VehicleAvailability.InShowroom;
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
        if (string.IsNullOrWhiteSpace(_vin.Text) || string.IsNullOrWhiteSpace(_brand.Text) || string.IsNullOrWhiteSpace(_model.Text))
        {
            MessageBox.Show("Uzupełnij VIN, markę i model.", "Cars4Us");
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

    public Customer Customer => new() { Name = _name.Text.Trim(), Phone = _phone.Text.Trim(), Email = _email.Text.Trim() };

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && string.IsNullOrWhiteSpace(_name.Text))
        {
            MessageBox.Show("Podaj imię i nazwisko klienta.", "Cars4Us");
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
        var panel = DialogLayout();
        AddRow(panel, "Imię i nazwisko", _name);
        AddRow(panel, "Rola", _role);
        AddButtons(panel, "Dodaj pracownika");
        Controls.Add(panel);
    }

    public Employee Employee => new() { Name = _name.Text.Trim(), Role = (EmployeeRole)_role.SelectedItem! };

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && string.IsNullOrWhiteSpace(_name.Text))
        {
            MessageBox.Show("Podaj imię i nazwisko pracownika.", "Cars4Us");
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

    public TestDriveEditorDialog(IEnumerable<Vehicle> vehicles, IEnumerable<Customer> customers, IEnumerable<Employee> employees)
    {
        Text = "Rezerwacja jazdy próbnej";
        ConfigureDialogWindow(this);
        _vehicle.DataSource = vehicles.Where(v => v.IsTestDriveCar).ToList();
        _customer.DataSource = customers.ToList();
        _salesperson.DataSource = employees.Where(e => e.Role == EmployeeRole.Salesperson).ToList();
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
        }
    }
}

public sealed class SaleEditorDialog : Form
{
    private readonly ComboBox _vehicle = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _customer = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _salesperson = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _financing = CreateCombo(typeof(FinancingKind));

    public SaleEditorDialog(IEnumerable<Vehicle> vehicles, IEnumerable<Customer> customers, IEnumerable<Employee> employees)
    {
        Text = "Rozpoczęcie sprzedaży";
        ConfigureDialogWindow(this);
        _vehicle.DataSource = vehicles.Where(v => VehicleStateFactory.From(v.StateName).CanReserve).ToList();
        _customer.DataSource = customers.ToList();
        _salesperson.DataSource = employees.Where(e => e.Role == EmployeeRole.Salesperson).ToList();

        var panel = DialogLayout();
        AddRow(panel, "Pojazd", _vehicle);
        AddRow(panel, "Klient", _customer);
        AddRow(panel, "Handlowiec", _salesperson);
        AddRow(panel, "Finansowanie", _financing);
        AddButtons(panel, "Rozpocznij sprzedaż");
        Controls.Add(panel);
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
        }
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
}
