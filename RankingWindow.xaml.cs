using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HazardGuessr
{
    public partial class RankingWindow : Window
    {
        private string _currentPlayerName;

        public RankingWindow(string playerName)
        {
            InitializeComponent();
            _currentPlayerName = playerName;
            Loaded += RankingWindow_Loaded;
        }

        public RankingWindow() : this("ゲスト")
        {
        }

        private void RankingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRanking();
        }

        private void LoadRanking()
        {
            // クリア
            RankingStackPanel.Children.Clear();

            try
            {
                // PlayerManagerからデータ取得
                var allPlayers = PlayerManager.GetAllPlayers();

                if (allPlayers == null || !allPlayers.Any())
                {
                    ShowNoDataMessage();
                    return;
                }

                // ヘッダーを追加
                AddHeader();

                // スコア順にソート（最高スコア→累計スコア）
                var sortedPlayers = allPlayers
                    .OrderByDescending(p => p.HighScore)
                    .ThenByDescending(p => p.TotalScore)
                    .Take(10)
                    .ToList();

                // ランキングアイテムを追加
                for (int i = 0; i < sortedPlayers.Count; i++)
                {
                    var player = sortedPlayers[i];
                    AddRankingItem(i + 1, player);
                }

                // 自分の順位を表示
                ShowMyRank(allPlayers, sortedPlayers);

                // UI表示
                RankingContainer.Visibility = Visibility.Visible;
                NoDataMessage.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ランキング読み込みエラー: {ex.Message}");
                ShowNoDataMessage();
            }
        }

        private void AddHeader()
        {
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            // ヘッダー背景
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 64)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            // ヘッダーテキスト追加
            AddHeaderText(headerGrid, "順位", 0);
            AddHeaderText(headerGrid, "プレイヤー名", 1);
            AddHeaderText(headerGrid, "最高スコア", 2);
            AddHeaderText(headerGrid, "プレイ回数", 3);
            AddHeaderText(headerGrid, "最終プレイ", 4);

            headerBorder.Child = headerGrid;
            RankingStackPanel.Children.Add(headerBorder);
        }

        private void AddHeaderText(Grid grid, string text, int column)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 204))
            };

            if (column == 1) // プレイヤー名
            {
                textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else
            {
                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            }

            Grid.SetColumn(textBlock, column);
            grid.Children.Add(textBlock);
        }

        private void AddRankingItem(int rank, PlayerManager.PlayerData player)
        {
            var itemGrid = new Grid();
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            // 1. 順位（丸い背景）
            var rankBorder = new Border
            {
                Width = 50,
                Height = 50,
                CornerRadius = new CornerRadius(25),
                Background = GetRankBackground(rank),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var rankText = new TextBlock
            {
                Text = rank.ToString(),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            rankBorder.Child = rankText;
            Grid.SetColumn(rankBorder, 0);
            itemGrid.Children.Add(rankBorder);

            // 2. プレイヤー名
            var nameText = new TextBlock
            {
                Text = player.Name,
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0)
            };

            // 現在のプレイヤーかチェック
            if (player.Name == _currentPlayerName)
            {
                nameText.Foreground = Brushes.Gold;
                nameText.FontWeight = FontWeights.Bold;
                nameText.FontSize = 20;
            }
            else
            {
                nameText.Foreground = Brushes.White;
            }

            Grid.SetColumn(nameText, 1);
            itemGrid.Children.Add(nameText);

            // 3. 最高スコア
            var scoreText = new TextBlock
            {
                Text = player.HighScore.ToString("#,##0"),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Gold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(scoreText, 2);
            itemGrid.Children.Add(scoreText);

            // 4. プレイ回数
            var gamesText = new TextBlock
            {
                Text = player.GamesPlayed.ToString(),
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(gamesText, 3);
            itemGrid.Children.Add(gamesText);

            // 5. 最終プレイ
            var dateText = new TextBlock
            {
                Text = FormatShortDate(player.LastPlayed),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dateText, 4);
            itemGrid.Children.Add(dateText);

            // アイテムコンテナ
            var itemContainer = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10),
                Child = itemGrid
            };

            // ホバーエフェクト
            itemContainer.MouseEnter += (s, e) =>
            {
                itemContainer.Background = new SolidColorBrush(Color.FromArgb(30, 0, 122, 204));
            };
            itemContainer.MouseLeave += (s, e) =>
            {
                itemContainer.Background = Brushes.Transparent;
            };

            RankingStackPanel.Children.Add(itemContainer);
        }

        private Brush GetRankBackground(int rank)
        {
            return rank switch
            {
                1 => new SolidColorBrush(Color.FromRgb(255, 215, 0)),    // 金
                2 => new SolidColorBrush(Color.FromRgb(192, 192, 192)),  // 銀
                3 => new SolidColorBrush(Color.FromRgb(205, 127, 50)),   // 銅
                _ => new SolidColorBrush(Color.FromRgb(0, 122, 204))     // 青
            };
        }

        private void ShowMyRank(List<PlayerManager.PlayerData> allPlayers, List<PlayerManager.PlayerData> top10)
        {
            var myPlayer = allPlayers.FirstOrDefault(p => p.Name == _currentPlayerName);
            if (myPlayer == null)
            {
                MyRankPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // トップ10に入っているかチェック
            bool isInTop10 = top10.Any(p => p.Name == _currentPlayerName);

            if (!isInTop10)
            {
                // 全体での順位を計算
                int myRank = allPlayers
                    .OrderByDescending(p => p.HighScore)
                    .ThenByDescending(p => p.TotalScore)
                    .ToList()
                    .FindIndex(p => p.Name == _currentPlayerName) + 1;

                MyRankText.Text = $"あなたの順位: {myRank}位 (スコア: {myPlayer.HighScore:#,##0})";
                MyRankPanel.Visibility = Visibility.Visible;
            }
            else
            {
                MyRankPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowNoDataMessage()
        {
            RankingContainer.Visibility = Visibility.Collapsed;
            MyRankPanel.Visibility = Visibility.Collapsed;
            NoDataMessage.Visibility = Visibility.Visible;
        }

        private string FormatShortDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString)) return "-";

            try
            {
                // "yyyy/MM/dd HH:mm" 形式から "MM/dd" 形式に
                if (dateString.Length >= 10)
                {
                    return dateString.Substring(5, 5); // "MM/dd"
                }
                return dateString;
            }
            catch
            {
                return dateString;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRanking();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            var homeWindow = new HomeWindow(_currentPlayerName, _currentPlayerName == "ゲスト");
            homeWindow.Show();
            this.Close();
        }

    }
}