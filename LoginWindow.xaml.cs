using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HazardGuessr
{
    public partial class LoginWindow : Window
    {
        public string SelectedPlayerName { get; private set; }
        public bool IsGuest { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PlayerNameTextBox.Focus();

            // Enterキーでログイン
            PlayerNameTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && GuestCheckBox.IsChecked != true)
                {
                    StartButton_Click(s, e);
                }
            };

            PasswordTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && GuestCheckBox.IsChecked != true)
                {
                    StartButton_Click(s, e);
                }
            };

            UpdateUI();
        }

        private void UpdateUI()
        {
            bool isGuest = GuestCheckBox.IsChecked == true;

            if (isGuest)
            {
                // ゲストモード
                SubtitleText.Text = "ゲストプレイ";
                ModeText.Text = "ゲストモード";
                ModeText.Foreground = Brushes.Orange;
                StartButton.Content = "ゲストで開始";
                SelectedPlayerName = "ゲスト";
                IsGuest = true;

                // 入力欄を無効化
                PlayerNameTextBox.IsEnabled = false;
                PasswordTextBox.IsEnabled = false;

                // ゲストテキストを設定
                PlayerNameTextBox.Text = "ゲスト";
                PasswordTextBox.Text = "";

                // 視覚的な無効化
                PlayerNameTextBox.Background = Brushes.Gray;
                PasswordTextBox.Background = Brushes.Gray;
            }
            else
            {
                // 通常モード
                IsGuest = false;

                // 入力欄を有効化
                PlayerNameTextBox.IsEnabled = true;
                PasswordTextBox.IsEnabled = true;

                // 背景色を通常に戻す
                PlayerNameTextBox.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x40));
                PasswordTextBox.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x40));

                // ゲストモードから戻ったときにテキストをクリア
                if (PlayerNameTextBox.Text == "ゲスト")
                {
                    PlayerNameTextBox.Text = "";
                }

                string username = PlayerNameTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(username))
                {
                    // ユーザー名が空
                    SubtitleText.Text = "ログイン";
                    ModeText.Text = "ユーザー名を入力してください";
                    ModeText.Foreground = Brushes.Yellow;
                    StartButton.Content = "ログイン";
                    StartButton.IsEnabled = false;
                }
                else
                {
                    // ユーザー名チェック
                    if (PlayerManager.UsernameExists(username))
                    {
                        // 既存ユーザー
                        if (PlayerManager.IsExistingPlayerWithoutPassword(username))
                        {
                            // パスワード未設定ユーザー（初回パスワード設定）
                            SubtitleText.Text = "パスワード設定";
                            ModeText.Text = "パスワードを設定してください（初回）";
                            ModeText.Foreground = Brushes.Cyan;
                            PasswordLabel.Text = "新しいパスワード:";
                            StartButton.Content = "パスワードを設定";
                        }
                        else
                        {
                            // 通常ログイン
                            SubtitleText.Text = "ログイン";
                            ModeText.Text = "ログインモード";
                            ModeText.Foreground = Brushes.LightGreen;
                            PasswordLabel.Text = "パスワード:";
                            StartButton.Content = "ログイン";
                        }
                    }
                    else
                    {
                        // 新規ユーザー
                        SubtitleText.Text = "新規登録";
                        ModeText.Text = "新規登録モード";
                        ModeText.Foreground = Brushes.Cyan;
                        PasswordLabel.Text = "パスワード:";
                        StartButton.Content = "登録して開始";
                    }
                }
            }

            // ボタンの有効/無効を更新
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            if (GuestCheckBox.IsChecked == true)
            {
                // ゲストモード：常に有効
                StartButton.IsEnabled = true;
                return;
            }

            string username = PlayerNameTextBox.Text.Trim();
            string password = PasswordTextBox.Text;

            // ユーザー名とパスワードが入力されているか
            bool hasUsername = !string.IsNullOrWhiteSpace(username);
            bool hasPassword = !string.IsNullOrWhiteSpace(password);

            // パスワードの最小長チェック（新規登録時のみ）
            bool passwordValid = true;
            if (hasPassword && !PlayerManager.UsernameExists(username))
            {
                // 新規ユーザーの場合はパスワード長チェック
                passwordValid = password.Length >= 4;
            }

            StartButton.IsEnabled = hasUsername && hasPassword && passwordValid;
        }

        private void PlayerNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            InfoText.Visibility = Visibility.Collapsed;
            UpdateUI();
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateButtonState();
        }

        private void GuestCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateUI();
        }

        private void GuestCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateUI();
            PlayerNameTextBox.Focus();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            InfoText.Visibility = Visibility.Collapsed;

            if (GuestCheckBox.IsChecked == true)
            {
                // ゲストモード
                SelectedPlayerName = "ゲスト";
                IsGuest = true;
                StartGame();
                return;
            }

            string username = PlayerNameTextBox.Text.Trim();
            string password = PasswordTextBox.Text;

            // 基本バリデーション
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("ユーザー名を入力してください");
                PlayerNameTextBox.Focus();
                return;
            }

            if (username.Length > 10)
            {
                ShowError("ユーザー名は10文字以内で入力してください");
                PlayerNameTextBox.Focus();
                PlayerNameTextBox.SelectAll();
                return;
            }

            // ユーザーが存在するかチェック
            bool userExists = PlayerManager.UsernameExists(username);

            if (userExists)
            {
                // 既存ユーザー
                if (PlayerManager.IsExistingPlayerWithoutPassword(username))
                {
                    // パスワード未設定ユーザー（初回パスワード設定）
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        ShowError("パスワードを入力してください");
                        PasswordTextBox.Focus();
                        return;
                    }

                    if (password.Length < 4)
                    {
                        ShowError("パスワードは4文字以上で入力してください");
                        PasswordTextBox.Focus();
                        return;
                    }

                    if (PlayerManager.SetPassword(username, password))
                    {
                        ShowInfo("パスワードを設定しました");
                        SelectedPlayerName = username;
                        IsGuest = false;
                    }
                    else
                    {
                        ShowError("パスワードの設定に失敗しました");
                        return;
                    }
                }
                else
                {
                    // 通常ログイン
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        ShowError("パスワードを入力してください");
                        PasswordTextBox.Focus();
                        return;
                    }

                    if (!PlayerManager.Authenticate(username, password))
                    {
                        ShowError("ユーザー名またはパスワードが間違っています");
                        PasswordTextBox.Focus();
                        PasswordTextBox.SelectAll();
                        return;
                    }

                    SelectedPlayerName = username;
                    IsGuest = false;
                }
            }
            else
            {
                // 新規ユーザー
                if (string.IsNullOrWhiteSpace(password))
                {
                    ShowError("パスワードを入力してください");
                    PasswordTextBox.Focus();
                    return;
                }

                if (password.Length < 4)
                {
                    ShowError("パスワードは4文字以上で入力してください");
                    PasswordTextBox.Focus();
                    return;
                }

                if (PlayerManager.RegisterPlayer(username, password))
                {
                    ShowInfo("新規登録が完了しました");
                    SelectedPlayerName = username;
                    IsGuest = false;
                }
                else
                {
                    ShowError("ユーザー登録に失敗しました");
                    return;
                }
            }

            // ゲーム開始
            StartGame();
        }

        private void StartGame()
        {
            var homeWindow = new HomeWindow(SelectedPlayerName, IsGuest);
            homeWindow.Show();
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void ShowInfo(string message)
        {
            InfoText.Text = message;
            InfoText.Visibility = Visibility.Visible;
        }
    }
}