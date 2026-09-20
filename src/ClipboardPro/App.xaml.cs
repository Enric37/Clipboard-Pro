using System.Reflection;
using System.Windows;
using Forms = System.Windows.Forms;
using Microsoft.Win32;
using ClipboardPro.Services;

namespace ClipboardPro;

public partial class App : System.Windows.Application
{
    private Mutex? _instanceMutex; private Forms.NotifyIcon? _tray; private Forms.ToolStripMenuItem? _updateMenu; private ClipDatabase? _database; private SettingsService? _settings; private ClipboardCaptureService? _clipboard; private HotkeyService? _hotkey; private MainWindow? _window; private UpdateService? _updates; private readonly LogService _log=new();
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e); _instanceMutex=new Mutex(true,"Local\\ClipboardPro.SingleInstance",out var first);
        if (!first) { Current.Shutdown(); return; }
        try
        {
            _settings=new SettingsService(); await _settings.LoadAsync(); ApplyTheme(_settings.Current.Theme); _database=new ClipDatabase(); await _database.InitializeAsync(); await _database.CleanupAsync(_settings.Current);
            _clipboard=new ClipboardCaptureService(_database,_settings,_log); _clipboard.Start(); _hotkey=new HotkeyService(); _hotkey.Pressed += async (_,_)=>await OpenPanelAsync(); _hotkey.Start(); _window=new MainWindow(_database,_clipboard,_settings); CreateTray(); _updates=new UpdateService(_settings); _updates.UpdateAvailable += OnUpdateAvailable; _ = _updates.CheckAsync();
            if (!e.Args.Contains("--background",StringComparer.OrdinalIgnoreCase)) await OpenPanelAsync();
        }
        catch(Exception ex) { _log.Write("startup-failed",ex); System.Windows.MessageBox.Show("Clipboard Pro no pudo iniciarse. Consulta los logs locales para obtener detalles.",Branding.AppName,MessageBoxButton.OK,MessageBoxImage.Error); Shutdown(); }
    }
    private async Task OpenPanelAsync() { if (_window is not null) await _window.Dispatcher.InvokeAsync(async () => await _window.OpenAsync()); }
#pragma warning disable CS8602 // ToolStripItemCollection.Add(string) returns a non-null menu item at runtime.
    private void CreateTray()
    {
        var menu=new Forms.ContextMenuStrip(); menu.Items.Add("Abrir Clipboard Pro",null,async (_,_)=>await OpenPanelAsync()); _updateMenu=menu.Items.Add("Actualización disponible",null,(_,_)=>OpenUpdatePage()) as Forms.ToolStripMenuItem; _updateMenu.Visible=false; var pause=menu.Items.Add("Pausar historial"); pause.Click += async (_,_)=> { if(_settings is null)return; _settings.Current.CapturePaused=!_settings.Current.CapturePaused; pause.Text=_settings.Current.CapturePaused?"Reanudar historial":"Pausar historial"; await _settings.SaveAsync(); }; menu.Items.Add("Limpiar historial",null,async (_,_)=>{if(_database is not null) await _database.ClearAsync();}); menu.Items.Add("Configuración",null,(_,_)=>_window?.Dispatcher.Invoke(()=>_window?.OpenSettings())); menu.Items.Add(new Forms.ToolStripSeparator()); menu.Items.Add("Salir",null,(_,_)=>Shutdown());
        var icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? throw new InvalidOperationException("Executable path unavailable")) ?? System.Drawing.SystemIcons.Application;
        _tray=new Forms.NotifyIcon { Text=Branding.AppName, Icon=icon, ContextMenuStrip=menu, Visible=true }; _tray.DoubleClick += async (_,_)=>await OpenPanelAsync();
    }
#pragma warning restore CS8602
    private void OnUpdateAvailable(object? sender, UpdateRelease update)
    {
        Dispatcher.Invoke(() => { if(_updateMenu is not null) { _updateMenu.Text=$"Actualizar a v{update.Version}"; _updateMenu.Visible=true; } _tray?.ShowBalloonTip(7000, Branding.AppName, $"Hay una actualización disponible: v{update.Version}. Haz clic en el icono de bandeja para instalarla.", Forms.ToolTipIcon.Info); });
    }
    private void OpenUpdatePage()
    {
        if (_updates?.AvailableRelease is not { } release) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(release.DownloadUrl) { UseShellExecute=true });
    }
    internal static void ApplyTheme(string theme)
    {
        var dark=theme switch { "Light" => false, "Dark" => true, _ => UsesDarkSystemTheme() }; var r=Current.Resources;
        r["BgColor"]=(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(dark?"#FF17171C":"#FFF7F7FA"); r["SurfaceColor"]=(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(dark?"#FF202027":"#FFFFFFFF"); r["SurfaceHoverColor"]=(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(dark?"#FF2A2A34":"#FFEBEAF1"); r["TextColor"]=(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(dark?"#FFF4F2FA":"#FF202027"); r["MutedColor"]=(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(dark?"#FFA7A3B2":"#FF686574");
    }
    private static bool UsesDarkSystemTheme()
    {
        try { using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"); return (key?.GetValue("AppsUseLightTheme") as int? ?? 1) == 0; } catch { return true; }
    }
    protected override void OnExit(ExitEventArgs e) { _tray?.Dispose(); _hotkey?.Dispose(); _clipboard?.Dispose(); _instanceMutex?.ReleaseMutex(); _instanceMutex?.Dispose(); base.OnExit(e); }
}
