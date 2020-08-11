using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Pdf.Interfaces;
using ISFG.Pdf.Models.Clause;
using ISFG.Pdf.Models.ShreddingPlan;
using ISFG.Pdf.Models.TransactionHistory;
using iText.IO.Font;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Pdfa;

namespace ISFG.Pdf.Services
{
    public class PdfService : IPdfService
    {
        #region Fields

        private readonly string _imageColorMatchingFilename = "sRGB_CS_profile.icm";
        private readonly string _trueTypeFontFilename = "Inconsolata.ttf";

        #endregion

        #region Implementation of IPdfService

        public Task<byte[]> AddClause(MemoryStream input, ClauseModel clauseModel)
        {
            var output = new MemoryStream();
            var pdfFont = PdfFontFactory.CreateFont(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _trueTypeFontFilename), PdfEncodings.IDENTITY_H, true);
            
            try
            {
                PdfDocument pdfDoc = new PdfDocument(new PdfReader(input), new PdfWriter(output));
                Document document = new Document(pdfDoc);

                pdfDoc.AddNewPage(pdfDoc.GetNumberOfPages() + 1);
                document.Add(new AreaBreak(AreaBreakType.LAST_PAGE));

                foreach (string paragraph in clauseModel.Paragraphs)
                    document.Add(CreateText(paragraph).SetFont(pdfFont));

                if (clauseModel.Sign != null)
                {
                    document.Add(CreateText(clauseModel.Sign).SetFont(pdfFont));
                    if (clauseModel.SignTimestamp != null)
                        document.Add(CreateText(clauseModel.SignTimestamp).SetFont(pdfFont));
                }
                
                if (clauseModel.Seal != null)
                {
                    document.Add(CreateText(clauseModel.Seal).SetFont(pdfFont));
                    if (clauseModel.SealTimestamp != null)
                        document.Add(CreateText(clauseModel.SealTimestamp).SetFont(pdfFont));
                }
                
                if (clauseModel.SealTimestamp == null && clauseModel.SignTimestamp == null && clauseModel.Timestamp != null)
                    document.Add(CreateText(clauseModel.Timestamp).SetFont(pdfFont));

                document.Add(CreateNewLine().SetFont(pdfFont));
                document.Add(CreateText(clauseModel.OriginalFileFormat + " " + clauseModel.OriginalFileFormatValue).SetFont(pdfFont));
                document.Add(CreateText(clauseModel.FilePrint + " " + clauseModel.FilePrintValue).SetFont(pdfFont));
                document.Add(CreateText(clauseModel.UsedAlgorithm + " " + clauseModel.UsedAlgorithmValue).SetFont(pdfFont));
                document.Add(CreateText(clauseModel.Organizer + " " + clauseModel.OrganizerValue).SetFont(pdfFont));
                document.Add(CreateText(clauseModel.NameLastName + " " + clauseModel.NameLastNameValue).SetFont(pdfFont));
                document.Add(CreateText(clauseModel.DateOfIssue + " " + clauseModel.DateOfIssueValue).SetFont(pdfFont));

                document.Close();
            }
            finally
            {
                input.Dispose();
                output.Dispose();
            }

            return Task.FromResult(output.ToArray());
        }

        public Task<byte[]> ConvertToPdfA2B(MemoryStream input)
        {
            var output = new MemoryStream();
            var iccStream = new FileStream(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _imageColorMatchingFilename), FileMode.Open, FileAccess.Read);

