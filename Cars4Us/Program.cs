using System;
using System.IO;
using System.Windows.Forms;

namespace Cars4Us;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var store = JsonDataStore.LoadOrSeed(Path.Combine(AppContext.BaseDirectory, "cars4us.db"));
        Application.ApplicationExit += (_, _) => store.Save();
        Application.Run(new MainForm(store));
    }
}
