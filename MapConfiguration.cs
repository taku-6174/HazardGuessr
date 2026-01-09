// MapConfiguration.cs (新規ファイル)

public class MapConfiguration
{
    // マップの地形データ（二次元配列）
    public int[,] MapData { get; }

    // プレイヤーの開始位置
    public (int Row, int Col) StartPosition { get; }

    // ゴールの位置
    public (int Row, int Col) GoalPosition { get; }

    public MapConfiguration(int[,] mapData, (int Row, int Col) startPosition, (int Row, int Col) goalPosition)
    {
        MapData = mapData;
        StartPosition = startPosition;
        GoalPosition = goalPosition;
    }
}