using System.Windows;
namespace ClipboardPro;
public partial class EditClipWindow : Window
{
    public string Value => Editor.Text;
    public EditClipWindow(string value) { InitializeComponent(); Editor.Text=value; Editor.Focus(); Editor.SelectAll(); }
    private void Save_Click(object sender, RoutedEventArgs e) { if (!string.IsNullOrWhiteSpace(Editor.Text)) DialogResult=true; }
}
