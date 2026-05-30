using Cars4Us;
using System.Windows.Forms;

ApplicationConfiguration.Initialize();
var store = JsonDataStore.LoadOrSeed(Path.Combine(AppContext.BaseDirectory, "cars4us-data.json"));
Application.Run(new MainForm(store));
