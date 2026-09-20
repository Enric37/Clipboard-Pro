using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClipboardPro.Interop;
using ClipboardPro.Models;
using ClipboardPro.Services;

namespace ClipboardPro;

public partial class MainWindow : Window
{
    private readonly ClipDatabase _database; private readonly ClipboardCaptureService _clipboard; private readonly SettingsService _settings; private readonly ObservableCollection<ClipItem> _items = new();
    private CancellationTokenSource? _searchCts; private ClipType? _type; private bool _favorites; private int _loaded; private IntPtr _previousWindow; private bool _openingSettings;
    public MainWindow(ClipDatabase database, ClipboardCaptureService clipboard, SettingsService settings)
    {
        InitializeComponent(); _database=database; _clipboard=clipboard; _settings=settings; ClipList.ItemsSource=_items; AllFilter.Background = (System.Windows.Media.Brush)FindResource("SurfaceHoverBrush");
    }
    public async Task OpenAsync()
    {
        _previousWindow=NativeMethods.GetForegroundWindow(); await RefreshAsync();
        if (!IsVisible) { PositionNearCursor(); Show(); }
        Activate(); Topmost=true; Topmost=false; Focus(); SearchBox.Focus(); if (ClipList.Items.Count>0) ClipList.SelectedIndex=0;
    }
    private void PositionNearCursor()
    {
        var screen=System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position); var area=screen.WorkingArea;
        Left=area.Left + Math.Max(12,(area.Width-Width)/2d); Top=area.Top+Math.Max(12,(area.Height-Height)/2d-35);
    }
    public void HidePanel() { if (IsVisible) Hide(); }
    private async Task RefreshAsync(bool append=false)
    {
        if (!append) { _items.Clear(); _loaded=0; }
        try { var result=await _database.SearchAsync(SearchBox?.Text, _type, _favorites, _loaded, 100); foreach(var item in result) _items.Add(item); _loaded += result.Count; EmptyState.Visibility=_items.Count==0?Visibility.Visible:Visibility.Collapsed; }
        catch { EmptyState.Visibility=Visibility.Visible; }
    }
    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility=string.IsNullOrEmpty(SearchBox.Text)?Visibility.Visible:Visibility.Collapsed; _searchCts?.Cancel(); var cts=_searchCts=new();
        try { await Task.Delay(80,cts.Token); if (!cts.IsCancellationRequested) await RefreshAsync(); } catch (TaskCanceledException) { }
    }
    private async void Filter_Click(object sender, RoutedEventArgs e)
    {
        var b=(Button)sender; var tag=(string)b.Tag; _favorites=tag=="Favorites"; _type=Enum.TryParse<ClipType>(tag,out var value)?value:null;
        foreach(var child in ((StackPanel)b.Parent).Children.OfType<Button>()) child.Background=System.Windows.Media.Brushes.Transparent; b.Background=(System.Windows.Media.Brush)FindResource("SurfaceHoverBrush"); await RefreshAsync();
    }
    private async void ClipList_ScrollChanged(object sender, ScrollChangedEventArgs e) { if (e.VerticalOffset+e.ViewportHeight>=e.ExtentHeight-80 && _items.Count>0) await RefreshAsync(true); }
    private void ClipList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void ClipList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => PasteSelected();
    private ClipItem? Selected => ClipList.SelectedItem as ClipItem;
    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key==Key.Escape) { HidePanel(); e.Handled=true; }
        else if (e.Key==Key.Enter) { PasteSelected(); e.Handled=true; }
        else if (e.Key==Key.Delete) { DeleteSelected(); e.Handled=true; }
        else if (Keyboard.Modifiers==ModifierKeys.Control && e.Key==Key.F) { SearchBox.Focus(); SearchBox.SelectAll(); e.Handled=true; }
        else if (Keyboard.Modifiers==ModifierKeys.Control && e.Key==Key.D) { ToggleFavorite(); e.Handled=true; }
    }
    private void Window_Deactivated(object sender, EventArgs e) { if (!_openingSettings) Dispatcher.BeginInvoke(HidePanel); }
    private void Copy_Click(object sender, RoutedEventArgs e) => CopySelected();
    private void CopySelected()
    {
        if (Selected is not { } item) return; try { _clipboard.PutOnClipboard(item); _= _database.MarkUsedAsync(item.Id); } catch { MessageBox.Show("No se pudo copiar este elemento.", Branding.AppName, MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private async void Paste_Click(object sender, RoutedEventArgs e) => await PasteSelectedAsync();
    private void PasteSelected() => _ = PasteSelectedAsync();
    private async Task PasteSelectedAsync()
    {
        if (Selected is not { } item) return; CopySelected(); await _database.MarkUsedAsync(item.Id);
        if (_settings.Current.CloseAfterPaste) HidePanel();
        await Task.Delay(45); if (_previousWindow!=IntPtr.Zero) NativeMethods.SetForegroundWindow(_previousWindow); await Task.Delay(45); NativeMethods.SendPaste();
    }
    private void Favorite_Click(object sender, RoutedEventArgs e) => ToggleFavorite();
    private async void ToggleFavorite() { if (Selected is not { } item) return; item.IsFavorite=!item.IsFavorite; await _database.SetFlagAsync(item.Id,"favorite",item.IsFavorite); await RefreshAsync(); }
    private void Pin_Click(object sender, RoutedEventArgs e) => TogglePin();
    private async void TogglePin() { if (Selected is not { } item) return; item.IsPinned=!item.IsPinned; await _database.SetFlagAsync(item.Id,"pinned",item.IsPinned); await RefreshAsync(); }
    private void Delete_Click(object sender, RoutedEventArgs e) => DeleteSelected();
    private async void DeleteSelected() { if (Selected is not { } item) return; await _database.DeleteAsync(item.Id); await RefreshAsync(); }
    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { Type: ClipType.Text or ClipType.Code or ClipType.Url or ClipType.Color } item) return; var dialog=new EditClipWindow(item.Content){Owner=this}; if(dialog.ShowDialog()==true) { await _database.UpdateContentAsync(item.Id,dialog.Value); await RefreshAsync(); }
    }
    private void More_Click(object sender, RoutedEventArgs e) { if (Selected is null) return; var menu=ClipList.ContextMenu; menu.PlacementTarget=(Button)sender; menu.IsOpen=true; }
    public void OpenSettings()
    {
        _openingSettings=true; var dialog=new SettingsWindow(_settings,_database){Owner=this}; dialog.Closed += (_,_) => { _openingSettings=false; }; dialog.ShowDialog();
    }
    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();
}
