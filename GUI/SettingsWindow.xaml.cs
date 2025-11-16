using System.Windows;
using CoreFreqWindows.Config;

namespace CoreFreqWindows.GUI;

/// <summary>
/// Settings window for configuring application preferences.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    public AppSettings UpdatedSettings { get; private set; } = null!;
    public bool SettingsSaved { get; private set; }
    public bool MinimizeToTray { get; private set; }
    public string TemperatureAlertThreshold { get; private set; } = "80";

    public SettingsWindow(AppSettings currentSettings)
    {
        InitializeComponent();
        _settings = currentSettings;
        LoadSettings();
    }

    private void LoadSettings()
    {
        UpdateIntervalTextBox.Text = _settings.UpdateInterval.ToString();
        ChartPointsTextBox.Text = "60"; // Default, could be added to settings
        
        TemperatureUnitComboBox.SelectedIndex = _settings.TemperatureUnit == "F" ? 1 : 0;
        FrequencyUnitComboBox.SelectedIndex = _settings.FrequencyUnit == "MHz" ? 1 : 0;
        
        TemperatureAlertTextBox.Text = "80"; // Default, could be added to settings
        EnableAlertsCheckBox.IsChecked = true; // Default
        
        AlwaysOnTopCheckBox.IsChecked = false; // Default
        MinimizeToTrayCheckBox.IsChecked = false; // Default
        MinimizeToTray = false;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdatedSettings = new AppSettings
            {
                UpdateInterval = int.TryParse(UpdateIntervalTextBox.Text, out var interval) ? interval : 1000,
                TemperatureUnit = TemperatureUnitComboBox.SelectedIndex == 1 ? "F" : "C",
                FrequencyUnit = FrequencyUnitComboBox.SelectedIndex == 1 ? "MHz" : "GHz",
                DefaultView = _settings.DefaultView,
                ColorTheme = _settings.ColorTheme,
                Logging = _settings.Logging
            };
            
            MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true;
            TemperatureAlertThreshold = TemperatureAlertTextBox.Text;

            SettingsSaved = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save settings:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsSaved = false;
        DialogResult = false;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CancelButton_Click(sender, e);
    }
}

