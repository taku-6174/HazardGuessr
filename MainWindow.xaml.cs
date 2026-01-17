using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading; // DispatcherTimer を使うために必要

namespace HazardGuessr
{
    public partial class MainWindow : Window
    {
        // 開発環境に関する定数
        private const int TILE_SIZE = 40;

        // ゲームデータ
        // 0:道(白/薄灰), 1:川(青), 2:山(濃緑), 3:建物(薄緑)
        private List<MapConfiguration> AllMaps = new List<MapConfiguration>();
        private List<MapConfiguration> gameRounds = new List<MapConfiguration>();
        private Random random = new Random();
        private int currentRound = 0;
        private bool isRouteSettingLocked = false; // ルート設定が確定したかを示すフラグ
        private string _playerName;
        private bool _isGuest;

        // ラウンドごとの情報

        private List<(int Row, int Col)> playerRoute = new List<(int, int)>();
        private Button _startButton;

        // 現在表示されているマップデータ（DrawMap時に更新される）
        private int[,] currentMapData;

        //  シミュレーション関連の変数 
        private DispatcherTimer _simulationTimer;
        private int _currentRouteIndex = 0; // シミュレーション中の現在ルートインデックス
        private Ellipse _simulationCharacter; // シミュレーションで動かすキャラクターアイコン (スタート地点の赤丸)
        private bool isSimulationRunning = false; // シミュレーション実行中フラグ
        private MapConfiguration currentMapConfig;



        // MainWindow.xaml.cs のクラスメンバー変数の定義部分に追加

        // スコア計算用
        private int _totalSimulationSteps = 0; // シミュレーションで経過した時間ステップ数 (1ステップ = 0.5秒)
                                               // 基本スコアを 5000 に変更 
        private const int BASE_SCORE = 5000;
        //  ルート長ペナルティの最大値を 1500 に大幅増加 
        private const int MAX_ROUTE_PENALTY = 2000;
        //  ゲームオーバー時のペナルティを 3000 に増加 
        private const int HAZARD_PENALTY = 3150;

        private TextBlock _scoreDisplay;

        private TextBlock _roundDisplay;
        private TextBlock _timeDisplay;

        private DispatcherTimer _routeTimer; // ルート決定用のタイマー
        private int _routeTimeSeconds = 0;   // ルート決定に要した時間（秒）
        private const int TIME_LIMIT_SECONDS = 60; // 制限時間 (例: 30秒)

        private int totalGameScore = 0;

        // ==========================================================
        // 1. コンストラクタと初期化
        // ==========================================================


        public MainWindow(string playerName, bool isGuest)
        {
            _playerName = playerName;
            _isGuest = isGuest;
            InitializeComponent();

            _startButton = StartButton;
            _scoreDisplay = ScoreDisplay;

            _roundDisplay = RoundDisplay;
            _timeDisplay = TimeDisplay;

            // 1. MapFactoryから、定義済みの全マップ設定（MapConfiguration）をロードする
            // AllMapsが MapConfiguration 型のリストであることを前提とします。
            // AllMaps = new List<MapConfiguration>(); のように定義が必要です。
            AllMaps.Clear();
            AllMaps.AddRange(MapFactory.AllAvailableMaps);

            // 2. ロードした全マップの中から、ゲーム用の5ラウンド分のマップをランダムに選択する
            SelectGameRounds();

            // --- マップのロード・選択処理は以上です。元のコードに戻ります。 ---

            // イベント登録
            this.KeyDown += MainWindow_KeyDown;
            // Window.Loadedイベントでゲームを開始する
            this.Loaded += (sender, e) => StartRound(currentRound);

            // DispatcherTimerの初期化
            _simulationTimer = new DispatcherTimer();
            _simulationTimer.Interval = TimeSpan.FromSeconds(0.5); // 0.5秒ごとに更新
            _simulationTimer.Tick += _simulationTimer_Tick;
        }
        private void SelectGameRounds()
        {
            gameRounds.Clear();

            // 5ラウンド分をランダムに選ぶ（重複なし）
            const int MAX_ROUNDS = 5;

            // 利用可能なマップのインデックスリストを作成
            List<int> availableIndices = new List<int>();
            for (int i = 0; i < AllMaps.Count; i++)
            {
                availableIndices.Add(i);
            }

            // 5ラウンド分のマップをランダムに選択（重複なし）
            for (int i = 0; i < MAX_ROUNDS; i++)
            {
                if (availableIndices.Count == 0)
                {
                    // マップが足りない場合は既に選んだマップから再利用
                    // またはエラー処理
                    MessageBox.Show("マップが不足しています。");
                    break;
                }

                // ランダムなインデックスを選択
                int randomIndexInAvailable = random.Next(0, availableIndices.Count);
                int selectedMapIndex = availableIndices[randomIndexInAvailable];

                // 選ばれたマップをゲームラウンドに追加
                gameRounds.Add(AllMaps[selectedMapIndex]);

                // 選択したインデックスを利用可能リストから削除
                availableIndices.RemoveAt(randomIndexInAvailable);
            }
        }
        // ==========================================================
        // 2. イベントハンドラー（キー/ボタン/マウス）
        // ==========================================================

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // メッセージを状況に応じて変更
            string message;
            string title = "確認";

