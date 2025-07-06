using System.IO;
using System.Text;
using System.Threading.Tasks;
using DevOpsAssistant.Utils;
using Pkg = DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using P = DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml;
using D = DocumentFormat.OpenXml.Drawing;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Core;
using Xunit;

namespace DevOpsAssistant.Tests.Utils;

public class DocumentHelpersTests
{
    [Fact]
    public async Task ExtractText_Returns_Content_For_Markdown()
    {
        var text = "# Heading\nContent";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        var result = await DocumentHelpers.ExtractTextAsync(stream, "test.md");

        Assert.Equal(text, result);
    }

    [Fact]
    public async Task ExtractText_Returns_Content_For_Docx()
    {
        using var ms = new MemoryStream();
        using (var doc = Pkg.WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new W.Document(new W.Body(new W.Paragraph(new W.Run(new W.Text("Docx Text")))));
            mainPart.Document.Save();
        }
        ms.Position = 0;

        var result = await DocumentHelpers.ExtractTextAsync(ms, "file.docx");

        Assert.Contains("Docx Text", result);
    }

    [Fact]
    public async Task ExtractText_Returns_Content_For_Pptx()
    {
        using var ms = new MemoryStream();
        using (var doc = Pkg.PresentationDocument.Create(ms, PresentationDocumentType.Presentation))
        {
            var presPart = doc.AddPresentationPart();
            presPart.Presentation = new P.Presentation();
            var slidePart = presPart.AddNewPart<Pkg.SlidePart>("rId1");
            slidePart.Slide = new P.Slide(new P.CommonSlideData(new P.ShapeTree()));
            presPart.Presentation.SlideIdList = new P.SlideIdList(new P.SlideId { Id = 256U, RelationshipId = "rId1" });
            var tree = slidePart.Slide.CommonSlideData!.ShapeTree!;
            tree.Append(new P.Shape(
                new P.NonVisualShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 2U, Name = "Title" },
                    new P.NonVisualShapeDrawingProperties(new D.ShapeLocks()),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.ShapeProperties(),
                new P.TextBody(new D.BodyProperties(), new D.ListStyle(), new D.Paragraph(new D.Run(new D.Text("Slide Text"))))
            ));
            slidePart.Slide.Save();
            presPart.Presentation.Save();
        }
        ms.Position = 0;

        var result = await DocumentHelpers.ExtractTextAsync(ms, "file.pptx");

        Assert.Contains("Slide Text", result);
    }

    [Fact]
    public async Task ExtractText_Returns_Content_For_Pdf()
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("Pdf Text", 12, new PdfPoint(25, 750), font);
        var bytes = builder.Build();
        using var ms = new MemoryStream(bytes);

        var result = await DocumentHelpers.ExtractTextAsync(ms, "file.pdf");

        Assert.Contains("Pdf Text", result);
    }

    [Fact]
    public async Task ExtractText_Returns_Empty_For_Unknown_Extension()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("ignored"));

        var result = await DocumentHelpers.ExtractTextAsync(ms, "file.txt");

        Assert.Equal(string.Empty, result);
    }
}
