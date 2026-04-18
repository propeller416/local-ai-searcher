using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Infrastructure.Data.Repositories;

public class SqliteDocumentRepository : IDocumentRepository
{
    private readonly SqliteDatabaseService _dbService;

    public SqliteDocumentRepository(SqliteDatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task<IEnumerable<Document>> GetAllAsync()
    {
        var list = new List<Document>();
        using var connection = await _dbService.GetConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Filename, FilePath, ContentType, Status, UploadedAt FROM documents ORDER BY UploadedAt DESC";
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Document
            {
                Id = Guid.Parse(reader.GetString(0)),
                Filename = reader.GetString(1),
                FilePath = reader.GetString(2),
                ContentType = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Status = (DocumentStatus)reader.GetInt32(4),
                UploadedAt = DateTime.Parse(reader.GetString(5))
            });
        }
        return list;
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        using var connection = await _dbService.GetConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Filename, FilePath, ContentType, Status, UploadedAt FROM documents WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id.ToString());
        
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Document
            {
                Id = Guid.Parse(reader.GetString(0)),
                Filename = reader.GetString(1),
                FilePath = reader.GetString(2),
                ContentType = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Status = (DocumentStatus)reader.GetInt32(4),
                UploadedAt = DateTime.Parse(reader.GetString(5))
            };
        }
        return null;
    }

    public async Task AddAsync(Document doc)
    {
        using var connection = await _dbService.GetConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO documents (Id, Filename, FilePath, ContentType, Status, UploadedAt) 
            VALUES (@Id, @Filename, @FilePath, @ContentType, @Status, @UploadedAt)";
        
        command.Parameters.AddWithValue("@Id", doc.Id.ToString());
        command.Parameters.AddWithValue("@Filename", doc.Filename);
        command.Parameters.AddWithValue("@FilePath", doc.FilePath);
        command.Parameters.AddWithValue("@ContentType", doc.ContentType);
        command.Parameters.AddWithValue("@Status", (int)doc.Status);
        command.Parameters.AddWithValue("@UploadedAt", doc.UploadedAt.ToString("o"));
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Document doc)
    {
        using var connection = await _dbService.GetConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE documents 
            SET Filename = @Filename, FilePath = @FilePath, ContentType = @ContentType, 
                Status = @Status, UploadedAt = @UploadedAt
            WHERE Id = @Id";
        
        command.Parameters.AddWithValue("@Id", doc.Id.ToString());
        command.Parameters.AddWithValue("@Filename", doc.Filename);
        command.Parameters.AddWithValue("@FilePath", doc.FilePath);
        command.Parameters.AddWithValue("@ContentType", doc.ContentType);
        command.Parameters.AddWithValue("@Status", (int)doc.Status);
        command.Parameters.AddWithValue("@UploadedAt", doc.UploadedAt.ToString("o"));
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = await _dbService.GetConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM documents WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id.ToString());
        await command.ExecuteNonQueryAsync();
    }
}
