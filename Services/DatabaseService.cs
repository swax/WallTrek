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
                CREATE TABLE IF NOT EXISTS Prompts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PromptText TEXT NOT NULL UNIQUE,
                    FirstUsedDate DATETIME NOT NULL,
                    LastUsedDate DATETIME NOT NULL,
                    UsageCount INTEGER NOT NULL DEFAULT 1,
                    IsFavorite INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS GeneratedImages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PromptId INTEGER NOT NULL,
                    ImagePath TEXT NOT NULL,
                    GeneratedDate DATETIME NOT NULL,
                    IsUploadedToDeviantArt INTEGER NOT NULL DEFAULT 0,
                    DeviantArtUrl TEXT NULL,
                    FOREIGN KEY (PromptId) REFERENCES Prompts (Id)
                );

                CREATE INDEX IF NOT EXISTS IX_GeneratedImages_PromptId ON GeneratedImages (PromptId);
                CREATE INDEX IF NOT EXISTS IX_GeneratedImages_GeneratedDate ON GeneratedImages (GeneratedDate);
            ";
            
            createTablesCommand.ExecuteNonQuery();
            
            // Handle migration for existing databases
            try
            {
                var migrationCommand = connection.CreateCommand();
                migrationCommand.CommandText = "ALTER TABLE Prompts ADD COLUMN IsFavorite INTEGER NOT NULL DEFAULT 0";
                migrationCommand.ExecuteNonQuery();
            }
            catch
            {
                // Column already exists, ignore the error
            }
            
            // Add DeviantArt columns to GeneratedImages if they don't exist
            try
            {
                var migrationCommand = connection.CreateCommand();
                migrationCommand.CommandText = "ALTER TABLE GeneratedImages ADD COLUMN IsUploadedToDeviantArt INTEGER NOT NULL DEFAULT 0";
                migrationCommand.ExecuteNonQuery();
            }
            catch
            {
                // Column already exists, ignore the error
            }
            
            try
            {
                var migrationCommand = connection.CreateCommand();
                migrationCommand.CommandText = "ALTER TABLE GeneratedImages ADD COLUMN DeviantArtUrl TEXT NULL";
                migrationCommand.ExecuteNonQuery();
            }
            catch
            {
                // Column already exists, ignore the error
            }
        }

        public async Task<int> AddOrUpdatePromptAsync(string promptText)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // Check if prompt exists
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT Id, UsageCount FROM Prompts WHERE PromptText = @prompt";
            selectCommand.Parameters.AddWithValue("@prompt", promptText);

            var result = await selectCommand.ExecuteScalarAsync();
            
            if (result != null)
            {
                // Update existing prompt
                var promptId = Convert.ToInt32(result);
                var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = @"
                    UPDATE Prompts 
                    SET LastUsedDate = @lastUsed, UsageCount = UsageCount + 1 
                    WHERE Id = @id";
                updateCommand.Parameters.AddWithValue("@lastUsed", DateTime.Now);
                updateCommand.Parameters.AddWithValue("@id", promptId);
                
                await updateCommand.ExecuteNonQueryAsync();
                return promptId;
            }
            else
            {
                // Insert new prompt
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"
                    INSERT INTO Prompts (PromptText, FirstUsedDate, LastUsedDate, UsageCount) 
                    VALUES (@prompt, @firstUsed, @lastUsed, 1);
                    SELECT last_insert_rowid();";
                insertCommand.Parameters.AddWithValue("@prompt", promptText);
                insertCommand.Parameters.AddWithValue("@firstUsed", DateTime.Now);
                insertCommand.Parameters.AddWithValue("@lastUsed", DateTime.Now);

                var newId = await insertCommand.ExecuteScalarAsync();
                return Convert.ToInt32(newId);
            }
        }

        public async Task AddGeneratedImageAsync(int promptId, string imagePath)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO GeneratedImages (PromptId, ImagePath, GeneratedDate) 
                VALUES (@promptId, @imagePath, @generatedDate)";
            insertCommand.Parameters.AddWithValue("@promptId", promptId);
            insertCommand.Parameters.AddWithValue("@imagePath", imagePath);
            insertCommand.Parameters.AddWithValue("@generatedDate", DateTime.Now);

            await insertCommand.ExecuteNonQueryAsync();
        }

        public async Task<List<PromptHistoryItem>> GetPromptHistoryAsync()
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT p.Id, p.PromptText, p.FirstUsedDate, p.LastUsedDate, p.UsageCount, p.IsFavorite,
                       GROUP_CONCAT(gi.ImagePath, '|') as ImagePaths,
                       GROUP_CONCAT(COALESCE(gi.IsUploadedToDeviantArt, 0), '|') as UploadStatuses,
                       GROUP_CONCAT(gi.DeviantArtUrl, '|') as DeviantArtUrls
                FROM Prompts p
                LEFT JOIN GeneratedImages gi ON p.Id = gi.PromptId
                GROUP BY p.Id, p.PromptText, p.FirstUsedDate, p.LastUsedDate, p.UsageCount, p.IsFavorite
                ORDER BY p.IsFavorite DESC, p.LastUsedDate DESC";

            var items = new List<PromptHistoryItem>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var imagePaths = reader.IsDBNull(6) ? new List<string>() : 
                    reader.GetString(6).Split('|').Where(s => !string.IsNullOrEmpty(s)).ToList();
                
                var uploadStatuses = reader.IsDBNull(7) ? new List<bool>() :
                    reader.GetString(7).Split('|').Select(s => s == "1").ToList();
                    
                var deviantArtUrls = reader.IsDBNull(8) ? new List<string?>() :
                    reader.GetString(8).Split('|').Select(s => string.IsNullOrEmpty(s) ? null : s).ToList();

                // Ensure all lists have the same length
                while (uploadStatuses.Count < imagePaths.Count) uploadStatuses.Add(false);
                while (deviantArtUrls.Count < imagePaths.Count) deviantArtUrls.Add(null);

                var imageItems = new List<ImageHistoryItem>();
                for (int i = 0; i < imagePaths.Count; i++)
                {
                    imageItems.Add(new ImageHistoryItem
                    {
                        ImagePath = imagePaths[i],
                        IsUploadedToDeviantArt = i < uploadStatuses.Count ? uploadStatuses[i] : false,
                        DeviantArtUrl = i < deviantArtUrls.Count ? deviantArtUrls[i] : null
                    });
                }

                items.Add(new PromptHistoryItem
                {
                    Id = reader.GetInt32(0),
                    PromptText = reader.GetString(1),
                    FirstUsedDate = reader.GetDateTime(2),
                    LastUsedDate = reader.GetDateTime(3),
                    UsageCount = reader.GetInt32(4),
                    IsFavorite = reader.GetInt32(5) == 1,
                    ImagePaths = imagePaths, // Keep for backward compatibility
                    ImageItems = imageItems
                });
            }

            return items;
        }

        public async Task DeletePromptAsync(int promptId)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // Delete associated images first
            var deleteImagesCommand = connection.CreateCommand();
            deleteImagesCommand.CommandText = "DELETE FROM GeneratedImages WHERE PromptId = @promptId";
            deleteImagesCommand.Parameters.AddWithValue("@promptId", promptId);
            await deleteImagesCommand.ExecuteNonQueryAsync();

            // Delete the prompt
            var deletePromptCommand = connection.CreateCommand();
            deletePromptCommand.CommandText = "DELETE FROM Prompts WHERE Id = @promptId";
            deletePromptCommand.Parameters.AddWithValue("@promptId", promptId);
            await deletePromptCommand.ExecuteNonQueryAsync();
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

        public async Task SetFavoriteAsync(int promptId, bool isFavorite)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE Prompts SET IsFavorite = @isFavorite WHERE Id = @promptId";
            updateCommand.Parameters.AddWithValue("@promptId", promptId);
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
    }

    public class ImageHistoryItem : INotifyPropertyChanged
    {
        public string ImagePath { get; set; } = string.Empty;
        public bool IsUploadedToDeviantArt { get; set; }
        public string? DeviantArtUrl { get; set; }

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

    public class PromptHistoryItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string PromptText { get; set; } = string.Empty;
        public DateTime FirstUsedDate { get; set; }
        public DateTime LastUsedDate { get; set; }
        public int UsageCount { get; set; }
        public List<string> ImagePaths { get; set; } = new List<string>(); // Keep for backward compatibility
        public List<ImageHistoryItem> ImageItems { get; set; } = new List<ImageHistoryItem>();

        private bool _isFavorite = false;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
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
}