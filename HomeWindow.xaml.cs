using System.Windows;

namespace HazardGuessr
{
    public partial class HomeWindow : Window
    {
        private string _playerName;
        private bool _isGuest;

        public HomeWindow(string playerName, bool isGuest)
        {
            InitializeComponent();

            _playerName = playerName;
            _isGuest = isGuest;

            // 必要ならプレイヤー名を表示
            // PlayerNameText.Text = playerName;

            // ゲストの場合はログアウトボタンを非表示にするか
            if (isGuest)
            {
                LogoutButton.Visibility = Visibility.Collapsed;
            }
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            // ゲーム画面を開く
            var mainWindow = new MainWindow(_playerName, _isGuest);
            mainWindow.Show();
            this.Close();
        }

        private void RankingButton_Click(object sender, RoutedEventArgs e)
        {
            // ランキング画面を開く
            var rankingWindow = new RankingWindow(_playerName);
            rankingWindow.Show();
            this.Close();
        }

        private void RulesButton_Click(object sender, RoutedEventArgs e)
        {
            // ルール説明画面を開く
            var ruleWindow = new RuleWindow(_playerName, _isGuest);
            ruleWindow.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // ログイン画面に戻る
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}