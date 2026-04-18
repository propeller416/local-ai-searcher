using Application.Interfaces;
using Microsoft.Data.Sqlite;
using System.IO;

namespace Infrastructure.Data;

public class SqliteDatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public SqliteDatabaseService(string databasePath = "database.sqlite")
    {
        var dbPath = Path.GetFullPath(databasePath);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Pooling = false
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Загружаем расширение vec0 для векторного поиска
        connection.LoadExtension("vec0");

        var createDocumentsTableCmd = connection.CreateCommand();
        createDocumentsTableCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS documents (
                Id TEXT PRIMARY KEY,
                Filename TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                ContentType TEXT,
                Status INTEGER NOT NULL,
                UploadedAt TEXT NOT NULL
            );
        ";
        await createDocumentsTableCmd.ExecuteNonQueryAsync();

        var createChunksTableCmd = connection.CreateCommand();
        createChunksTableCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS document_chunks (
                RowId INTEGER PRIMARY KEY AUTOINCREMENT,
                Id TEXT UNIQUE NOT NULL,
                DocumentId TEXT NOT NULL,
                Text TEXT NOT NULL,
                ChunkIndex INTEGER NOT NULL,
                FOREIGN KEY (DocumentId) REFERENCES documents (Id) ON DELETE CASCADE
            );
        ";
        await createChunksTableCmd.ExecuteNonQueryAsync();

        var createEmbeddingsTableCmd = connection.CreateCommand();
        createEmbeddingsTableCmd.CommandText = @"
            CREATE VIRTUAL TABLE IF NOT EXISTS chunk_embeddings USING vec0(
                id INTEGER PRIMARY KEY,
                embedding float[768]
            );
        ";
        await createEmbeddingsTableCmd.ExecuteNonQueryAsync();
    }

    public async Task<SqliteConnection> GetConnectionAsync(bool loadVecExtension = false)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        if (loadVecExtension)
        {
            connection.LoadExtension("vec0");
        }
        
        return connection;
    }
}
