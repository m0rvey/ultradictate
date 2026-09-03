using System;
using System.Threading;
using System.Windows.Forms;
using UltraDictate.Windows.Core;
using UltraDictate.Windows.UI;

namespace UltraDictate.Windows;

static class Program
{
    private const string MutexName = @"Global\UltraDictate_SingleInstance_Mutex";

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "UltraDictate is already running in the system tray.",
                "UltraDictate",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            var settings = SettingsManager.Load();
            using var context = new TrayApplicationContext(settings);
            Application.Run(context);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fatal error in UltraDictate: {ex.Message}",
                "UltraDictate Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
