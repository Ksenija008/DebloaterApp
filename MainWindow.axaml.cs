using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        { "Microsoft.OneDrive", "OneDrive" },
        { "Microsoft.WindowsCalculator", "Calculator" },
        { "Microsoft.BingWeather", "Weather" },
        { "Microsoft.News", "News" },
        { "Microsoft.ZuneMusic", "Media Player" },
        { "Microsoft.XboxApp", "Xbox Gaming" },
        { "Microsoft.549981C3F5F10", "Cortana" },
        { "Microsoft.MicrosoftSolitaireCollection", "Solitaire" },
        { "king.com.CandyCrush", "Candy Crush" },
        { "Microsoft.ZuneVideo", "Movies & TV" },
        { "Microsoft.Windows.Photos", "Photos" }
    };

    private Dictionary<CheckBox, string> appsToUninstall = new();
    private Button? deleteButton;
    private TextBlock? statusMessage;
    private TextBox? outputTerminal;
    private Image? successImage;

    public MainWindow()
    {
        InitializeComponent();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        deleteButton = this.FindControl<Button>("DeleteButton");
        statusMessage = this.FindControl<TextBlock>("StatusMessage");
        outputTerminal = this.FindControl<TextBox>("OutputTerminal");
        successImage = this.FindControl<Image>("SuccessImage");
        var copilotCheckBox = this.FindControl<CheckBox>("CopilotCheckBox");
        var oneDriveCheckBox = this.FindControl<CheckBox>("OneDriveCheckBox");
        var calculatorCheckBox = this.FindControl<CheckBox>("CalculatorCheckBox");
        var weatherCheckBox = this.FindControl<CheckBox>("WeatherCheckBox");
        var newsCheckBox = this.FindControl<CheckBox>("NewsCheckBox");
        var mediaPlayerCheckBox = this.FindControl<CheckBox>("MediaPlayerCheckBox");
        var xboxCheckBox = this.FindControl<CheckBox>("XboxCheckBox");
        var cortanaCheckBox = this.FindControl<CheckBox>("CortanaCheckBox");
        var gamesCheckBox = this.FindControl<CheckBox>("GamesCheckBox");
        var moviesCheckBox = this.FindControl<CheckBox>("MoviesCheckBox");
        var photosCheckBox = this.FindControl<CheckBox>("PhotosCheckBox");

        if (deleteButton != null)
        {
            deleteButton.Click += OnDeleteButtonClicked;
        }

        if (copilotCheckBox != null)
            appsToUninstall[copilotCheckBox] = "Microsoft.Copilot";
        if (oneDriveCheckBox != null)
            appsToUninstall[oneDriveCheckBox] = "Microsoft.OneDrive";
        if (calculatorCheckBox != null)
            appsToUninstall[calculatorCheckBox] = "Microsoft.WindowsCalculator";
        if (weatherCheckBox != null)
            appsToUninstall[weatherCheckBox] = "Microsoft.BingWeather";
        if (newsCheckBox != null)
            appsToUninstall[newsCheckBox] = "Microsoft.News";
        if (mediaPlayerCheckBox != null)
            appsToUninstall[mediaPlayerCheckBox] = "Microsoft.ZuneMusic";
        if (xboxCheckBox != null)
            appsToUninstall[xboxCheckBox] = "Microsoft.XboxApp";
        if (cortanaCheckBox != null)
            appsToUninstall[cortanaCheckBox] = "Microsoft.549981C3F5F10";
        if (gamesCheckBox != null)
        {
            appsToUninstall[gamesCheckBox] = "Microsoft.MicrosoftSolitaireCollection|king.com.CandyCrush";
        }
        if (moviesCheckBox != null)
            appsToUninstall[moviesCheckBox] = "Microsoft.ZuneVideo";
        if (photosCheckBox != null)
            appsToUninstall[photosCheckBox] = "Microsoft.Windows.Photos";
        
        // Cleanup items
        var tempCheckBox = this.FindControl<CheckBox>("TempFolderCheckBox");
        var browserCacheCheckBox = this.FindControl<CheckBox>("BrowserCacheCheckBox");
        var recentFilesCheckBox = this.FindControl<CheckBox>("RecentFilesCheckBox");
        var thumbnailCacheCheckBox = this.FindControl<CheckBox>("ThumbnailCacheCheckBox");
        
        if (tempCheckBox != null)
            appsToUninstall[tempCheckBox] = "CLEANUP_TEMP";
        if (browserCacheCheckBox != null)
            appsToUninstall[browserCacheCheckBox] = "CLEANUP_BROWSER_CACHE";
        if (recentFilesCheckBox != null)
            appsToUninstall[recentFilesCheckBox] = "CLEANUP_RECENT_FILES";
        if (thumbnailCacheCheckBox != null)
            appsToUninstall[thumbnailCacheCheckBox] = "CLEANUP_THUMBNAIL_CACHE";
    }

    /// <summary>
    /// Appends text to the output terminal with a timestamp.
    /// </summary>
    private void AppendOutput(string text)
    {
        if (outputTerminal == null) return;
        
        // Dispatch to UI thread if not already on it
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.InvokeAsync(() => AppendOutput(text));
            return;
        }
        
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
        
        // Dispatch to UI thread if not already on it
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.InvokeAsync(() => UpdateStatus(message, color));
            return;
        }
        
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

        // Show success image if all operations were successful
        if (allSuccessful && results.Count > 0)
        {
            ShowSuccessImage();
        }

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
        // Handle cleanup operations
        if (appId.StartsWith("CLEANUP_"))
        {
            await CleanupSystemCacheAsync(appId);
            return;
        }

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

    private async Task CleanupSystemCacheAsync(string cleanupType)
    {
        try
        {
            await Task.Run(() =>
            {
                string folderPath = cleanupType switch
                {
                    "CLEANUP_TEMP" => Environment.GetEnvironmentVariable("TEMP") ?? Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Temp"),
                    "CLEANUP_BROWSER_CACHE" => Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "Microsoft\\Edge\\User Data\\Default\\Cache"),
                    "CLEANUP_RECENT_FILES" => Path.Combine(Environment.GetEnvironmentVariable("APPDATA") ?? "", "Microsoft\\Windows\\Recent"),
                    "CLEANUP_THUMBNAIL_CACHE" => Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "Microsoft\\Windows\\Explorer"),
                    _ => throw new Exception($"Unknown cleanup type: {cleanupType}")
                };

                if (Directory.Exists(folderPath))
                {
                    var files = Directory.GetFiles(folderPath);
                    int deletedCount = 0;
                    long freedSpace = 0;

                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            freedSpace += fileInfo.Length;
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch
                        {
                            // Skip files that can't be deleted (in use)
                        }
                    }

                    var freedMB = freedSpace / (1024 * 1024);
                    AppendOutput($"Deleted {deletedCount} files, freed ~{freedMB}MB");
                }
                else
                {
                    AppendOutput($"Folder not found or already clean");
                }
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Cleanup failed: {ex.Message}");
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

    private void ShowSuccessImage()
    {
        if (successImage == null)
            return;

        try
        {
            // Try to load image from Assets/success.png
            string imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "success.png");
            
            if (!File.Exists(imagePath))
            {
                // Try relative path from exe
                var alternativePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "success.png");
                if (File.Exists(alternativePath))
                    imagePath = alternativePath;
                else
                    return; // Image not found, silently skip
            }

            // Load and display the image
            using (var stream = File.OpenRead(imagePath))
            {
                var bitmap = new Bitmap(stream);
                successImage.Source = bitmap;
                successImage.IsVisible = true;
            }
        }
        catch
        {
            // Silently fail if image can't be loaded
        }
    }
}