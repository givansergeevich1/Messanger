using System;
using System.Windows;
using Messenger.Models;
using Messenger.Services;
using Messenger.Views;
using System.Threading.Tasks;
using System.Net;
using Messenger.Themes; 

namespace Messenger
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // Настройка протокола безопасности
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 |
                    SecurityProtocolType.Tls11 |
                    SecurityProtocolType.Tls;

                LoadAndApplyTheme();

                var localStorage = new LocalStorageService();
                var user = localStorage.GetCurrentUser();

                if (user != null)
                {
                    User.CurrentUser = user;

                    Task.Run(async () =>
                    {
                        try
                        {
                            var firebaseService = new FirebaseService();
                            user.Status = UserStatus.Online;
                            user.LastSeen = DateTime.UtcNow;
                            await firebaseService.UpdateUserAsync(user);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating user status: {ex.Message}");
                        }
                    });

                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    MainWindow = mainWindow;
                }
                else
                {
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();
                    MainWindow = loginWindow;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical error on startup: {ex.Message}", "Error");
                Shutdown();
            }
        }

        private void LoadAndApplyTheme()
        {
            try
            {
                ThemeManager.ApplyCurrentTheme(Current.Resources);
                this.Resources = Current.Resources;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading theme: {ex.Message}");
                ThemeManager.ApplyLightTheme(Current.Resources);
            }
        }
    }
}