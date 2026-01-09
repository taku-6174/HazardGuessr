using System.Windows;

namespace HazardGuessr
{
    public partial class RuleWindow : Window
    {
        private string _playerName;
        private bool _isGuest;

        public RuleWindow(string playerName, bool isGuest)
        {
            InitializeComponent();
            _playerName = playerName;
            _isGuest = isGuest;
        }

        // コンストラクタのオーバーロード（互換性）
        public RuleWindow() : this("ゲスト", true)
        {
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // ホーム画面に戻る
            var homeWindow = new HomeWindow(_playerName, _isGuest);
            homeWindow.Show();
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // アプリケーションを終了
            Application.Current.Shutdown();
        }
    }
}