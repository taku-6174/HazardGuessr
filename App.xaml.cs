// App.xaml.cs
using System.Windows;

namespace HazardGuessr
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 最初にログイン画面を表示
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}