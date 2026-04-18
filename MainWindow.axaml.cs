using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DebloaterApp;

public partial class MainWindow : Window
{
    private Dictionary<CheckBox, string> appsToUninstall = new();

    public MainWindow()
    {
        InitializeComponent();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        var deleteButton = this.FindControl<Button>("DeleteButton");
        var copilotCheckBox = this.FindControl<CheckBox>("CopilotCheckBox");
        var edgeCheckBox = this.FindControl<CheckBox>("EdgeCheckBox");
        var oneDriveCheckBox = this.FindControl<CheckBox>("OneDriveCheckBox");

        if (deleteButton != null)
        {
            deleteButton.Click += OnDeleteButtonClicked;
        }

        // Map apps to their uninstall commands
        if (copilotCheckBox != null)
            appsToUninstall[copilotCheckBox] = "Microsoft.Copilot";
        if (edgeCheckBox != null)
            appsToUninstall[edgeCheckBox] = "Microsoft.MicrosoftEdge";
        if (oneDriveCheckBox != null)
            appsToUninstall[oneDriveCheckBox] = "Microsoft.OneDrive";
    }

    private async void OnDeleteButtonClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var statusMessage = this.FindControl<TextBlock>("StatusMessage");
        if (statusMessage == null) return;

        var selectedApps = appsToUninstall
            .Where(kvp => kvp.Key.IsChecked == true)
            .Select(kvp => kvp.Value)
            .ToList();

        if (selectedApps.Count == 0)
        {
            statusMessage.Text = "Please select at least one app to remove.";
            statusMessage.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Orange);
            return;
        }

        statusMessage.Text = "Starting removal process...";
        statusMessage.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Blue);

        var deleteButton = this.FindControl<Button>("DeleteButton");
        if (deleteButton != null)
            deleteButton.IsEnabled = false;

        try
        {
            await UninstallAppsAsync(selectedApps);
        }
        finally
        {
            if (deleteButton != null)
                deleteButton.IsEnabled = true;
        }
    }

    private async Task UninstallAppsAsync(List<string> appIds)
    {
        var statusMessage = this.FindControl<TextBlock>("StatusMessage");
        if (statusMessage == null) return;

        try
        {
            foreach (var appId in appIds)
            {
                statusMessage.Text = $"Removing {appId}...";
                await UninstallAppAsync(appId);
            }

            statusMessage.Text = "✓ Successfully removed selected apps!";
            statusMessage.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Green);

            // Uncheck the checkboxes after successful deletion
            foreach (var checkBox in appsToUninstall.Keys)
            {
                if (checkBox.IsChecked == true)
                    checkBox.IsChecked = false;
            }
        }
        catch (Exception ex)
        {
            statusMessage.Text = $"✗ Error: {ex.Message}";
            statusMessage.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Red);
        }
    }

    private async Task UninstallAppAsync(string appId)
    {
        // Use winget to uninstall the app with auto-confirm and silent mode
        var processInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -Command \"winget uninstall --id {appId} -e --accept-source-agreements --accept-package-agreements -h\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            if (process != null)
            {
                // Add a 2-minute timeout to prevent hanging
                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)))
                {
                    try
                    {
                        await process.WaitForExitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        process.Kill();
                        throw new Exception($"Uninstall timed out after 2 minutes. Winget may not be installed or responding.");
                    }
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Failed to uninstall {appId}. Exit code: {process.ExitCode}. Error: {error}");
                }
            }
        }
    }
}