using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using ClipboardPro.Models;
using ClipboardPro.Services;

namespace ClipboardPro;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings; private readonly ClipDatabase _database;
    public event EventHandler? HistoryChanged;
    public SettingsWindow(SettingsService settings, ClipDatabase database) { InitializeComponent(); _settings=settings; _database=database; ShowGeneralV2(); }
    private TextBlock Heading(string text) => new() { Text=text, FontSize=21, FontWeight=FontWeights.SemiBold, Margin=new Thickness(0,0,0,20) };
    private System.Windows.Controls.CheckBox Check(string text, Func<bool> get, Action<bool> set) { var c=new System.Windows.Controls.CheckBox { Content=text, IsChecked=get(), Margin=new Thickness(0,6,0,6), Foreground=(System.Windows.Media.Brush)FindResource("TextBrush") }; c.Checked += async (_,_)=>{set(true);await Save();};c.Unchecked += async (_,_)=>{set(false);await Save();};return c; }
    private TextBlock Info(string text) => new() { Text=text, TextWrapping=TextWrapping.Wrap, Foreground=(System.Windows.Media.Brush)FindResource("MutedBrush"), Margin=new Thickness(0,0,0,15) };
    private async Task Save() => await _settings.SaveAsync();
    private void General_Click(object sender,RoutedEventArgs e) => ShowGeneralV2(); private void History_Click(object sender,RoutedEventArgs e) => ShowHistoryWithCleanupOptions(); private void Privacy_Click(object sender,RoutedEventArgs e)=>ShowPrivacy(); private void Storage_Click(object sender,RoutedEventArgs e)=>ShowStorage(); private void About_Click(object sender,RoutedEventArgs e)=>ShowAbout();
    private void ShowGeneral()
    {
        ContentPanel.Children.Clear(); ContentPanel.Children.Add(Heading("General")); ContentPanel.Children.Add(Check("Iniciar Clipboard Pro con Windows",()=>_settings.Current.StartWithWindows,v=>{_settings.Current.StartWithWindows=v;StartupService.SetEnabled(v);})); ContentPanel.Children.Add(Check("Mantener Clipboard Pro activo en segundo plano",()=>_settings.Current.KeepRunning,v=>_settings.Current.KeepRunning=v)); ContentPanel.Children.Add(Check("Cerrar panel después de pegar",()=>_settings.Current.CloseAfterPaste,v=>_settings.Current.CloseAfterPaste=v)); ContentPanel.Children.Add(new TextBlock { Text="Apariencia", FontWeight=FontWeights.SemiBold, Margin=new Thickness(0,18,0,6) }); var appearance=new StackPanel { Orientation=Orientation.Horizontal }; foreach(var t in new[]{"System","Light","Dark"}) { var b=new Button { Content=t, Tag=t }; b.Click += async (_,_)=>{_settings.Current.Theme=(string)b.Tag; App.ApplyTheme(_settings.Current.Theme);await Save();};appearance.Children.Add(b); } ContentPanel.Children.Add(appearance); ContentPanel.Children.Add(Info("Atajo principal: Ctrl + Shift + V\nWin + V se reserva para el historial nativo de Windows y nunca se intercepta."));
    }
    private void ShowGeneralV2()
    {
        ContentPanel.Children.Clear(); ContentPanel.Children.Add(Heading("General"));
        ContentPanel.Children.Add(Check("Iniciar Clipboard Pro con Windows",()=>_settings.Current.StartWithWindows,v=>{_settings.Current.StartWithWindows=v;StartupService.SetEnabled(v);}));
        ContentPanel.Children.Add(Check("Mantener Clipboard Pro activo en segundo plano",()=>_settings.Current.KeepRunning,v=>_settings.Current.KeepRunning=v));
        ContentPanel.Children.Add(Check("Cerrar panel después de pegar",()=>_settings.Current.CloseAfterPaste,v=>_settings.Current.CloseAfterPaste=v));
        ContentPanel.Children.Add(new TextBlock { Text="Tema", FontWeight=FontWeights.SemiBold, Margin=new Thickness(0,20,0,7) });
        ContentPanel.Children.Add(Info("Elige cómo se ve Clipboard Pro. Sistema sigue el modo de Windows."));
        var themes=new StackPanel { Orientation=Orientation.Horizontal };
        themes.Children.Add(ThemeButton("Sistema","System")); themes.Children.Add(ThemeButton("Claro","Light")); themes.Children.Add(ThemeButton("Oscuro","Dark")); ContentPanel.Children.Add(themes);
        ContentPanel.Children.Add(new TextBlock { Text="Color de acento", FontWeight=FontWeights.SemiBold, Margin=new Thickness(0,22,0,7) });
        ContentPanel.Children.Add(Info("Se aplica al instante a los resaltados y controles de la aplicación."));
        var colors=new StackPanel { Orientation=Orientation.Horizontal };
        foreach(var choice in new[]{("Violeta","#FF8B72FF"),("Verde","#FF2DBE96"),("Azul","#FF3989FF"),("Coral","#FFFF7A59"),("Dorado","#FFE9B949")}) colors.Children.Add(AccentButton(choice.Item1,choice.Item2));
        ContentPanel.Children.Add(colors); ContentPanel.Children.Add(Info("El botón 📌 del panel principal contiene el anclaje y sus tamaños."));
    }
    private Button ThemeButton(string label,string value)
    {
        var selected=_settings.Current.Theme==value; var accent=(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_settings.Current.AccentColor)!;
        var button=new Button { Content=label, Width=106, Height=36, Margin=new Thickness(0,0,8,0), FontWeight=selected?FontWeights.SemiBold:FontWeights.Normal, BorderThickness=selected?new Thickness(2):new Thickness(1), BorderBrush=selected?new System.Windows.Media.SolidColorBrush(accent):(System.Windows.Media.Brush)FindResource("BorderBrush"), Background=selected?new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(96,accent.R,accent.G,accent.B)):(System.Windows.Media.Brush)FindResource("SurfaceHoverBrush") };
        button.Click += async (_,_)=> { _settings.Current.Theme=value; App.ApplyTheme(value); await Save(); ShowGeneralV2(); };
        return button;
    }
    private Button AccentButton(string name,string color)
    {
        var selected=string.Equals(_settings.Current.AccentColor,color,StringComparison.OrdinalIgnoreCase); var brush=(System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(color)!;
        var button=new Button { Content=selected?"✓":string.Empty, ToolTip=name, Width=42, Height=36, Margin=new Thickness(0,0,8,0), Padding=new Thickness(0), FontSize=16, FontWeight=FontWeights.Bold, Foreground=System.Windows.Media.Brushes.White, Background=brush, BorderThickness=selected?new Thickness(3):new Thickness(1), BorderBrush=selected?(System.Windows.Media.Brush)FindResource("TextBrush"):(System.Windows.Media.Brush)FindResource("BorderBrush") };
        button.Click += async (_,_)=> { _settings.Current.AccentColor=color; App.ApplyTheme(_settings.Current.Theme); await Save(); ShowGeneralV2(); };
        return button;
    }
    private void AddAppearanceSettings()
    {
        ContentPanel.Children.Add(new TextBlock { Text="Color de acento", FontWeight=FontWeights.SemiBold, Margin=new Thickness(0,18,0,6) });
        var colors=new StackPanel { Orientation=Orientation.Horizontal };
        foreach(var color in new[]{"#FF8B72FF","#FF2DBE96","#FF3989FF","#FFFF7A59","#FFE9B949"})
        {
            var button=new Button { Width=34, Height=30, Margin=new Thickness(0,0,6,0), Background=(System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(color)! };
            button.Click += async (_,_)=> { _settings.Current.AccentColor=color; App.ApplyTheme(_settings.Current.Theme); await Save(); };
            colors.Children.Add(button);
        }
        ContentPanel.Children.Add(colors); ContentPanel.Children.Add(Info("El color se aplica al instante a los controles y resaltados."));
    }
    private void AddClearFavoritesButton()
    {
        ContentPanel.Children.Add(Info("Al limpiar el historial se conservan únicamente los favoritos; los elementos fijados sin favorito también se eliminan."));
        var clearFavorites=new Button { Content="Limpiar favoritos", HorizontalAlignment=HorizontalAlignment.Left, Margin=new Thickness(0,4,0,0) };
        clearFavorites.Click += async (_,_)=>{if(MessageBox.Show("Se eliminarán todos los favoritos, incluidos sus archivos e imágenes guardados.",Branding.AppName,MessageBoxButton.OKCancel,MessageBoxImage.Warning)==MessageBoxResult.OK) { await _database.ClearFavoritesAsync(); HistoryChanged?.Invoke(this,EventArgs.Empty); }};
        ContentPanel.Children.Add(clearFavorites);
    }
    private void ShowHistoryWithCleanupOptions()
    {
        ContentPanel.Children.Clear(); ContentPanel.Children.Add(Heading("Historial"));
        ContentPanel.Children.Add(Check("Guardar imágenes",()=>_settings.Current.SaveImages,v=>_settings.Current.SaveImages=v));
        ContentPanel.Children.Add(Check("Guardar archivos y carpetas",()=>_settings.Current.SaveFiles,v=>_settings.Current.SaveFiles=v));
        ContentPanel.Children.Add(Check("Conservar HTML y RTF",()=>_settings.Current.SaveFormattedText,v=>_settings.Current.SaveFormattedText=v));
        ContentPanel.Children.Add(Check("Permitir duplicados",()=>_settings.Current.AllowDuplicates,v=>_settings.Current.AllowDuplicates=v));
        var clear=new Button { Content="Limpiar historial", HorizontalAlignment=HorizontalAlignment.Left, Margin=new Thickness(0,18,0,0) };
        clear.Click += async (_,_)=>{if(MessageBox.Show("Se eliminará todo el historial, incluidos los archivos e imágenes guardados. Solo se conservarán los favoritos.",Branding.AppName,MessageBoxButton.OKCancel,MessageBoxImage.Warning)==MessageBoxResult.OK) { await _database.ClearAsync(); HistoryChanged?.Invoke(this,EventArgs.Empty); }};
        ContentPanel.Children.Add(clear); AddClearFavoritesButton();
    }
    private void ShowHistory()
    {
        ContentPanel.Children.Clear(); ContentPanel.Children.Add(Heading("Historial")); ContentPanel.Children.Add(Check("Guardar imágenes",()=>_settings.Current.SaveImages,v=>_settings.Current.SaveImages=v)); ContentPanel.Children.Add(Check("Guardar archivos y carpetas",()=>_settings.Current.SaveFiles,v=>_settings.Current.SaveFiles=v)); ContentPanel.Children.Add(Check("Conservar HTML y RTF",()=>_settings.Current.SaveFormattedText,v=>_settings.Current.SaveFormattedText=v)); ContentPanel.Children.Add(Check("Permitir duplicados",()=>_settings.Current.AllowDuplicates,v=>_settings.Current.AllowDuplicates=v));
        var clear=new Button { Content="Limpiar historial", HorizontalAlignment=HorizontalAlignment.Left, Margin=new Thickness(0,18,0,0) }; clear.Click += async (_,_)=>{if(MessageBox.Show("Se eliminará el historial normal. Los favoritos y fijados se conservarán.",Branding.AppName,MessageBoxButton.OKCancel,MessageBoxImage.Warning)==MessageBoxResult.OK) await _database.ClearAsync();}; ContentPanel.Children.Add(clear);
    }
    private void ShowPrivacy()
    {
        ContentPanel.Children.Clear(); ContentPanel.Children.Add(Heading("Privacidad")); ContentPanel.Children.Add(Info("Tu portapapeles nunca sale de tu ordenador. Clipboard Pro no envía ni registra el contenido copiado.")); ContentPanel.Children.Add(Check("Intentar no guardar contenido potencialmente sensible",()=>_settings.Current.ProtectSensitiveContent,v=>_settings.Current.ProtectSensitiveContent=v));
        ContentPanel.Children.Add(new TextBlock { Text="Aplicaciones excluidas", FontWeight=FontWeights.SemiBold, Margin=new Thickness(0,16,0,8) }); var list=new ListBox { Height=145, ItemsSource=_settings.Current.ExcludedProcesses }; ContentPanel.Children.Add(list); var row=new StackPanel { Orientation=Orientation.Horizontal, Margin=new Thickness(0,10,0,0) }; var add=new TextBox { Width=200, Tag="Nombre del proceso, p. ej. KeePass" }; var button=new Button { Content="Añadir" }; button.Click += async (_,_)=>{if(!string.IsNullOrWhiteSpace(add.Text) && !_settings.Current.ExcludedProcesses.Contains(add.Text,StringComparer.OrdinalIgnoreCase)){_settings.Current.ExcludedProcesses.Add(add.Text.Trim()); await Save(); ShowPrivacy();}}; var remove=new Button{Content="Quitar seleccionado"};remove.Click += async (_,_)=>{if(list.SelectedItem is string x){_settings.Current.ExcludedProcesses.Remove(x);await Save();ShowPrivacy();}};row.Children.Add(add);row.Children.Add(button);row.Children.Add(remove);ContentPanel.Children.Add(row);
    }
    private void ShowStorage()
    {
        ContentPanel.Children.Clear(); ContentPanel.Children.Add(Heading("Almacenamiento")); ContentPanel.Children.Add(Info($"Datos locales: {Branding.DataDirectory}\nBase de datos: {Branding.DatabasePath}\nLas imágenes se almacenan en disco y se cargan bajo demanda.")); var open=new Button{Content="Abrir carpeta de datos",HorizontalAlignment=HorizontalAlignment.Left};open.Click += (_,_)=>Process.Start(new ProcessStartInfo("explorer.exe",$"\"{Branding.DataDirectory}\""){UseShellExecute=true});ContentPanel.Children.Add(open);
    }
    private void ShowAbout()
    {
        ContentPanel.Children.Clear(); ContentPanel.Children.Add(Heading(Branding.ProductName)); ContentPanel.Children.Add(Info($"Versión {Branding.Version}\nDiseñado localmente, sin cuentas ni telemetría de contenido.\n\nActualizaciones: la arquitectura no consulta la red automáticamente; un instalador puede distribuir versiones firmadas.")); var logs=new Button{Content="Abrir logs",HorizontalAlignment=HorizontalAlignment.Left};logs.Click += (_,_)=>Process.Start(new ProcessStartInfo("explorer.exe",$"\"{Branding.LogDirectory}\""){UseShellExecute=true});ContentPanel.Children.Add(logs);
    }
}
