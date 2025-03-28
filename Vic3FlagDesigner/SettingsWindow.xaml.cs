using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Text.Json;


namespace Vic3FlagDesigner
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private const string SettingsFile = "settings.json";
        private SettingsData settings;

        private MainWindow _mainWindow;

        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true // Makes the JSON file more readable
        };

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            LoadSettings();
            _mainWindow = mainWindow;
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFile))
            {
                settings = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(SettingsFile));
            }
            else
            {
                settings = new SettingsData();
            }

            PatternFolderTextBox.Text = settings.PatternFolder;
            EmblemFolderTextBox.Text = settings.EmblemFolder;
            TextureFolderTextBox.Text = settings.TextureFolder;
            ModFolderTextBox.Text = settings.ModFolder;
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            settings.PatternFolder = PatternFolderTextBox.Text;
            settings.EmblemFolder = EmblemFolderTextBox.Text;
            settings.TextureFolder = TextureFolderTextBox.Text;
            settings.ModFolder = ModFolderTextBox.Text;

            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(settings, jsonOptions));
            _mainWindow.SetSettings();
            Close();
            System.Windows.MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void BrowseFolder(System.Windows.Controls.TextBox textBox)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    textBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void BrowsePatternFolder(object sender, RoutedEventArgs e) => BrowseFolder(PatternFolderTextBox);
        private void BrowseEmblemFolder(object sender, RoutedEventArgs e) => BrowseFolder(EmblemFolderTextBox);
        private void BrowseTextureFolder(object sender, RoutedEventArgs e) => BrowseFolder(TextureFolderTextBox);
        private void BrowseModFolder(object sender, RoutedEventArgs e) => BrowseFolder(ModFolderTextBox);
    }

    public class SettingsData
    {
        public string PatternFolder { get; set; } = "";
        public string EmblemFolder { get; set; } = "";
        public string TextureFolder { get; set; } = "";
        public string ModFolder { get; set; } = "";

    }
}
