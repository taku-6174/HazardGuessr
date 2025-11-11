using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HazardGuessr
{
    public partial class MainWindow : Window
    {
        // 開発環境に関する定数
        private const int TILE_SIZE = 40;

        // ゲームデータ
        // 0:道(白/薄灰), 1:川(青), 2:山(濃緑), 3:建物(薄緑)
        private List<int[,]> AllMaps = new List<int[,]>();
        private int currentRound = 0;

        // ラウンドごとの情報
        private (int Row, int Col) StartPosition = (2, 2);   // スタート地点
        private (int Row, int Col) GoalPosition = (19, 32);  // ゴール地点 (マップサイズに合わせて修正)
        private List<(int Row, int Col)> playerRoute = new List<(int Row, int Col)>();

        // 現在表示されているマップデータ（DrawMap時に更新される）
        private int[,] currentMapData;


        // ==========================================================
        // 1. コンストラクタと初期化
        // ==========================================================

        public MainWindow()
        {
            InitializeComponent();

            // マップデータをリストに格納する (具体的な地形を定義)
            // マップ1: 22行 x 36列 の平地
            AllMaps.Add(new int[,]
            {
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            });
            // マップ2: すべて川 (テスト用)
            AllMaps.Add(new int[,]
            {
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
            });
            // AllMaps.Add(new int[,] { /* Round 3 のデータ */ });


            // イベント登録
            this.KeyDown += MainWindow_KeyDown;
            this.Loaded += (sender, e) => StartRound(currentRound);
            // MapCanvasのマウスイベント登録はXAMLで行ってください
            // <Canvas Name="MapCanvas" ... MouseDown="MapCanvas_MouseDown"> 

        }


        // ==========================================================
        // 2. イベントハンドラー（キー/ボタン/マウス）
        // ==========================================================

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void MapCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 1. クリックされたピクセル座標を取得
            Point clickPoint = e.GetPosition(MapCanvas);

            // 2. ピクセル座標をグリッド座標 (Row, Col) に変換
            int col = (int)(clickPoint.X / TILE_SIZE);
            int row = (int)(clickPoint.Y / TILE_SIZE);

            // 3. マップの範囲外チェック
            if (currentMapData == null || row < 0 || row >= currentMapData.GetLength(0) || col < 0 || col >= currentMapData.GetLength(1))
            {
                return;
            }

            // 4. ルートリストに追加
            (int Row, int Col) newPoint = (row, col);

            // (A) 最初は必ずスタート地点から開始
            if (playerRoute.Count == 0)
            {
                if (newPoint.Row == StartPosition.Row && newPoint.Col == StartPosition.Col)
                {
                    playerRoute.Add(newPoint);
                    DrawRoute();
                }
            }
            // (B) 2点目以降
            else
            {
                // 最後に登録されたマスを取得
                (int lastR, int lastC) = playerRoute[playerRoute.Count - 1];

                // 行の差と列の差を絶対値で計算
                int rowDiff = System.Math.Abs(lastR - row);
                int colDiff = System.Math.Abs(lastC - col);

                // **【上下左右チェック】**
                // 縦1マス移動 (rowDiff=1, colDiff=0) または 横1マス移動 (rowDiff=0, colDiff=1) のみ許可
                bool isOrthogonalNeighbor = (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);

                if (isOrthogonalNeighbor)
                {
                    playerRoute.Add(newPoint);
                    DrawRoute();
                }
                

                // (C) ゴール地点に到達した場合の処理
                if (newPoint.Row == GoalPosition.Row && newPoint.Col == GoalPosition.Col)
                {
                    MessageBox.Show("ルート設定完了！シミュレーションを開始できます。");
                    // 今後のためにマウス入力を無効化する処理などを追加
                }
            }
        }

        // ==========================================================
        // 3. 描画ロジック
        // ==========================================================

        /// <summary>
        /// 指定したラウンドのマップを表示する
        /// </summary>
        private void StartRound(int roundIndex)
        {
            if (roundIndex < AllMaps.Count)
            {
                // 描画したいマップデータを取り出し、現在のマップデータとして保存
                int[,] nextMapData = AllMaps[roundIndex];
                currentMapData = nextMapData;

                // 新しいマップデータを描画
                DrawMap(nextMapData);

                // ルートもリセット
                playerRoute.Clear();
                // (その他、現在地・避難所・災害シナリオの設定もここで行う)

                // ラウンド情報を更新
                currentRound = roundIndex;
            }
            else
            {
                MessageBox.Show("全ラウンドクリア！ゲーム終了です。");
            }
        }

        /// <summary>
        /// マップデータ配列に基づき、Canvas上にグリッドマップとアイコンを描画する
        /// </summary>
        private void DrawMap(int[,] mapToDraw)
        {
            // MapCanvasから全ての要素をクリア（前のマップ、ルート線など全て）
            MapCanvas.Children.Clear();

            int rows = mapToDraw.GetLength(0);
            int cols = mapToDraw.GetLength(1);

            MapCanvas.Width = cols * TILE_SIZE;
            MapCanvas.Height = rows * TILE_SIZE;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Brush tileBrush;
                    switch (mapToDraw[r, c])
                    {
                        case 1: // 川
                            tileBrush = Brushes.DodgerBlue;
                            break;
                        case 2: // 山
                            tileBrush = Brushes.DarkGreen;
                            break;
                        case 3: // 建物
                            tileBrush = Brushes.LightGreen;
                            break;
                        default: // 0: 道
                            tileBrush = Brushes.LightGray;
                            break;
                    }

                    Rectangle tile = new Rectangle
                    {
                        Width = TILE_SIZE,
                        Height = TILE_SIZE,
                        Fill = tileBrush,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 0.5
                    };

                    Canvas.SetLeft(tile, c * TILE_SIZE);
                    Canvas.SetTop(tile, r * TILE_SIZE);
                    MapCanvas.Children.Add(tile);
                }
            }

            // スタート地点（現在地）の描画
            Ellipse startCircle = CreateCircle(Brushes.Red);
            SetPosition(startCircle, StartPosition.Row, StartPosition.Col);
            MapCanvas.Children.Add(startCircle);

            // ゴール地点（避難所）の描画
            Ellipse goalCircle = CreateCircle(Brushes.Yellow);
            SetPosition(goalCircle, GoalPosition.Row, GoalPosition.Col);
            MapCanvas.Children.Add(goalCircle);
        }

        private void DrawRoute()
        {
            // 既存のルート線があれば削除（再描画のため）
            var existingLines = MapCanvas.Children.OfType<Polyline>().Where(p => p.Name == "RouteLine").ToList();
            foreach (var line in existingLines)
            {
                MapCanvas.Children.Remove(line);
            }

            if (playerRoute.Count < 2) return;

            PointCollection routePoints = new PointCollection();
            foreach (var p in playerRoute)
            {
                double x = p.Col * TILE_SIZE + TILE_SIZE / 2;
                double y = p.Row * TILE_SIZE + TILE_SIZE / 2;
                routePoints.Add(new Point(x, y));
            }
            Polyline routeLine = new Polyline
            {
                Name = "RouteLine",
                Points = routePoints,
                Stroke = Brushes.Gold,
                StrokeThickness = 8
            };

            // ✅ 添付プロパティ (ZIndex) の正しい設定方法
            Panel.SetZIndex(routeLine, 1); // 描画順序を最前面に設定

            MapCanvas.Children.Add(routeLine);

           
        }

        // ==========================================================
        // 4. ヘルパーメソッド
        // ==========================================================

        private Ellipse CreateCircle(Brush color)
        {
            return new Ellipse
            {
                Width = TILE_SIZE / 2,
                Height = TILE_SIZE / 2,
                Fill = color
            };
        }

        private void SetPosition(UIElement element, int r, int c)
        {
            // マスの中心に配置するために、オフセットを考慮
            Canvas.SetLeft(element, c * TILE_SIZE + TILE_SIZE / 4);
            Canvas.SetTop(element, r * TILE_SIZE + TILE_SIZE / 4);
        }
    }
}