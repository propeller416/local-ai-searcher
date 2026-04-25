using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace Infrastructure.Services;

public class DocumentProcessorService
{
    public async Task<string> ExtractTextAsync(string filePath, string extension)
    {
        return await Task.Run(() =>
        {
            var text = string.Empty;
            var ext = extension.ToLowerInvariant();

            if (ext == ".pdf")
            {
                using var document = PdfDocument.Open(filePath);
                var sb = new StringBuilder();
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
                text = sb.ToString();
            }
            else if (ext == ".docx")
            {
                using var wordDoc = WordprocessingDocument.Open(filePath, false);
                if (wordDoc.MainDocumentPart?.Document?.Body != null)
                {
                    text = wordDoc.MainDocumentPart.Document.Body.InnerText;
                }
            }
            else if (ext == ".txt")
            {
                text = File.ReadAllText(filePath);
            }

            // Basic cleanup
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text;
        });
    }

    public List<string> ChunkText(string text, int chunkSize = 1000, int overlap = 200)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        int start = 0;
        while (start < text.Length)
        {
            int length = Math.Min(chunkSize, text.Length - start);
            chunks.Add(text.Substring(start, length));
            
            if (start + length >= text.Length)
                break;
                
            start += chunkSize - overlap;
        }

        return chunks;
    }
}