            if (isSimulationRunning || playerRoute.Count > 1 || currentRound > 0)
            {
                message = "ゲームを中断してホーム画面に戻りますか？\n現在の進捗は保存されません。";
                title = "ゲーム中断の確認";
            }
            else
            {
                message = "ホーム画面に戻りますか？";
            }

            // カスタム確認ダイアログを表示
            var dialog = new ConfirmDialog(message, title);
            dialog.Owner = this; // 親ウィンドウを設定

            // ダイアログを表示して結果を待つ
            bool? result = dialog.ShowDialog();

            if (result == true) // 「はい」が押された場合
            {
                // ホーム画面を表示
                var homeWindow = new HomeWindow(_playerName, _isGuest);
                homeWindow.Show();
                _simulationTimer.Stop();
                _routeTimer?.Stop();
                // 現在のゲーム画面を閉じる
                this.Close();
            }
        }

        // シミュレーション開始ボタンに割り当てることを想定したメソッド
        public void StartSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            string buttonText = clickedButton.Content.ToString();

            if (buttonText == "シミュレーション開始")
            {
                if (!isSimulationRunning)
                {
                    StartSimulation();
                }
            }
            else if (buttonText == "次のマップへ")
            {
                currentRound++;
                StartRound(currentRound);
            }
            else if (buttonText.Contains("結果を確認"))
            {
                // ScoreWindowに移動 
                HandleGameEnd();
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // シミュレーション実行中はキー操作を受け付けない
            if (isSimulationRunning) return;
            // ルート設定がロックされている場合もキー操作を受け付けない
            if (isRouteSettingLocked) return;

            // 1. Backspaceでルートを一つ前に戻る (Undo)
            if (e.Key == Key.Back)
            {
                // ルートが2点以上ある場合 (スタート地点だけの場合は戻らない)
                if (playerRoute.Count > 1)
                {
                    playerRoute.RemoveAt(playerRoute.Count - 1);
                    DrawRoute();
                }
                e.Handled = true; // ビープ音を防ぐ
                return;
            }

            // 2. 十字キーでルートを追加
            if (playerRoute.Count > 0)
            {
                (int r, int c) = playerRoute.Last(); // 現在のルートの最後の座標を取得
                int nextR = r;
                int nextC = c;
                bool moved = false;

                // 十字キーによる次の座標の決定
                if (e.Key == Key.Up) { nextR -= 1; moved = true; }
                else if (e.Key == Key.Down) { nextR += 1; moved = true; }
                else if (e.Key == Key.Left) { nextC -= 1; moved = true; }
                else if (e.Key == Key.Right) { nextC += 1; moved = true; }

                if (moved)
                {
                    int maxR = currentMapData.GetLength(0);
                    int maxC = currentMapData.GetLength(1);

                    // マップ範囲外チェック
                    if (nextR >= 0 && nextR < maxR && nextC >= 0 && nextC < maxC)
                    {
                        int nextTileType = currentMapData[nextR, nextC];

                        // ：既に通ったマスかチェック 
                        bool isAlreadyVisited = playerRoute.Any(point => point.Item1 == nextR && point.Item2 == nextC);

                        // 地形タイプ '0' (道、道路) の場合、またはゴール地点の場合のみ移動を許可
                        //  一度も通っていないマスにのみ移動可能 
                        if ((nextTileType == 0 || (nextR == currentMapConfig.GoalPosition.Row && nextC == currentMapConfig.GoalPosition.Col))
                            && !isAlreadyVisited) // ← この条件を追加
                        {
                            // 既に同じマスにいる場合は追加しない（既に上でチェック済み）
                            playerRoute.Add((nextR, nextC));
                            DrawRoute();

                            // ゴール地点に到達した場合の処理
                            if (nextR == currentMapConfig.GoalPosition.Row && nextC == currentMapConfig.GoalPosition.Col)
                            {
                                // 必要ならここで何か処理を追加
                            }
                        }
                    }
                    e.Handled = true; // イベント処理済み
                }
            }
        }
        // マウス操作のイベントハンドラー（このメソッドは空で残します）
        private void MapCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // ルート設定はキーボード操作とStartRoundで自動開始するため、このメソッドは特に何もしない
        }

        // ==========================================================
        // 3. ルート確定とシミュレーションロジック
        // ==========================================================

        /// <summary>
        /// ルート設定を確定し、シミュレーションを開始する準備をする
        /// </summary>
        private void LockRoute()
        {
            if (isRouteSettingLocked) return;

            // 1. ルートがゴールに到達しているかチェック
            if (playerRoute.Count > 0 &&
                playerRoute.Last().Item1 == currentMapConfig.GoalPosition.Row &&
                playerRoute.Last().Item2 == currentMapConfig.GoalPosition.Col)
            {
                isRouteSettingLocked = true;
                // MessageBox.Show($"ルート確定！シミュレーションを開始します。ルート長: {playerRoute.Count} マス");
            }
            else
            {
                MessageBox.Show("ゴール地点に到達していません。ルートを完成させてからボタンを押してください。");
            }
        }

        private void StartSimulation()
        {
            if (!isRouteSettingLocked)
            {
                LockRoute();
                if (!isRouteSettingLocked) return;
            }

            if (playerRoute.Count <= 1)
            {
                MessageBox.Show("ルートが設定されていません。");
                return;
            }

            isSimulationRunning = true;
            _currentRouteIndex = 0;

            // キャラクターが既にある場合は位置だけ更新
            if (_simulationCharacter == null)
            {
                //  シミュレーション用も同じ大きさに
                _simulationCharacter = CreateCircle(Brushes.Red, sizeRatio: 0.8);
                MapCanvas.Children.Add(_simulationCharacter);
            }

            // スタート位置に設定
            SetPosition(_simulationCharacter, playerRoute[0].Item1, playerRoute[0].Item2);
            Panel.SetZIndex(_simulationCharacter, 3); // シミュレーション中は最前面に

            _routeTimer?.Stop();
            _simulationTimer.Start();
        }

        private void StopSimulation(string message)
        {
            _simulationTimer.Stop();
            isSimulationRunning = false;

            // スコア計算
            bool isGameOver = message.Contains("ゲームオーバー");
            int finalScore = CalculateScore(isGameOver);

            // 合計スコアを加算
            totalGameScore += finalScore;



            // スコア表示
            if (_scoreDisplay != null)
            {
                _scoreDisplay.Text = $"{finalScore} P";
                _scoreDisplay.Foreground = isGameOver ? Brushes.Red : Brushes.Gold;
            }

            //  最終ラウンドかどうかチェック 
            bool isFinalRound = (currentRound + 1 >= gameRounds.Count);

            if (isFinalRound)
            {
                //  最終ラウンド：結果表示ボタンに変更 
                if (_startButton != null)
                {
                    _startButton.Content = $"結果を確認";
                    // ボタンの色を変更（ゴールド色など）
                    _startButton.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // ゴールド
                    _startButton.Foreground = Brushes.Black;

                }
            }
            else
            {
                //  通常ラウンド：次のマップへボタン 
                if (_startButton != null)
                {
                    _startButton.Content = "次のマップへ";
                    // ボタンの色を変更（緑色など）
                    _startButton.Background = new SolidColorBrush(Color.FromRgb(50, 205, 50)); // ライムグリーン
                    _startButton.Foreground = Brushes.Black;
                }
            }
        }
        private void _simulationTimer_Tick(object sender, EventArgs e)
        {
            // 1. 先にプレイヤーの移動先座標を特定
            if (_currentRouteIndex >= playerRoute.Count)
            {
                (int lastR, int lastC) = playerRoute.Last();
                if (lastR == currentMapConfig.GoalPosition.Row && lastC == currentMapConfig.GoalPosition.Col)
                {
                    StopSimulation("無事にゴールに到達しました！");
                }
                else
                {
                    StopSimulation($"時間切れにより停止しました。");
                }
                return;
            }

            (int nextR, int nextC) = playerRoute[_currentRouteIndex];

            // 2. 移動を実行 (キャラクターの位置を更新)
            if (_simulationCharacter != null)
            {
                SetPosition(_simulationCharacter, nextR, nextC);
            }

            // 3. 移動した「後」に、そのマスが既に浸水していないかチェック
            if (IsCharacterInWater(nextR, nextC))
            {
                StopSimulation("緊急事態！移動した先が既に浸水していました。ゲームオーバー！");
                return;
            }

            // 4. ゴールに到達したかチェック
            if (nextR == currentMapConfig.GoalPosition.Row && nextC == currentMapConfig.GoalPosition.Col)
            {
                StopSimulation("無事にゴールに到達しました！");
                return;
            }

            // 5. プレイヤーの移動が終わったので、ここで「水の氾濫」を進行させる
            FloodWater();

            // 6. 水が広がった結果、プレイヤーが飲み込まれていないか再チェック
            if (IsCharacterInWater(nextR, nextC))
            {
                StopSimulation("背後から水が迫ってきました！ゲームオーバー！");
                return;
            }

            _currentRouteIndex++;
            DrawMap();
        }
        private void RouteTimer_Tick(object sender, EventArgs e)
        {
            _routeTimeSeconds++;

            //  残り時間の計算 
            int remainingTime = TIME_LIMIT_SECONDS - _routeTimeSeconds;

            if (_timeDisplay != null)
            {
                //  表示テキストを残り時間に変更 
                _timeDisplay.Text = $"{remainingTime} 秒";

                //  警告色ロジックを残り時間ベースに変更 
                // 例: 残り時間が制限時間の 30% (ここでは 9秒) を切ったら警告色にする
                if (remainingTime <= TIME_LIMIT_SECONDS * 0.3)
                {
                    _timeDisplay.Foreground = Brushes.Red; // 赤色に変更
                }
                else if (remainingTime <= TIME_LIMIT_SECONDS * 0.7)
                {
                    _timeDisplay.Foreground = Brushes.Yellow; // 黄色に変更
                }
                else
                {
                    _timeDisplay.Foreground = Brushes.White; // 初期色に戻す
                }
            }

            // 制限時間オーバーの処理
            if (_routeTimeSeconds >= TIME_LIMIT_SECONDS)
            {
                _routeTimer.Stop();
                isRouteSettingLocked = true;
                _startButton.Content = "シミュレーション開始";
                MessageBox.Show("時間切れ！制限時間内にルート設定が完了しませんでした。そのままシミュレーションを開始します。");
            }
        }

        private void StartRound(int roundIndex)
        {
            if (roundIndex < gameRounds.Count)
            {
                currentMapConfig = gameRounds[roundIndex];
                int rows = currentMapConfig.MapData.GetLength(0);
                int cols = currentMapConfig.MapData.GetLength(1);
                currentMapData = new int[rows, cols];
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        currentMapData[r, c] = currentMapConfig.MapData[r, c];
                    }
                }

                MapCanvas.Width = currentMapData.GetLength(1) * TILE_SIZE;
                MapCanvas.Height = currentMapData.GetLength(0) * TILE_SIZE;

                playerRoute.Clear();
                isRouteSettingLocked = false;
                isSimulationRunning = false;
                _currentRouteIndex = 0;

                // ルートの初期化
                playerRoute.Add(currentMapConfig.StartPosition);

                // DrawMap()を呼び出す
                DrawMap();

                if (_startButton != null)
                {
                    _startButton.Content = "シミュレーション開始";
                    
                    _startButton.IsEnabled = true;
                }

                // ここが問題！_simulationCharacterをnullにしない！ 
                // 代わりに、Canvasから削除して新しい位置で作り直す
                if (_simulationCharacter != null)
                {
                    // Canvasから削除
                    MapCanvas.Children.Remove(_simulationCharacter);
                    _simulationCharacter = null; // これはOK - 新しい位置で作り直すため
                }

                //  _goalCircleもリセットする 
                if (_goalCircle != null)
                {
                    MapCanvas.Children.Remove(_goalCircle);
                    _goalCircle = null;
                }

                // ルートの初期化
                playerRoute.Add(currentMapConfig.StartPosition);

                DrawRoute(); // DrawRoute内で_simulationCharacterがnullなら作成する

                // UI更新部分も追加
                if (_roundDisplay != null)
                {
                    _roundDisplay.Text = $"{roundIndex + 1} / {gameRounds.Count}";
                }

                // スコア表示をリセット
                if (_scoreDisplay != null)
                {
                    _scoreDisplay.Text = "--";
                    _scoreDisplay.Foreground = Brushes.White;
                }

                // タイマー表示をリセット
                if (_timeDisplay != null)
                {
                    _timeDisplay.Text = $" {TIME_LIMIT_SECONDS} 秒";
                    _timeDisplay.Foreground = Brushes.White;
                }

                currentRound = roundIndex;

                // ルート決定タイマーの初期化と開始
                _routeTimer?.Stop();
                _routeTimeSeconds = 0;
                if (_routeTimer == null)
                {
                    _routeTimer = new DispatcherTimer();
                    _routeTimer.Interval = TimeSpan.FromSeconds(1);
                    _routeTimer.Tick += RouteTimer_Tick;
                }
                _routeTimer.Start();

                // スタートボタンをリセット
                if (_startButton != null)
                {
                    _startButton.Content = "シミュレーション開始";
                    _startButton.IsEnabled = true;
                }

            }
            else
            {
                HandleGameEnd();
            }
        }

        /// <summary>
        /// マップデータ配列に基づき、Canvas上にグリッドマップとアイコンを描画する
        /// </summary>
        // DrawMap の引数なしオーバーロード (メイン描画ロジックをここに実装します)
        private void DrawMap()
        {
            MapCanvas.Children.Clear();

            int rows = currentMapData.GetLength(0);
            int cols = currentMapData.GetLength(1);

            // 1. マップタイルの描画
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int tileType = currentMapData[r, c];
                    Brush tileColor = GetTileBrush(tileType);

                    Rectangle rect = new Rectangle
                    {
                        Width = TILE_SIZE,
                        Height = TILE_SIZE,
                        Fill = tileColor,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 0.5
                    };

                    Canvas.SetLeft(rect, c * TILE_SIZE);
                    Canvas.SetTop(rect, r * TILE_SIZE);
                    MapCanvas.Children.Add(rect);
                    Panel.SetZIndex(rect, 0);
                }
            }



            // 2. ルートラインの描画（黄色）
            if (playerRoute.Count >= 2)
            {
                Polyline routeLine = new Polyline();
                routeLine.Stroke = Brushes.Yellow;
                routeLine.StrokeThickness = 5;
                Panel.SetZIndex(routeLine, 1);

                foreach (var (r, c) in playerRoute)
                {
                    double x = c * TILE_SIZE + TILE_SIZE / 2;
                    double y = r * TILE_SIZE + TILE_SIZE / 2;
                    routeLine.Points.Add(new Point(x, y));
                }
                MapCanvas.Children.Add(routeLine);
            }

            // 3. スタート地点とゴール地点の描画
            if (playerRoute.Count > 0)
            {
                // シミュレーション中かどうかで描画を分ける 
                if (isSimulationRunning)
                {
                    // シミュレーション中の場合は赤い丸を現在位置に表示
                    if (_currentRouteIndex < playerRoute.Count)
                    {
                        int targetIndex = Math.Min(_currentRouteIndex, playerRoute.Count - 1);
                        (int currentR, int currentC) = playerRoute[targetIndex];

                        // 移動中の赤い丸（ZIndexを高くして最前面に）
                        Ellipse movingCircle = CreateCircle(Brushes.Red, sizeRatio: 0.8);
                        SetPosition(movingCircle, currentR, currentC);
                        Panel.SetZIndex(movingCircle, 3);
                        MapCanvas.Children.Add(movingCircle);

                        // スタート地点には何も表示しない
                    }
                }
                else
                {
                    // シミュレーション前はスタート地点に赤い丸を表示
                    Ellipse startCircle = CreateCircle(Brushes.Red, sizeRatio: 0.8);
                    SetPosition(startCircle, playerRoute[0].Item1, playerRoute[0].Item2);
                    Panel.SetZIndex(startCircle, 2);
                    MapCanvas.Children.Add(startCircle);
                }

                // ゴール地点（黄色）
                Ellipse goalCircle = CreateCircle(Brushes.Yellow, sizeRatio: 0.8);
                SetPosition(goalCircle, currentMapConfig.GoalPosition.Row, currentMapConfig.GoalPosition.Col);
                Panel.SetZIndex(goalCircle, 2);
                MapCanvas.Children.Add(goalCircle);
            }
        }
        /// <summary>
        /// プレイヤーのルート線を Canvas 上に描画する
        /// </summary>
        // DrawRoute の引数なしオーバーロード (メイン描画ロジックをここに実装します)
        private Ellipse _goalCircle; // ゴールマーカーを保持する変数を追加

        private void DrawRoute()
        {
            // DrawMap()を呼び出す 
            DrawMap();
        }
        // ==========================================================
        // 4. ヘルパーメソッド
        // ==========================================================
        private Brush GetTileBrush(int tileType)
        {
            switch (tileType)
            {
                case 0: // 道 (白/薄灰)
                    return new SolidColorBrush(Color.FromRgb(231, 231, 231)); // LightGray
                case 1: // 川 (青)
                    return new SolidColorBrush(Color.FromRgb(78, 109, 254));     // Blue
                case 2: // 山 (濃緑)
                    return new SolidColorBrush(Color.FromRgb(14, 190, 99));     // DarkGreen
                case 3: // 建物 (薄緑)
                    return new SolidColorBrush(Color.FromRgb(254, 233, 180)); // LightGreen
                default:
                    return new SolidColorBrush(Color.FromRgb(255, 255, 255)); // White
            }
        }
        /// <summary>
        /// 指定された色とサイズで円を作成します。ZIndexの既定値は2です。
        /// </summary>
        /// <param name="color">円の色</param>
        /// <param name="size">円のサイズ (既定値は TILE_SIZE * 0.9)</param>
        /// <param name="zIndex">ZIndex (既定値は 2)</param>
        private Ellipse CreateCircle(Brush color, double? sizeRatio = null, double? absoluteSize = null, int zIndex = 2)
        {
            double circleSize;

            if (absoluteSize.HasValue)
            {
                // 絶対サイズが指定された場合
                circleSize = absoluteSize.Value;
            }
            else if (sizeRatio.HasValue)
            {
                // 比率が指定された場合
                circleSize = TILE_SIZE * sizeRatio.Value;
            }
            else
            {
                // デフォルト：TILE_SIZEの90%
                circleSize = TILE_SIZE * 0.9;
            }

            Ellipse circle = new Ellipse
            {
                Width = circleSize,
                Height = circleSize,
                Fill = color
            };

            Panel.SetZIndex(circle, zIndex);
            return circle;
        }
        /// <summary>
        /// 円形要素を指定したマスの中央に配置する
        /// </summary>
        private void SetPosition(Ellipse circle, int row, int col)
        {
            if (circle == null) return;

            // マスの中央座標を計算
            double centerX = col * TILE_SIZE + (TILE_SIZE - circle.Width) / 2;
            double centerY = row * TILE_SIZE + (TILE_SIZE - circle.Height) / 2;

            Canvas.SetLeft(circle, centerX);
            Canvas.SetTop(circle, centerY);
        }
        // SetPosition はそのまま維持します
        // SetPositionの定義を以下のように変更

        /// <summary>
        /// 現在のマップデータに基づき、水を隣接するタイルに拡散させ、マップを更新する
        /// </summary>
        private void FloodWater()
        {
            int rows = currentMapData.GetLength(0);
            int cols = currentMapData.GetLength(1);
            List<(int R, int C)> nextFloodTiles = new List<(int R, int C)>();
            bool mapChanged = false;

            // 水の拡散ロジック
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (currentMapData[r, c] == 1)
                    {
                        (int dr, int dc)[] directions = { (0, 1), (0, -1), (1, 0), (-1, 0) };
                        foreach (var dir in directions)
                        {
                            int nextR = r + dir.dr;
                            int nextC = c + dir.dc;
                            if (nextR >= 0 && nextR < rows && nextC >= 0 && nextC < cols)
                            {
                                if (currentMapData[nextR, nextC] == 0)
                                {
                                    nextFloodTiles.Add((nextR, nextC));
                                }
                            }
                        }
                    }
                }
            }

            foreach (var tile in nextFloodTiles)
            {
                if (currentMapData[tile.R, tile.C] == 0)
                {
                    currentMapData[tile.R, tile.C] = 1;
                    mapChanged = true;
                }
            }

            if (mapChanged)
            {
                // DrawMap()だけを呼び出す 
                DrawMap();
            }
        }
        /// <summary>
        /// 指定された座標が水 (タイルタイプ 1) かどうかを判定する
        /// </summary>
        private bool IsCharacterInWater(int r, int c)
        {
            // マップの境界を既に超えている場合はチェックしない (ここでは既に範囲内である前提)
            if (r >= 0 && r < currentMapData.GetLength(0) && c >= 0 && c < currentMapData.GetLength(1))
            {
                return currentMapData[r, c] == 1;
            }
            return false;
        }

        // ==========================================================
        // 5. ユーティリティ・スコア計算メソッド
        // ==========================================================

        /// <summary>
        /// 現在のマップ上でスタートからゴールまでの最短ルートの長さを幅優先探索 (BFS) で計算する
        /// </summary>
        /// <returns>最短ルートの長さ。到達不可能な場合は int.MaxValue を返す</returns>
        private int CalculateShortestPathLength()
        {
            int rows = currentMapData.GetLength(0);
            int cols = currentMapData.GetLength(1);

            //  ゴール地点が到達可能かチェック 
            (int goalR, int goalC) = currentMapConfig.GoalPosition;

            // ゴールがマップ範囲外
            if (goalR < 0 || goalR >= rows || goalC < 0 || goalC >= cols)
                return int.MaxValue;

            //  ゴール地点が移動可能なタイルかチェック 
            // 道(0)または特別に許可されたタイルのみ
            int goalTileType = currentMapData[goalR, goalC];
            if (goalTileType != 0) // 道(0)以外は到達不可能
                return int.MaxValue;

            var distances = new Dictionary<(int, int), int>();
            var queue = new Queue<(int, int)>();

            //  スタート地点が移動可能かチェック 
            (int startR, int startC) = currentMapConfig.StartPosition;
            int startTileType = currentMapData[startR, startC];
            if (startTileType != 0) // スタート地点も道(0)でなければならない
                return int.MaxValue;

            distances[(startR, startC)] = 0;
            queue.Enqueue((startR, startC));

            (int dr, int dc)[] directions = { (0, 1), (0, -1), (1, 0), (-1, 0) };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int currentR = current.Item1;
                int currentC = current.Item2;
                int currentDist = distances[current];

                // ゴールに到達
                if (currentR == goalR && currentC == goalC)
                {
                    return currentDist;
                }

                foreach (var dir in directions)
                {
                    int nextR = currentR + dir.dr;
                    int nextC = currentC + dir.dc;

                    // 境界チェック
                    if (nextR < 0 || nextR >= rows || nextC < 0 || nextC >= cols)
                        continue;

                    //  移動可能なタイルタイプを明確に定義 
                    int nextTileType = currentMapData[nextR, nextC];

                    // 道(0)のみ移動可能
                    // 注意：シミュレーション中の水(1)拡散は考慮しない（初期状態で計算）
                    if (nextTileType != 0)
                        continue;

                    var next = (nextR, nextC);

                    if (!distances.ContainsKey(next))
                    {
                        distances[next] = currentDist + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            // ゴールに到達できなかった場合
            return int.MaxValue;
        }
        private int CalculateScore(bool isGameOver)
        {
            int score = BASE_SCORE; // 5000点

            // 1. ゲームオーバー
            if (isGameOver)
            {
                score -= HAZARD_PENALTY;
            }
            else
            {
                score = +100;
            }

                // 2. 最短ルートチェック
                int shortestLength = CalculateShortestPathLength();
            int actualLength = playerRoute.Count - 1;

            //  最短ルートと実際のルートが同じ長さかチェック 
            bool isShortestRoute = (shortestLength == actualLength);

            if (shortestLength != int.MaxValue && shortestLength > 0)
            {
                // 最短ルートの場合
                if (isShortestRoute)
                {
                    // 最短ルートなら効率減点なし
                    Console.WriteLine("✓ 最短ルート達成");
                }
                else
                {
                    // 最短より長い場合のみ減点
                    double efficiency = (double)shortestLength / actualLength;

                    // 効率が85%未満の場合のみ減点
                    if (efficiency < 0.85)
                    {
                        double inefficiency = 0.85 - efficiency; // 0.0〜0.85
                        int routePenalty = (int)((inefficiency / 0.85) * MAX_ROUTE_PENALTY);
                        score -= routePenalty;
                        Console.WriteLine($"ルート効率減点: 効率{efficiency:P}, 減点{routePenalty}");
                    }
                    else
                    {
                        Console.WriteLine($"ルート効率: 効率{efficiency:P}, 減点なし");
                    }
                }
            }

            // 3. 時間ペナルティ（緩やか）
            // 20秒以内：加点（ボーナスあり）
            // 20-30秒：少し減点
            // 30秒～50秒：増加減点
            int timePenalty = 0;
            

            if (_routeTimeSeconds >= 10)
            {
                if (_routeTimeSeconds < 20)
                {
                    // 20-30秒：軽い減点
                    timePenalty = (_routeTimeSeconds -10) * 23; // 最大230点
                }
                else if (20 <= _routeTimeSeconds && _routeTimeSeconds < 30)
                {
                    
                    timePenalty = 750 + (_routeTimeSeconds - 30) * 81; // 230点＋最大810点
                }
                else if (30 <= _routeTimeSeconds && _routeTimeSeconds < 60)
                {
                    
                    timePenalty = 3000 + (_routeTimeSeconds - 60) * 122; // 1040点＋無制限増加
                }
                else
                {
                    timePenalty = Math.Min(timePenalty, 4500);
                    score -= timePenalty;
                    Console.WriteLine($"時間減点: {_routeTimeSeconds}秒, 減点{timePenalty}");
                }
            }
            else
            {
                // --- 10秒までなら100点加点 ---
                timePenalty = 100;
                score += timePenalty;
            }

            // 4. 水の危険度ペナルティ（大幅緩和）
            int waterPenalty = CalculateWaterProximityPenalty();
            if (waterPenalty > 0)
            {
                score -= waterPenalty;
                Console.WriteLine($"水の危険度減点: {waterPenalty}");
            }
            else
            {
                Console.WriteLine($"水の危険度: 減点なし");
            }

            // 最低0点保証、最大5000点
            return Math.Min(BASE_SCORE, Math.Max(0, score));
           
        }

        private int CalculateWaterProximityPenalty()
        {
            int penalty = 0;
            int rows = currentMapData.GetLength(0);
            int cols = currentMapData.GetLength(1);

            foreach (var (r, c) in playerRoute)
            {
                //  変更：隣接マス（上下左右）に水がある場合のみ減点 
                (int dr, int dc)[] directions = { (0, 1), (0, -1), (1, 0), (-1, 0) };

                foreach (var dir in directions)
                {
                    int checkR = r + dir.dr;
                    int checkC = c + dir.dc;

                    if (checkR >= 0 && checkR < rows && checkC >= 0 && checkC < cols)
                    {
                        if (currentMapData[checkR, checkC] == 1)
                        {
                            penalty += 33; // 水に隣接する場合の減点
                        }
                    }
                }
            }

            //  最大減点を500点に緩和 
            return Math.Min(penalty, 500);
        }


        // MainWindow.xaml.cs
        private void HandleGameEnd()
        {
            // スコア保存
            if (!_isGuest && !string.IsNullOrEmpty(_playerName) && _playerName != "ゲスト")
            {
                PlayerManager.AddOrUpdatePlayer(_playerName, totalGameScore);
            }

            // ScoreWindowを表示
            var scoreWindow = new ScoreWindow(totalGameScore, _playerName);
            scoreWindow.Show();

            // MainWindowを閉じる
            this.Close();
        }
     
       
    }
}