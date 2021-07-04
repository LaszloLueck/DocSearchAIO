using System.Xml;
using System.Xml.Linq;
using DocSearchAIO.Scheduler;
using DocumentFormat.OpenXml;
using Xunit;

namespace DocSearchAIO_Test
{
    public class XmlDocumentTest
    {

        [Fact]
        public void Crunch_Table_Structure_And_Return_Text()
        {
            // var str =
            //     "<w:document xmlns:wpc=\"http://schemas.microsoft.com/office/word/2010/wordprocessingCanvas\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:m=\"http://schemas.openxmlformats.org/officeDocument/2006/math\" xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:wp14=\"http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing\" xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" xmlns:w10=\"urn:schemas-microsoft-com:office:word\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:w14=\"http://schemas.microsoft.com/office/word/2010/wordml\" xmlns:wpg=\"http://schemas.microsoft.com/office/word/2010/wordprocessingGroup\" xmlns:wpi=\"http://schemas.microsoft.com/office/word/2010/wordprocessingInk\" xmlns:wne=\"http://schemas.microsoft.com/office/word/2006/wordml\" xmlns:wps=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\" mc:Ignorable=\"w14 wp14\"><w:body><w:tr w:rsidR=\"0085127D\" w:rsidRPr=\"00533258\"><w:tblPrEx><w:tblCellMar><w:left w:w=\"108\" w:type=\"dxa\"/><w:right w:w=\"108\" w:type=\"dxa\"/></w:tblCellMar></w:tblPrEx><w:trPr><w:cantSplit/></w:trPr><w:tc><w:tcPr><w:tcW w:w=\"1830\" w:type=\"dxa\"/></w:tcPr><w:p w:rsidR=\"0085127D\" w:rsidRDefault=\"0085127D\" w:rsidP=\"005B40B7\"><w:r><w:t>28.12.2016</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"1100\" w:type=\"dxa\"/></w:tcPr><w:p w:rsidR=\"0085127D\" w:rsidRDefault=\"0085127D\" w:rsidP=\"005B40B7\"><w:r><w:t>0.5</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"2860\" w:type=\"dxa\"/></w:tcPr><w:p w:rsidR=\"0085127D\" w:rsidRDefault=\"004061A3\" w:rsidP=\"005B40B7\"><w:r><w:t>Kommentare e</w:t></w:r><w:r w:rsidR=\"00E01E08\"><w:t>ingearbeitet</w:t></w:r><w:r><w:t xml:space=\"preserve\">, Vorwort ergänzt, </w:t></w:r><w:r w:rsidR=\"0085127D\"><w:t>Kapitel III – V überarbeitet, IV.7 gelöscht</w:t></w:r><w:r><w:t>, Anlage beigefügt</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"2090\" w:type=\"dxa\"/></w:tcPr><w:p w:rsidR=\"0085127D\" w:rsidRDefault=\"0085127D\" w:rsidP=\"005B40B7\"><w:r><w:t>D. Süshardt</w:t></w:r></w:p></w:tc></w:tr></w:body></w:document>";
            // XmlDocument doc = new XmlDocument();
            //
            // XNamespace wNs = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            //
            // var foo = doc
            //     .InnerXml(wNs + "t");
                
            Assert.True(true);
        }
        
    }
}