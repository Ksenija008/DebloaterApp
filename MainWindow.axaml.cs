using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DebloaterApp;

public partial class MainWindow : Window
{
    // Mapping: AppId -> Display Name
    private readonly Dictionary<string, string> appRegistry = new()
    {
        { "Microsoft.Copilot", "Copilot" },
        { "Microsoft.OneDrive", "OneDrive" }
    };

    private Dictionary<CheckBox, string> appsToUninstall = new();
    private Button? deleteButton;
    private TextBlock? statusMessage;
    private TextBlock? outputTerminal;

    public MainWindow()
    {
        InitializeComponent();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        deleteButton = this.FindControl<Button>("DeleteButton");
        statusMessage = this.FindControl<TextBlock>("StatusMessage");
        outputTerminal = this.FindControl<TextBlock>("OutputTerminal");
        var copilotCheckBox = this.FindControl<CheckBox>("CopilotCheckBox");
        var oneDriveCheckBox = this.FindControl<CheckBox>("OneDriveCheckBox");

        if (deleteButton != null)
        {
            deleteButton.Click += OnDeleteButtonClicked;
        }

        if (copilotCheckBox != null)
            appsToUninstall[copilotCheckBox] = "Microsoft.Copilot";
        if (oneDriveCheckBox != null)
            appsToUninstall[oneDriveCheckBox] = "Microsoft.OneDrive";
    }

    /// <summary>
    /// Appends text to the output terminal with a timestamp.
    /// </summary>
    private void AppendOutput(string text)
    {
        if (outputTerminal == null) return;
        
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var line = $"[{timestamp}] {text}\n";
        
        outputTerminal.Text += line;
    }

    /// <summary>
    /// Clears the output terminal.
    /// </summary>
    private void ClearOutput()
    {
        if (outputTerminal == null) return;
        outputTerminal.Text = "";
    }

    private async void OnDeleteButtonClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var selectedApps = appsToUninstall
            .Where(kvp => kvp.Key.IsChecked == true)
            .Select(kvp => kvp.Value)
            .ToList();

        if (selectedApps.Count == 0)
        {
            UpdateStatus("Please select at least one app to remove.", Colors.Orange);
            return;
        }

        UpdateStatus($"Starting removal of {selectedApps.Count} app(s)...", Colors.Blue);
        SetButtonState(false, "Removing...");

