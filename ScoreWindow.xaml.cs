using System.Windows;

namespace HazardGuessr
{
    public partial class ScoreWindow : Window
    {
        private int _totalScore;
        private string _playerName;

        // 2つの引数を受け取るコンストラクタ
        public ScoreWindow(int totalScore, string playerName)
        {
            InitializeComponent();

            _totalScore = totalScore;
            _playerName = playerName;

            // スコア表示
            TotalScoreDisplay.Text = $"{totalScore} P";

            // プレイヤー名表示
            PlayerNameText.Text = $"プレイヤー: {playerName}";

            // メッセージを設定
            SetMessage(totalScore);

            // 最高スコア比較（PlayerManagerが使える場合）
            UpdateHighScoreComparison();
        }

        // 元のコンストラクタも残す（互換性）
        public ScoreWindow(int totalScore) : this(totalScore, "ゲスト")
        {
        }

        private void SetMessage(int score)
        {
            if (score >= 20000)
            {
                MessageText.Text = "🎉 素晴らしい！完璧に近いプレイです！";
                MessageText.Foreground = System.Windows.Media.Brushes.Gold;
            }
            else if (score >= 15000)
            {
                MessageText.Text = "👍 優秀なスコアです！お見事！";
                MessageText.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else if (score >= 10000)
            {
                MessageText.Text = "👏 良いスコアです！次はさらに高得点を！";
                MessageText.Foreground = System.Windows.Media.Brushes.LightBlue;
            }
            else
            {
                MessageText.Text = "💪 まずまずのスタート！次回はもっと高得点を目指そう！";
                MessageText.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void UpdateHighScoreComparison()
        {
            // PlayerManagerを使って最高スコアを比較（実装があれば）
            // if (PlayerManager.HasHighScore(_playerName, _totalScore))
            // {
            //     HighScoreComparisonText.Text = "🎊 自己ベストを更新しました！";
            //     HighScoreComparisonText.Visibility = Visibility.Visible;
            // }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // ホーム画面に戻る
            var homeWindow = new HomeWindow(_playerName, _playerName == "ゲスト");
            homeWindow.Show();
            this.Close();
        }

    }
}