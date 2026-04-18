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
        var oneDriveCheckBox = this.FindControl<CheckBox>("OneDriveCheckBox");

        if (deleteButton != null)
        {
            deleteButton.Click += OnDeleteButtonClicked;
        }

        // Map apps to their uninstall commands
        if (copilotCheckBox != null)
            appsToUninstall[copilotCheckBox] = "Microsoft.Copilot";
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
                statusMessage.Text = $"Removing {appId}... (this may take a moment)";
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
        // First try winget
        try
        {
            await UninstallViaWingetAsync(appId);
        }
        catch (Exception wingetError)
        {
            // Fall back to PowerShell app removal for built-in apps
            try
            {
                await UninstallViaAppxAsync(appId);
            }
            catch (Exception appxError)
            {
                throw new Exception($"Both methods failed. Winget: {wingetError.Message}. Appx: {appxError.Message}");
            }
        }
    }

    private async Task UninstallViaWingetAsync(string appId)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -Command \"winget uninstall --id {appId} -e --accept-source-agreements --accept-package-agreements -h 2>&1\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            if (process != null)
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3)))
                {
                    try
                    {
                        await process.WaitForExitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        process.Kill();
                        throw new Exception($"Winget timed out after 3 minutes.");
                    }
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (process.ExitCode != 0)
                {
                    var fullMessage = string.IsNullOrEmpty(output) ? error : output;
                    throw new Exception($"Winget failed: {fullMessage.Substring(0, Math.Min(200, fullMessage.Length))}");
                }
            }
        }
    }

    private async Task UninstallViaAppxAsync(string appId)
    {
        // For built-in Windows apps, use PowerShell Get-AppxPackage
        // This searches for packages matching the pattern and removes them
        var processInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -Command \"$packages = Get-AppxPackage -AllUsers | Where-Object {{$_.Name -like '*{appId}*'}}; if ($packages) {{ $packages | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue; 'Removed'; exit 0 }} else {{ 'Not found'; exit 1 }}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            if (process != null)
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3)))
                {
                    try
                    {
                        await process.WaitForExitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        process.Kill();
                        throw new Exception($"App removal timed out.");
                    }
                }

                var output = process.StandardOutput.ReadToEnd().Trim();
                var error = process.StandardError.ReadToEnd();

                // Check if app was found and removed
                if (output.Contains("Not found") || (process.ExitCode != 0 && string.IsNullOrEmpty(output)))
                {
                    throw new Exception($"App '{appId}' not found on system or removal failed.");
                }
            }
        }
    }
}