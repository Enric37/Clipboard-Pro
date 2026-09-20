using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using ClipboardPro.Models;
using ClipboardPro.Services;

namespace ClipboardPro;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings; private readonly ClipDatabase _database;
    public SettingsWindow(SettingsService settings, ClipDatabase database) { InitializeComponent(); _settings=settings; _database=database; ShowGeneral(); }
    private TextBlock Heading(string text) => new() { Text=text, FontSize=21, FontWeight=FontWeights.SemiBold, Margin=new Thickness(0,0,0,20) };
    private System.Windows.Controls.CheckBox Check(string text, Func<bool> get, Action<bool> set) { var c=new System.Windows.Controls.CheckBox { Content=text, IsChecked=get(), Margin=new Thickness(0,6,0,6), Foreground=(System.Windows.Media.Brush)FindResource("TextBrush") }; c.Checked += async (_,_)=>{set(true);await Save();};c.Unchecked += async (_,_)=>{set(false);await Save();};return c; }
    private TextBlock Info(string text) => new() { Text=text, TextWrapping=TextWrapping.Wrap, Foreground=(System.Windows.Media.Brush)FindResource("MutedBrush"), Margin=new Thickness(0,0,0,15) };
    private async Task Save() => await _settings.SaveAsync();
    private void General_Click(object sender,RoutedEventArgs e)=>ShowGeneral(); private void History_Click(object sender,RoutedEventArgs e)=>ShowHistory(); private void Privacy_Click(object sender,RoutedEventArgs e)=>ShowPrivacy(); private void Storage_Click(object sender,RoutedEventArgs e)=>ShowStorage(); private void About_Click(object sender,RoutedEventArgs e)=>ShowAbout();
    private void ShowGeneral()
    {
        ContentPanel.Children.Clear(); ContentPanel.Children.Add(Heading("General")); ContentPanel.Children.Add(Check("Iniciar Clipboard Pro con Windows",()=>_settings.Current.StartWithWindows,v=>{_settings.Current.StartWithWindows=v;StartupService.SetEnabled(v);})); ContentPanel.Children.Add(Check("Mantener Clipboard Pro activo en segundo plano",()=>_settings.Current.KeepRunning,v=>_settings.Current.KeepRunning=v)); ContentPanel.Children.Add(Check("Cerrar panel después de pegar",()=>_settings.Current.CloseAfterPaste,v=>_settings.Current.CloseAfterPaste=v)); ContentPanel.Children.Add(new TextBlock { Text="Apariencia", FontWeight=FontWeights.SemiBold, Margin=new Thickness(0,18,0,6) }); var appearance=new StackPanel { Orientation=Orientation.Horizontal }; foreach(var t in new[]{"System","Light","Dark"}) { var b=new Button { Content=t, Tag=t }; b.Click += async (_,_)=>{_settings.Current.Theme=(string)b.Tag; App.ApplyTheme(_settings.Current.Theme);await Save();};appearance.Children.Add(b); } ContentPanel.Children.Add(appearance); ContentPanel.Children.Add(Info("Atajo principal: Ctrl + Shift + V\nWin + V se reserva para el historial nativo de Windows y nunca se intercepta."));
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
