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
        using var transaction = connection.BeginTransaction();
        try
        {
            // Delete vectors first
            using var deleteVectorsCmd = connection.CreateCommand();
            deleteVectorsCmd.Transaction = transaction;
            deleteVectorsCmd.CommandText = @"
                DELETE FROM chunk_embeddings 
                WHERE id IN (SELECT RowId FROM document_chunks WHERE DocumentId = @Id)";
            deleteVectorsCmd.Parameters.AddWithValue("@Id", id.ToString());
            await deleteVectorsCmd.ExecuteNonQueryAsync();

            // Delete chunks
            using var deleteChunksCmd = connection.CreateCommand();
            deleteChunksCmd.Transaction = transaction;
            deleteChunksCmd.CommandText = "DELETE FROM document_chunks WHERE DocumentId = @Id";
            deleteChunksCmd.Parameters.AddWithValue("@Id", id.ToString());
            await deleteChunksCmd.ExecuteNonQueryAsync();

            // Delete document
            using var deleteDocCmd = connection.CreateCommand();
            deleteDocCmd.Transaction = transaction;
            deleteDocCmd.CommandText = "DELETE FROM documents WHERE Id = @Id";
            deleteDocCmd.Parameters.AddWithValue("@Id", id.ToString());
            await deleteDocCmd.ExecuteNonQueryAsync();

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task SaveChunksAsync(Guid documentId, List<DocumentChunk> chunks, List<float[]> embeddings)
    {
        if (chunks.Count != embeddings.Count)
            throw new ArgumentException("Chunks and embeddings count must match.");

        using var connection = await _dbService.GetConnectionAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var embedding = embeddings[i];

                using var insertChunkCmd = connection.CreateCommand();
                insertChunkCmd.Transaction = transaction;
                insertChunkCmd.CommandText = @"
                    INSERT INTO document_chunks (Id, DocumentId, Text, ChunkIndex)
                    VALUES (@Id, @DocumentId, @Text, @ChunkIndex)
                    RETURNING RowId";
                
                insertChunkCmd.Parameters.AddWithValue("@Id", chunk.Id.ToString());
                insertChunkCmd.Parameters.AddWithValue("@DocumentId", chunk.DocumentId.ToString());
                insertChunkCmd.Parameters.AddWithValue("@Text", chunk.Text);
                insertChunkCmd.Parameters.AddWithValue("@ChunkIndex", chunk.ChunkIndex);
                
                var rowIdObj = await insertChunkCmd.ExecuteScalarAsync();
                if (rowIdObj == null) throw new Exception("Failed to insert chunk");
                var rowId = Convert.ToInt64(rowIdObj);

                using var insertEmbeddingCmd = connection.CreateCommand();
                insertEmbeddingCmd.Transaction = transaction;
                insertEmbeddingCmd.CommandText = @"
                    INSERT INTO chunk_embeddings (id, embedding)
                    VALUES (@Id, @Embedding)";
                
                insertEmbeddingCmd.Parameters.AddWithValue("@Id", rowId);
                
                var embeddingBytes = new byte[embedding.Length * sizeof(float)];
                Buffer.BlockCopy(embedding, 0, embeddingBytes, 0, embeddingBytes.Length);
                
                insertEmbeddingCmd.Parameters.AddWithValue("@Embedding", embeddingBytes);
                
                await insertEmbeddingCmd.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
