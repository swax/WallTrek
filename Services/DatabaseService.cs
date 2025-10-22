using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace WallTrek.Services
{
    public class DatabaseService
    {
        private readonly string connectionString;
        private readonly string databasePath;

        public DatabaseService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var wallTrekPath = Path.Combine(appDataPath, "WallTrek");
            Directory.CreateDirectory(wallTrekPath);
            
            databasePath = Path.Combine(wallTrekPath, "walltrek.db");
            connectionString = $"Data Source={databasePath}";
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var createTablesCommand = connection.CreateCommand();
            createTablesCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS GeneratedImages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ImagePath TEXT NOT NULL,
                    GeneratedDate DATETIME NOT NULL,
                    IsUploadedToDeviantArt INTEGER NOT NULL DEFAULT 0,
                    DeviantArtUrl TEXT NULL,
                    LlmModel TEXT NOT NULL DEFAULT 'gpt5',
                    ImgModel TEXT NOT NULL DEFAULT 'dalle3',
                    PromptText TEXT NOT NULL DEFAULT '',
                    IsFavorite INTEGER NOT NULL DEFAULT 0
                );

                CREATE INDEX IF NOT EXISTS IX_GeneratedImages_GeneratedDate ON GeneratedImages (GeneratedDate);
            ";

            createTablesCommand.ExecuteNonQuery();
        }

        public async Task AddGeneratedImageAsync(string imagePath, string llmModel, string imgModel, string promptText, bool isFavorite = false)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO GeneratedImages (ImagePath, GeneratedDate, LlmModel, ImgModel, PromptText, IsFavorite)
                VALUES (@imagePath, @generatedDate, @llmModel, @imgModel, @promptText, @isFavorite)";
            insertCommand.Parameters.AddWithValue("@imagePath", imagePath);
            insertCommand.Parameters.AddWithValue("@generatedDate", DateTime.Now);
            insertCommand.Parameters.AddWithValue("@llmModel", llmModel);
            insertCommand.Parameters.AddWithValue("@imgModel", imgModel);
            insertCommand.Parameters.AddWithValue("@promptText", promptText);
            insertCommand.Parameters.AddWithValue("@isFavorite", isFavorite ? 1 : 0);

            await insertCommand.ExecuteNonQueryAsync();
        }

        public async Task DeleteImageAsync(string imagePath)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM GeneratedImages WHERE ImagePath = @imagePath";
            deleteCommand.Parameters.AddWithValue("@imagePath", imagePath);
            await deleteCommand.ExecuteNonQueryAsync();
        }

        public async Task SetImageFavoriteAsync(string imagePath, bool isFavorite)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE GeneratedImages SET IsFavorite = @isFavorite WHERE ImagePath = @imagePath";
            updateCommand.Parameters.AddWithValue("@imagePath", imagePath);
            updateCommand.Parameters.AddWithValue("@isFavorite", isFavorite ? 1 : 0);
            await updateCommand.ExecuteNonQueryAsync();
        }

        public async Task SetDeviantArtUploadAsync(string imagePath, bool isUploaded, string? deviantArtUrl = null)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE GeneratedImages SET IsUploadedToDeviantArt = @isUploaded, DeviantArtUrl = @url WHERE ImagePath = @imagePath";
            updateCommand.Parameters.AddWithValue("@imagePath", imagePath);
            updateCommand.Parameters.AddWithValue("@isUploaded", isUploaded ? 1 : 0);
            updateCommand.Parameters.AddWithValue("@url", deviantArtUrl ?? (object)DBNull.Value);
            await updateCommand.ExecuteNonQueryAsync();
        }

        public async Task<List<ImageGridItem>> GetAllImagesAsync()
        {
            return await GetImagesAsync(0, int.MaxValue);
        }

        public async Task<List<ImageGridItem>> GetImagesAsync(int offset, int limit)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();

            command.CommandText = $@"
                SELECT gi.ImagePath, gi.GeneratedDate, gi.PromptText, gi.IsFavorite, gi.IsUploadedToDeviantArt, gi.DeviantArtUrl, gi.LlmModel, gi.ImgModel
                FROM GeneratedImages gi
                ORDER BY gi.GeneratedDate DESC
                LIMIT @limit OFFSET @offset";

            command.Parameters.AddWithValue("@limit", limit);
            command.Parameters.AddWithValue("@offset", offset);

            var items = new List<ImageGridItem>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new ImageGridItem
                {
                    ImagePath = reader.GetString(0),
                    GeneratedDate = reader.GetDateTime(1),
                    PromptText = reader.GetString(2),
                    IsFavorite = reader.GetInt32(3) == 1,
                    IsUploaded = reader.IsDBNull(4) ? false : reader.GetInt32(4) == 1,
                    DeviantArtUrl = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    LlmModel = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    ImgModel = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
                });
            }

            return items;
        }

        public async Task<int> GetImageCountAsync(bool favoritesOnly, bool notUploadedOnly)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();

            var whereClause = "";
            if (favoritesOnly)
            {
                whereClause += " AND gi.IsFavorite = 1";
            }
            if (notUploadedOnly)
            {
                whereClause += " AND (gi.IsUploadedToDeviantArt = 0 OR gi.IsUploadedToDeviantArt IS NULL)";
            }

            command.CommandText = $@"
                SELECT COUNT(*)
                FROM GeneratedImages gi
                WHERE 1=1 {whereClause}";

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }

    public class ImageHistoryItem : INotifyPropertyChanged
    {
        public string ImagePath { get; set; } = string.Empty;
        public bool IsUploadedToDeviantArt { get; set; }
        public string? DeviantArtUrl { get; set; }
        public string LlmModel { get; set; } = string.Empty;
        public string ImgModel { get; set; } = string.Empty;

        private bool _isUploading = false;
        public bool IsUploading
        {
            get => _isUploading;
            set
            {
                if (_isUploading != value)
                {
                    _isUploading = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ImageGridItem
    {
        public string ImagePath { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public string PromptText { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public bool IsUploaded { get; set; }
        public string DeviantArtUrl { get; set; } = string.Empty;
        public string LlmModel { get; set; } = string.Empty;
        public string ImgModel { get; set; } = string.Empty;
    }
}