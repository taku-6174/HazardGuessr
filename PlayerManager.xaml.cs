// PlayerManager.cs（パスワード機能追加版）
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HazardGuessr
{
    public static class PlayerManager
    {
        private static readonly string SavePath = "players.json";
        private static List<PlayerData> _players;

        public class PlayerData
        {
            public string Name { get; set; }
            public string PasswordHash { get; set; } // 追加：ハッシュ化されたパスワード
            public string Salt { get; set; }         // 追加：ソルト
            public int HighScore { get; set; }
            public int TotalScore { get; set; }
            public int GamesPlayed { get; set; }
            public string LastPlayed { get; set; }
            public bool HasPassword { get; set; }    // 追加：パスワード設定済みか
        }

        static PlayerManager()
        {
            LoadPlayers();
        }

        // ユーザー名が存在するかチェック
        public static bool UsernameExists(string username)
        {
            return _players.Any(p => p.Name == username);
        }

        // 新しいユーザーを登録（パスワード付き）
        public static bool RegisterPlayer(string username, string password)
        {
            if (UsernameExists(username))
                return false;

            string salt = GenerateSalt();
            string passwordHash = HashPassword(password, salt);

            var newPlayer = new PlayerData
            {
                Name = username,
                PasswordHash = passwordHash,
                Salt = salt,
                HasPassword = true,
                HighScore = 0,
                TotalScore = 0,
                GamesPlayed = 0,
                LastPlayed = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm")
            };

            _players.Add(newPlayer);
            SavePlayers();
            return true;
        }

        // 既存ユーザーのパスワードを追加/変更
        public static bool SetPassword(string username, string password)
        {
            var player = _players.FirstOrDefault(p => p.Name == username);
            if (player == null)
                return false;

            string salt = GenerateSalt();
            string passwordHash = HashPassword(password, salt);

            player.PasswordHash = passwordHash;
            player.Salt = salt;
            player.HasPassword = true;

            SavePlayers();
            return true;
        }

        // ログイン認証
        public static bool Authenticate(string username, string password)
        {
            var player = _players.FirstOrDefault(p => p.Name == username);
            if (player == null || !player.HasPassword)
                return false;

            string inputHash = HashPassword(password, player.Salt);
            return inputHash == player.PasswordHash;
        }

        // 既存のユーザー（パスワード未設定）かチェック
        public static bool IsExistingPlayerWithoutPassword(string username)
        {
            var player = _players.FirstOrDefault(p => p.Name == username);
            return player != null && !player.HasPassword;
        }

        // ゲスト用の一時アカウント作成
        public static void CreateGuestAccount(string username)
        {
            if (!UsernameExists(username))
            {
                var newPlayer = new PlayerData
                {
                    Name = username,
                    PasswordHash = "",
                    Salt = "",
                    HasPassword = false,
                    HighScore = 0,
                    TotalScore = 0,
                    GamesPlayed = 0,
                    LastPlayed = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm")
                };

                _players.Add(newPlayer);
                SavePlayers();
            }
        }

        // パスワードのハッシュ化
        private static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var bytes = Encoding.UTF8.GetBytes(saltedPassword);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // ソルト生成
        private static string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        // 以下は既存のメソッド（変更なし）
        public static List<string> GetPlayerNames()
        {
            return _players.Select(p => p.Name).ToList();
        }

        public static List<PlayerData> GetAllPlayers()
        {
            return _players;
        }

        public static void AddOrUpdatePlayer(string name, int scoreToAdd = 0)
        {
            var player = _players.FirstOrDefault(p => p.Name == name);

            if (player == null)
            {
                // 新規プレイヤー（ゲスト用）
                player = new PlayerData
                {
                    Name = name,
                    HighScore = scoreToAdd,
                    TotalScore = scoreToAdd,
                    GamesPlayed = 1,
                    LastPlayed = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
                    HasPassword = false,
                    PasswordHash = "",
                    Salt = ""
                };
                _players.Add(player);
            }
            else
            {
                if (scoreToAdd > player.HighScore)
                {
                    player.HighScore = scoreToAdd;
                }
                player.TotalScore += scoreToAdd;
                player.GamesPlayed++;
                player.LastPlayed = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            }

            SavePlayers();
        }

        private static void LoadPlayers()
        {
            if (File.Exists(SavePath))
            {
                try
                {
                    string json = File.ReadAllText(SavePath);
                    _players = JsonSerializer.Deserialize<List<PlayerData>>(json)
                             ?? new List<PlayerData>();

                    // 互換性処理
                    foreach (var player in _players)
                    {
                        if (player.HighScore == 0 && player.TotalScore > 0)
                        {
                            player.HighScore = player.TotalScore;
                        }
                        // 既存データにパスワードフィールドがない場合
                        if (string.IsNullOrEmpty(player.PasswordHash))
                        {
                            player.HasPassword = false;
                        }
                    }
                }
                catch
                {
                    _players = new List<PlayerData>();
                }
            }
            else
            {
                _players = new List<PlayerData>();
            }
        }

        private static void SavePlayers()
        {
            try
            {
                string json = JsonSerializer.Serialize(_players,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SavePath, json);
            }
            catch { /* エラー処理 */ }
        }
    }
}