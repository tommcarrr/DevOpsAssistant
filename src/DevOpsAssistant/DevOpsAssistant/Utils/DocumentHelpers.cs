using DocumentFormat.OpenXml.Packaging;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace DevOpsAssistant.Utils;

public static class DocumentHelpers
{
    public static async Task<string> ExtractTextAsync(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => ExtractPdf(stream),
            ".docx" => ExtractDocx(stream),
            ".pptx" => ExtractPptx(stream),
            ".md" => await ExtractMarkdownAsync(stream),
            _ => string.Empty
        };
    }

    private static string ExtractPdf(Stream stream)
    {
        using var doc = PdfDocument.Open(stream);
        var sb = new System.Text.StringBuilder();
        foreach (var page in doc.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    private static string ExtractDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        return string.Join("\n", doc.MainDocumentPart!.Document
            .Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
            .Select(t => t.Text));
    }

    private static string ExtractPptx(Stream stream)
    {
        using var doc = PresentationDocument.Open(stream, false);
        return string.Join("\n", doc.PresentationPart!.SlideParts.SelectMany(
            s => s.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
            .Select(t => t.Text));
    }

    private static async Task<string> ExtractMarkdownAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