        try
        {
            await UninstallAppsAsync(selectedApps);
        }
        finally
        {
            SetButtonState(true, "Remove Selected Apps");
        }
    }

    /// <summary>
    /// Updates the status message with the given text and color.
    /// </summary>
    private void UpdateStatus(string message, Color color)
    {
        if (statusMessage == null) return;
        statusMessage.Text = message;
        statusMessage.Foreground = new SolidColorBrush(color);
    }

    /// <summary>
    /// Updates the delete button state and text.
    /// </summary>
    private void SetButtonState(bool isEnabled, string text)
    {
        if (deleteButton == null) return;
        deleteButton.IsEnabled = isEnabled;
        deleteButton.Content = text;
    }

    /// <summary>
    /// Gets the display name for an app ID.
    /// </summary>
    private string GetAppDisplayName(string appId)
    {
        return appRegistry.TryGetValue(appId, out var displayName) ? displayName : appId;
    }

    private async Task UninstallAppsAsync(List<string> appIds)
    {
        ClearOutput();
        AppendOutput("=== Removal Process Started ===");
        
        var results = new List<(string appId, bool success, string message)>();
        int completed = 0;

        foreach (var appId in appIds)
        {
            var displayName = GetAppDisplayName(appId);
            AppendOutput($"\n--- Removing {displayName} ---");
            UpdateStatus($"[{completed + 1}/{appIds.Count}] Removing {displayName}...", Colors.Blue);

            try
            {
                await UninstallAppAsync(appId);
                results.Add((appId, true, $"{displayName}: ✓ Removed successfully"));
                AppendOutput($"✓ {displayName} removed successfully");
            }
            catch (Exception ex)
            {
                results.Add((appId, false, $"{displayName}: ✗ {ex.Message}"));
                AppendOutput($"✗ {displayName} removal failed: {ex.Message}");
            }

            completed++;
        }

        AppendOutput("\n=== Removal Process Completed ===");
        DisplayResults(results);
    }

    private void DisplayResults(List<(string appId, bool success, string message)> results)
    {
        var successCount = results.Count(r => r.success);
        var failureCount = results.Count(r => !r.success);
        var allSuccessful = failureCount == 0;

        var summaryMessage = allSuccessful
            ? $"✓ Successfully removed all {successCount} app(s)!"
            : $"✓ {successCount} removed, ✗ {failureCount} failed";

        UpdateStatus(summaryMessage, allSuccessful ? Colors.Green : Colors.Orange);

        // Uncheck successful removals
        foreach (var (appId, success, _) in results.Where(r => r.success))
        {
            var checkbox = appsToUninstall.FirstOrDefault(kvp => kvp.Value == appId).Key;
            if (checkbox != null && checkbox.IsChecked == true)
                checkbox.IsChecked = false;
        }
    }

    private async Task UninstallAppAsync(string appId)
    {
        // First try winget
        try
        {
            await ExecuteUninstallCommandAsync(GetWingetCommand(appId), "winget");
        }
        catch (Exception wingetError)
        {
            // Fall back to PowerShell app removal for built-in apps
            try
            {
                await ExecuteUninstallCommandAsync(GetAppxCommand(appId), "appx");
            }
            catch (Exception appxError)
            {
                throw new Exception($"Winget: {wingetError.Message}. Appx: {appxError.Message}");
            }
        }
    }

    private ProcessStartInfo GetWingetCommand(string appId)
    {
        var escapedId = EscapePowerShellString(appId);
        return CreatePowerShellProcess(
            $"winget uninstall --id {escapedId} -e --accept-source-agreements -h 2>&1"
        );
    }

    private ProcessStartInfo GetAppxCommand(string appId)
    {
        var escapedId = EscapePowerShellString(appId);
        var script = $@"
$package = Get-AppxPackage -AllUsers | Where-Object {{$_.Name -eq {escapedId}}}
if ($package) {{
    Get-AppxPackage -AllUsers | Where-Object {{$_.Name -eq {escapedId}}} | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue
    Write-Host 'Removal completed'
    exit 0
}} else {{
    Write-Host 'Package not found'
    exit 1
}}";
        return CreatePowerShellProcess(script);
    }

    private ProcessStartInfo CreatePowerShellProcess(string command)
    {
        return new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -Command \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    /// <summary>
    /// Escapes a string for safe use in PowerShell commands.
    /// </summary>
    private string EscapePowerShellString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "''";

        // Escape single quotes by doubling them
        var escaped = input.Replace("'", "''");
        return $"'{escaped}'";
    }

    private async Task ExecuteUninstallCommandAsync(ProcessStartInfo processInfo, string methodName)
    {
        AppendOutput($"Starting {methodName}...");
        
        using (var process = Process.Start(processInfo))
        {
            if (process == null)
                throw new Exception($"{methodName}: Failed to start process");

            using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3)))
            {
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    process.Kill();
                    AppendOutput($"✗ {methodName} timed out after 3 minutes");
                    throw new Exception($"{methodName}: Operation timed out after 3 minutes");
                }
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            // Display command output
            if (!string.IsNullOrWhiteSpace(output))
            {
                foreach (var line in output.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        AppendOutput($"  {line}");
                }
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                foreach (var line in error.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        AppendOutput($"  ERROR: {line}");
                }
            }

            // Handle appx-specific exit codes
            if (methodName == "appx" && process.ExitCode == 1)
            {
                AppendOutput($"Package not found (exit code 1)");
                throw new Exception("Package not found or already removed");
            }

            if (process.ExitCode != 0)
            {
                var errorMessage = string.IsNullOrEmpty(error) ? output : error;
                var truncated = errorMessage.Length > 150 
                    ? errorMessage.Substring(0, 150) + "..." 
                    : errorMessage;
                AppendOutput($"✗ {methodName} failed with exit code {process.ExitCode}");
                throw new Exception($"{methodName} failed: {truncated}");
            }

            AppendOutput($"✓ {methodName} completed successfully (exit code 0)");
        }
    }
}