using System.IO;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;

namespace Messenger.Themes
{
    public static class ThemeManager
    {
        private static readonly string ConfigPath = "theme_config.json";
        public static bool IsDarkTheme { get; private set; }

        static ThemeManager()
        {
            LoadSettings();
        }

        private static void LoadSettings()
        {
            if (!File.Exists(ConfigPath))
            {
                IsDarkTheme = false;
                SaveSettings();
                return;
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonConvert.DeserializeObject<ThemeConfig>(json);
                IsDarkTheme = config?.IsDarkTheme ?? false;
            }
            catch
            {
                IsDarkTheme = false;
            }
        }

        private static void SaveSettings()
        {
            try
            {
                var config = new ThemeConfig { IsDarkTheme = IsDarkTheme };
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }

        public static void ApplyDarkTheme(ResourceDictionary resources)
        {
            IsDarkTheme = true;
            SaveSettings();

            var darkColors = new ResourceDictionary
            {
                // Фоны
                ["WindowBackground"] = new SolidColorBrush(Color.FromRgb(30, 30, 35)),
                ["PanelBackground"] = new SolidColorBrush(Color.FromRgb(40, 40, 45)),
                ["MessageBackgroundSelf"] = new SolidColorBrush(Color.FromRgb(0, 90, 158)),
                ["MessageBackgroundOther"] = new SolidColorBrush(Color.FromRgb(60, 60, 65)),

                // Текст
                ["TextPrimary"] = new SolidColorBrush(Colors.White),
                ["TextSecondary"] = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                ["TextDisabled"] = new SolidColorBrush(Color.FromRgb(120, 120, 120)),

                // Элементы управления
                ["ButtonBackground"] = new SolidColorBrush(Color.FromRgb(60, 60, 65)),
                ["ButtonHoverBackground"] = new SolidColorBrush(Color.FromRgb(70, 70, 75)),
                ["ButtonPressedBackground"] = new SolidColorBrush(Color.FromRgb(80, 80, 85)),

                // Границы
                ["BorderColor"] = new SolidColorBrush(Color.FromRgb(70, 70, 75)),
                ["InputBorder"] = new SolidColorBrush(Color.FromRgb(80, 80, 85)),

                // Списки
                ["ListBackground"] = new SolidColorBrush(Color.FromRgb(35, 35, 40)),
                ["ListItemHover"] = new SolidColorBrush(Color.FromRgb(50, 50, 55)),
                ["ListItemSelected"] = new SolidColorBrush(Color.FromRgb(0, 90, 158))
            };

            ApplyResourceDictionary(resources, darkColors);
        }

        public static void ApplyLightTheme(ResourceDictionary resources)
        {
            IsDarkTheme = false;
            SaveSettings();

            var lightColors = new ResourceDictionary
            {
                // Фоны
                ["WindowBackground"] = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                ["PanelBackground"] = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                ["MessageBackgroundSelf"] = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                ["MessageBackgroundOther"] = new SolidColorBrush(Color.FromRgb(240, 240, 240)),

                // Текст
                ["TextPrimary"] = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                ["TextSecondary"] = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                ["TextDisabled"] = new SolidColorBrush(Color.FromRgb(150, 150, 150)),

                // Элементы управления
                ["ButtonBackground"] = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                ["ButtonHoverBackground"] = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                ["ButtonPressedBackground"] = new SolidColorBrush(Color.FromRgb(220, 220, 220)),

                // Границы
                ["BorderColor"] = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                ["InputBorder"] = new SolidColorBrush(Color.FromRgb(200, 200, 200)),

                // Списки
                ["ListBackground"] = new SolidColorBrush(Colors.White),
                ["ListItemHover"] = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                ["ListItemSelected"] = new SolidColorBrush(Color.FromRgb(227, 242, 253))
            };

            ApplyResourceDictionary(resources, lightColors);
        }

        private static void ApplyResourceDictionary(ResourceDictionary target, ResourceDictionary source)
        {
            foreach (var key in source.Keys)
            {
                if (target.Contains(key))
                {
                    target.Remove(key);
                }

                target.Add(key, source[key]);
            }

            var window = Application.Current.MainWindow;
            if (window != null)
            {
                UpdateDynamicResources(window);
            }
        }
        private static void UpdateDynamicResources(DependencyObject obj)
        {
            if (obj == null) return;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                UpdateDynamicResources(child);

                if (child is FrameworkElement element)
                {
                    foreach (var key in element.Resources.Keys)
                    {
                        var resource = element.Resources[key];
                        if (resource is SolidColorBrush brush)
                        {
                            element.Resources[key] = brush;
                        }
                    }
                }
            }
        }

        public static void ToggleTheme(ResourceDictionary resources)
        {
            if (IsDarkTheme)
                ApplyLightTheme(resources);
            else
                ApplyDarkTheme(resources);
        }

        public static void ApplyCurrentTheme(ResourceDictionary resources)
        {
            if (IsDarkTheme)
                ApplyDarkTheme(resources);
            else
                ApplyLightTheme(resources);
        }
        
        public static event EventHandler ThemeChanged;

        public static void ApplyDarkTheme()
        {
            IsDarkTheme = true;
            SaveSettings();
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void ApplyLightTheme()
        {
            IsDarkTheme = false;
            SaveSettings();
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    internal class ThemeConfig
    {
        public bool IsDarkTheme { get; set; }
    }
}