            try
            {
                var intent = new PdfOutputIntent("Custom", "", "http://www.color.org", "sRGB IEC61966-2.1", iccStream);

                var currentPdf = new PdfADocument(new PdfWriter(output), PdfAConformanceLevel.PDF_A_2B, intent);
                var inputPdf = new PdfDocument(new PdfReader(input));
            
                currentPdf.SetTagged();
                currentPdf.GetCatalog().SetLang(new PdfString("en-US"));
                currentPdf.GetCatalog().SetViewerPreferences(new PdfViewerPreferences().SetDisplayDocTitle(true));
            
                for (int i = 1; i <= inputPdf.GetNumberOfPages(); i++)
                    currentPdf.AddPage(inputPdf.GetPage(i).CopyTo(currentPdf));

                //inputPdf.CopyPagesTo(1, inputPdf.GetNumberOfPages(), currentPdf);

                currentPdf.Close();
                
                return Task.FromResult(output.ToArray());
            }
            finally
            {
                iccStream.Dispose();
                input.Dispose();
                output.Dispose();
            }
        }

        public Task<byte[]> GenerateShreddingPlan(ShreddingPlan shreddingPlan)
        {
            var output = new MemoryStream();
            var iccStream = new FileStream(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _imageColorMatchingFilename), FileMode.Open, FileAccess.Read);
            var pdfFont = PdfFontFactory.CreateFont(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _trueTypeFontFilename), PdfEncodings.IDENTITY_H, true);
            
            try
            {
                var intent = new PdfOutputIntent("Custom", "", "http://www.color.org", "sRGB IEC61966-2.1", iccStream);      
                var pdf = new PdfADocument(new PdfWriter(output), PdfAConformanceLevel.PDF_A_2B, intent);
                Document document = new Document(pdf);
                
                document.Add(CreateTextAlignment(shreddingPlan.Title).SetFont(pdfFont).SetBold().SetTextAlignment(TextAlignment.CENTER));
                document.Add(CreateNewLine().SetFont(pdfFont));

                if (shreddingPlan.Columns.Any())
                {
                    var table = new Table(3, false);
                    table.AddCell(CreateRootCell(shreddingPlan.Rows.FileMark).SetFont(pdfFont));
                    table.AddCell(CreateRootCell(shreddingPlan.Rows.FileMarkText).SetFont(pdfFont));
                    table.AddCell(CreateRootCell(shreddingPlan.Rows.ShreddingMode).SetFont(pdfFont));

                    foreach (var column in shreddingPlan.Columns)
                        if (column.IsParent)
                        {
                            table.AddCell(CreateCell(column.FileMark).SetFont(pdfFont).SetBold());
                            table.AddCell(CreateCell(column.FileMarkText, TextAlignment.LEFT).SetFont(pdfFont).SetBold());
                            table.AddCell(CreateCell("").SetFont(pdfFont));
                        }
                        else
                        {
                            table.AddCell(CreateCell(column.FileMark).SetFont(pdfFont));
                            table.AddCell(CreateCell(column.FileMarkText, TextAlignment.LEFT).SetFont(pdfFont));
                            table.AddCell(CreateCell($"{column.RetentionMark}/{column.Period}").SetFont(pdfFont));
                        }
                    
                    document.Add(table);
                }
                
                var numberOfPages = pdf.GetNumberOfPages();

                for (int pageNum = 1; pageNum <= numberOfPages; pageNum++) 
                {
                    Rectangle pageSize = pdf.GetPage(pageNum).GetPageSize();
                    
                    document.ShowTextAligned(CreateText(pageNum + " / " + numberOfPages, 7).SetFont(pdfFont), pageSize.GetWidth() / 2, pageSize.GetBottom() + 15, pageNum, TextAlignment.CENTER, VerticalAlignment.BOTTOM, 0);
                }
                
                document.Close();

                return Task.FromResult(output.ToArray());
            }
            finally
            {
                iccStream.Dispose();
                output.Dispose();
            }            
        }

        public Task<byte[]> GenerateTransactionHistory(TransactionHistoryModel transactionHistoryModel)
        {
            var output = new MemoryStream();
            var iccStream = new FileStream(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _imageColorMatchingFilename), FileMode.Open, FileAccess.Read);
            var pdfFont = PdfFontFactory.CreateFont(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _trueTypeFontFilename), PdfEncodings.IDENTITY_H, true);
            
            try
            {
                var intent = new PdfOutputIntent("Custom", "", "http://www.color.org", "sRGB IEC61966-2.1", iccStream);      
                var pdf = new PdfADocument(new PdfWriter(output), PdfAConformanceLevel.PDF_A_2B, intent);
                Document document = new Document(pdf);
                
                if (transactionHistoryModel.Columns.Any())
                {
                    var table = new Table(new float[]{ 5, 3, 5, 15, 3, 3 }).SetFixedLayout().UseAllAvailableWidth();
                    table.SetWidth(pdf.GetDefaultPageSize().GetWidth() - 80);
                    table.AddCell(CreateRootCell(transactionHistoryModel.Rows.Pid).SetFont(pdfFont));
                    table.AddCell(CreateRootCell(transactionHistoryModel.Rows.TypeOfObject).SetFont(pdfFont));
                    table.AddCell(CreateRootCell(transactionHistoryModel.Rows.TypeOfChanges).SetFont(pdfFont));
                    table.AddCell(CreateRootCell(transactionHistoryModel.Rows.Descriptions).SetFont(pdfFont));
                    table.AddCell(CreateRootCell(transactionHistoryModel.Rows.Author).SetFont(pdfFont));
                    table.AddCell(CreateRootCell(transactionHistoryModel.Rows.Date).SetFont(pdfFont));
                
                    foreach (var column in transactionHistoryModel.Columns)
                    {
                        table.AddCell(CreateCell(column.Pid).SetFont(pdfFont));
                        table.AddCell(CreateCell(column.TypeOfObject).SetFont(pdfFont));
                        table.AddCell(CreateCell(column.TypeOfChanges).SetFont(pdfFont));
                        table.AddCell(CreateCell(column.Descriptions, TextAlignment.LEFT).SetFont(pdfFont));
                        table.AddCell(CreateCell(column.Author, TextAlignment.LEFT).SetFont(pdfFont));
                        table.AddCell(CreateCell(column.Date, TextAlignment.LEFT).SetFont(pdfFont));    
                    }
                    
                    document.Add(table);
                    document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }

                document.Add(CreateNewLine().SetFont(pdfFont));
                document.Add(CreateLineSeparator().SetFont(pdfFont));
                document.Add(CreateTextAlignment(transactionHistoryModel.Name).SetFont(pdfFont));
                document.Add(CreateTextAlignment(transactionHistoryModel.Originator).SetFont(pdfFont));
                document.Add(CreateTextAlignment(transactionHistoryModel.Address).SetFont(pdfFont));
                document.Add(CreateTextAlignment(transactionHistoryModel.SerialNumber).SetFont(pdfFont));
                document.Add(CreateTextAlignment($"{transactionHistoryModel.NumberOfPages}{pdf.GetNumberOfPages()}").SetFont(pdfFont));

                var numberOfPages = pdf.GetNumberOfPages();
                
                pdf.MovePage(numberOfPages, 1);
                
                for (int pageNum = 1; pageNum <= numberOfPages; pageNum++) 
                {
                    Rectangle pageSize = pdf.GetPage(pageNum).GetPageSize();
                    
                    if (pageNum != 1)
                        document.ShowTextAligned(CreateTextAlignment(transactionHistoryModel.Header, 7).SetFont(pdfFont), pageSize.GetLeft() + 35, pageSize.GetTop() - 13, pageNum, TextAlignment.LEFT, VerticalAlignment.TOP, 0);
                    
                    document.ShowTextAligned(CreateText(pageNum + " / " + numberOfPages, 7).SetFont(pdfFont), pageSize.GetWidth() / 2, pageSize.GetBottom() + 15, pageNum, TextAlignment.CENTER, VerticalAlignment.BOTTOM, 0);
                }
                
                document.Close();

                return Task.FromResult(output.ToArray());
            }
            finally
            {
                iccStream.Dispose();
                output.Dispose();
            }
        }

        public Task<bool> IsPdfA2B(MemoryStream input)
        {
            PdfDocument inputPdf = new PdfDocument(new PdfReader(input));
            
            try
            {
                PdfAConformanceLevel conformanceLevel = inputPdf.GetReader().GetPdfAConformanceLevel();
                var conformance = conformanceLevel.GetConformance();
                var part = conformanceLevel.GetPart();

                return Task.FromResult(conformance.Equals("B") && part.Equals("2"));
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
            finally
            {
                inputPdf.Close();
                input.Dispose();
            }
        }

        #endregion

        #region Private Methods

        private Cell CreateCell(string text, TextAlignment textAlignment = TextAlignment.CENTER) => new Cell(1, 1)
            .SetTextAlignment(textAlignment)
            .SetFontSize(8)
            .Add(new Paragraph(text));

        private LineSeparator CreateLineSeparator() => 
            new LineSeparator(new SolidLine());

        private Paragraph CreateNewLine() => 
            new Paragraph(new Text("\n"));

        private Cell CreateRootCell(string text) => new Cell(1, 1)
            .SetBackgroundColor(ColorConstants.GRAY)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontSize(8)
            .Add(new Paragraph(text));

        private Paragraph CreateText(string text, int fontSize = 9) => new Paragraph(text)
            .SetFontSize(fontSize);

        private Paragraph CreateTextAlignment(string text, int fontSize = 9, TextAlignment textAlignment = TextAlignment.LEFT) => new Paragraph(text)
            .SetFontSize(fontSize)
            .SetTextAlignment(textAlignment);

        #endregion
    }
}
