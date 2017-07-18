using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Net;

using DOKuStar.Data.Xml;
using System.Xml;


namespace ScriptingUtilities
{
    [TestClass]
    public class Test_ScriptingUtilities
    {
        private string sampleDocumentDir;
        private string repBase = @"\\JSCHACHTGXY3JC2\DOKuStarDispatchData\Repositories\Standard\";

        public Test_ScriptingUtilities()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            sampleDocumentDir = Directory.GetParent(path).Parent.Parent.Parent.FullName + @"\SampleDocuments";
        }


        [TestMethod]
        public void TestAndExplore()
        {
            Guid id = Guid.NewGuid();
            string x = id.ToString();
        }

        [TestMethod]
        public void Watermark_1()
        {
            string wmPage1Bw = Path.Combine(sampleDocumentDir, "TestDocument_Imaging-P1-BW.tif");
            string wmPage1Color = Path.Combine(sampleDocumentDir, "TestDocument_Imaging-P1-Color.tif");
            string wmPag2Bw = Path.Combine(sampleDocumentDir, "TestDocument_Imaging-P2-BW.tif");
            string wmPage2Color = Path.Combine(sampleDocumentDir, "TestDocument_Imaging-P2-Color.tif");
            OCCSinglePage sp;

            sp = new OCCSinglePage(wmPage1Bw);
            sp.AddTextToImage("Hello World", 0, 0, 0, 0, 0);
            sp.Save(@"c:\temp\P1Bw.tif");
        }

        [TestMethod]
        public void Watermark_2()
        {
            string repository = repBase + "Imprint.jc3.0000";
            string dataPoolFile = Path.Combine(repository, "18803_o44613_wj12_Rec.data");
            DataPool pool = new DataPool();
            pool.Load(dataPoolFile);

            ScriptingUtilities.Imprint(pool, "<Date>_<:Field1>_<PageNumber>", true, 3, true, true, 0, 0, 0, 0);
            pool.Save(Path.Combine(repository, "xx.data"));
        }

        [TestMethod]
        public void Redact()
        {
            string repository = repBase + "Reda-err.jc16.0000";
            string dataPoolFile = Path.Combine(repository, "20801_o52805_wj8_Dat.data");
            DataPool pool = new DataPool();
            pool.Load(dataPoolFile);

            DataPool.CacheFolder = @"c:\temp";
            ScriptingUtilities.Redact(pool, "Pink");
            pool.Save(Path.Combine(repository, "xx.data"));
        }

        [TestMethod]
        public void Rendition_1()
        {
            Color col;
            col = ScriptingUtilities.rd_GetColor("Blue");
            Assert.IsTrue(col.R == 0 && col.G == 0 && col.B == 255);
            col = ScriptingUtilities.rd_GetColor("UnkownColor");
            Assert.IsTrue(col.R == 0 && col.G == 0 && col.B == 0);
            col = ScriptingUtilities.rd_GetColor("#123456");
            Assert.IsTrue(col.R == 0x12 && col.G == 0x34 && col.B == 0x56);
            col = ScriptingUtilities.rd_GetColor("255");
            Assert.IsTrue(col.R == 0 && col.G == 0 && col.B == 0xFF);
            col = ScriptingUtilities.rd_GetColor("0x7F00FF00");
            Assert.IsTrue(col.A == 127 && col.R == 0 && col.G == 255 && col.B == 0);
        }

        [TestMethod]
        [Ignore]
        public void RotatedImage()
        {
            Color col = ScriptingUtilities.rd_GetColor("Pink");
            string img = Path.Combine(sampleDocumentDir, "Rotate.tif");

            OCCSinglePage sp = new OCCSinglePage(img, 270);
            sp.FillRectangle(new SolidBrush(col), new Rectangle(50, 50, 100, 40));
            sp.FillRectangle(new SolidBrush(col), new Rectangle(1000, 1000, 500, 200));
            sp.Save(img + ".tif");
        }

        [TestMethod]
        [Ignore]
        public void CutImage()
        {
            string dataPoolFile = Path.Combine(sampleDocumentDir, "204_o423_wj13_Dat.data");
            DataPool pool = new DataPool();
            pool.Load(dataPoolFile);
            DataPool.CacheFolder = sampleDocumentDir;

            Document newDocument = new Document(pool, "Photo");
            pool.RootNode.Documents.Add(newDocument);
            Field f = pool.RootNode.Documents[0].Fields["Photo"] as Field;
            ScriptingUtilities.CutImage(pool, f, newDocument);
            pool.Save(Path.Combine(sampleDocumentDir, "CutImage.data"));
        }

        [TestMethod]
        public void RemoveBlankPages()
        {
            string repository = @"\\JSCHACHTGXY3JC2\DOKuStarDispatchData\Repositories\Standard\Profile1.jc1.0000";

            string dataPoolFile = Path.Combine(repository, "12601_o19201_wj23_Rec.data");
            DataPool pool = new DataPool();

            pool.Load(dataPoolFile);

            ScriptingUtilities.RemoveBlankPage(pool, "BlankPage");
            pool.Save(Path.Combine(repository, "noBP.data"));
        }

        [TestMethod]
        public void Test_Email2PDF()
        {
            //string content = File.ReadAllText(@"c:\temp\tt.html");
            //EmailToPDF.Convert(content, @"c:\temp\tt.pdf");

            string repository = @"\\JSCHACHTGXY3JC2\DOKuStarDispatchData\Repositories\Standard\emai2pdf.jc7.0001";
            string dataPoolFile = Path.Combine(repository, "13803_o21405_wj0_Imp.data");
            DataPool pool = new DataPool();
            pool.Load(dataPoolFile);

            ScriptingUtilities.ConvertEmailToPDF(pool, 0);
            pool.Save(Path.Combine(repository, "xx.data"));
        }

        [TestMethod]
        public void Test_JoinDocuments()
        {

            string repository = @"\\JSCHACHTGXY3JC2\DOKuStarDispatchData\Repositories\Standard\emai2pdf.jc7.0001";
            string dataPoolFile = Path.Combine(repository, "14001_o30201_wj35_Rec.data");
            DataPool pool = new DataPool();
            pool.Load(dataPoolFile);

            ScriptingUtilities.JoinDocuments(pool, 0);
            pool.Save(Path.Combine(repository, "xx.data"));
        }

        [TestMethod]
        public void Test_PDFCompression()
        {
            string folder = @"C:\01_Work\01_OCC\dev\OCC_Head\occ_b\Samples\ScriptingUtilities\";
            string input = folder + @"SampleDocuments\ColorImage.jpeg";
            string output = @"c:\temp\xx.pdf";
            //IPdfCompressor compress = new Luratech_Compressor();
            IPdfCompressor compress = new Dummy_Compressor();
            compress.Initialize(folder + @"ScriptingUtilitiesProfileExtension\PdfCompression\LuraTech\SampleLuraTechConfig.dat");
            compress.Compress(input, output);
        }

        [TestMethod]
        public void Test_ExportAttachments()
        {
            string repository = @"\\JSCHACHTGXY3JC2\DOKuStarDispatchData\Repositories\Standard\Emaients.jc9.0001\";
            string dataPoolFile = Path.Combine(repository, "17605_o39023_wj8_Rec.data");
            DataPool pool = new DataPool();
            pool.Load(dataPoolFile);

            ScriptingUtilities.ExportAttachments(pool, 0);
            pool.Save(Path.Combine(repository, "xx.data"));
        }

        [TestMethod]
        public void Test_RenderAttachments()
        {
            string repository = @"\\JSCHACHTGXY3JC2\DOKuStarDispatchData\Repositories\Standard\Rendents.jc10.0000\";
            string dataPoolFile = Path.Combine(repository, "17801_o39202_wj0_Imp.data");
            DataPool pool = new DataPool();
            pool.Load(dataPoolFile);
            DataPool.CacheFolder = @"c:\temp";
            Dummy_Renderer.SampleFileContent = Properties.Resources.SampleDocument; // in case we use it...

            string exchangeFolder = @"\\occ - 3.cloudapp.net\xChange\Default";
            ScriptingUtilities.RenderAttachments(pool, exchangeFolder, -1);
            pool.Save(Path.Combine(repository, "xx.data"));
        }

        [TestMethod]
        public void Test_Blazon()
        {
            string exchangeFolder = @"\\occ-3.cloudapp.net\xChange\Default";
            renderTest(exchangeFolder, Properties.Resources.MsWord_Test, true);
            renderTest(exchangeFolder, Properties.Resources.MsWord_NotWorking, false);
        }

        private void renderTest(string exchangeFolder, byte[] content, bool excpectSuccess)
        {
            Blazon_Renderer br = new Blazon_Renderer();

            string testFile = @"C:\temp\RenderInput.docx";
            string resultFile = @"C:\temp\RenderResult.pdf";

            File.WriteAllBytes(testFile, content);

            bool gotError = false;
            string errorMsg = null;
            try { br.Render(exchangeFolder, testFile, resultFile); }
            catch (Exception e) { gotError = true; errorMsg = e.Message; }

            Assert.IsTrue(excpectSuccess != gotError);
            if (gotError)
                Assert.IsTrue(errorMsg.Length > 1);
            else
                Assert.IsTrue(File.Exists(resultFile));

            File.Delete(testFile);
            File.Delete(resultFile);
        }

        [TestMethod]
        public void Test_RemoveUncertainRows()
        {
            var td = new[]
            {
                new { n = 0, remaining = 7, ucrd = new ScriptingUtilities.UncertainRowsDefinition() {
                }},
                new { n = 1, remaining = 6, ucrd = new ScriptingUtilities.UncertainRowsDefinition() {
                    Top = true,
                }},
                new { n = 2, remaining = 2, ucrd = new ScriptingUtilities.UncertainRowsDefinition() {
                    Top = true, Bottom = true,
                }},
                new { n = 3, remaining = 2, ucrd = new ScriptingUtilities.UncertainRowsDefinition() {
                    Top = true, Bottom = true, TopFields = new List<string>() { "Price" },
                }},
                new { n = 4, remaining = 3, ucrd = new ScriptingUtilities.UncertainRowsDefinition() {
                    Top = true, Bottom = true, TopFields = new List<string>() { "Price", "Qty" },
                }},
                new { n = 5, remaining = 7, ucrd = new ScriptingUtilities.UncertainRowsDefinition() {
                    Bottom = true, BottomFields = new List<string>() { "Total" },
                }},
                new { n = 6, remaining = 3, ucrd = null as ScriptingUtilities.UncertainRowsDefinition },
            };
            int doOnly = -1;
            for (int i = 0; i != td.Length; i++)
            {
                if (doOnly > 0 && td[i].n != doOnly) continue;

                DataPool pool = new DataPool();
                pool.Load(new MemoryStream(Properties.Resources.UncertainRows));

                string tablename = "Items";
                ScriptingUtilities.RemoveUncertainRows(pool, tablename, td[i].ucrd);
                Assert.AreEqual(td[i].remaining, (pool.RootNode.Documents[0].Fields[tablename] as Table).Rows.Count);
            }
            Assert.IsTrue(doOnly < 0);

            ScriptingUtilities.UncertainRowsDefinition ucrd = new ScriptingUtilities.UncertainRowsDefinition()
            {
                Top = true,
                //TopFields = new List<string>() { "Price", "Qty" },
                //Bottom = true,
                //BottomFields = new List<string>() { "Total" },
            };

            //DataPool pool = new DataPool();
            //pool.Load(new MemoryStream(Properties.Resources.UncertainRows));

            //ScriptingUtilities.RemoveUncertainRows(pool, tableName, ucrd);
            //pool.Save(Path.Combine(repository, "xx.data"));
        }


        [TestMethod]
        public void Test_SqlVerification()
        {
            string repository = @"\\JSCHACHTGXY3JC2\DOKuStarDispatchData\Repositories\Standard\SqlVtion.jc15.0000\";
            string dataPoolFile = Path.Combine(repository, "20201_o51405_wj5_Rec.data");
            DataPool data = new DataPool();
            data.Load(dataPoolFile);

            SqlTableSpec tableSpec = new SqlTableSpec(
                @"jschachtGXY3JC2\DOKUSTAR",
                "OCCExport", "CustomerDB",
                "sa", "#DOKuStar#");

            List<SqlFieldMapping> fieldMap = new List<SqlFieldMapping>()
            {
                new SqlFieldMapping("Firstname", "Firstname"),
                new SqlFieldMapping("Lastname", "Lastname"),
                new SqlFieldMapping("Street", "Street"),
            };

            //ScriptingUtilities.SqlVerification(data, tableSpec, fieldMap, "Confirmation");

            // Preparation
            //ScriptingUtilities.AddSqlVerificationSpec(data,
            //    new SqlVerificationSepc("Test", tableSpec, "DocumentClass", fieldMap));
            //ScriptingUtilities.AddSqlVerificationSpec(data,
            //     new SqlVerificationSepc("Test1", tableSpec, "DocumentClass", fieldMap));
            //ScriptingUtilities.SqlVerify(data, "Test");

            // Execution
            data.RootNode.Annotations.Add(new Annotation(data, "SU_SqlVerify", "Test"));
            XmlDocument xmlDoc = data.XmlDocument;
            ScriptingUtilitiesProfileExtension supe = new ScriptingUtilitiesProfileExtension();
            supe.transform(xmlDoc, null);

            data.Save(Path.Combine(repository, "xx.data"));
        }

        [TestMethod]
        public void Test_GetProfilenameFromHotspotSitemap()
        {
            GetProfilenameFromHotspotSitemap(
                @"\\JSCHACHTGXY3JC2\DOKuStarDispatchData\Config\RdaProject\JobClass14_Steps\HotSpot\JobClass14_HotSpot.sitemap"
            );
        }

        private string GetProfilenameFromHotspotSitemap(string sitemap)
        {
            string result = null;
            DirectoryInfo s1 = Directory.GetParent(Path.GetDirectoryName(sitemap));
            string rdaProjectFile = Path.Combine(s1.Parent.FullName, "RdaProjects.rpj");
            string jobname = Path.GetFileName(s1.FullName);

            XElement xe = XElement.Load(rdaProjectFile);
            try
            {
                result = xe.Descendants("ProjectDescriptor")
                    .Where(n => n.Element("HomeDirectory").Value == jobname)
                    .First()
                    .Element("Name").Value;
            }
            catch { }
            return result;
        }

        [TestMethod]
        public void Test_ShowAllFields()
        {
            string repository = @"\\JSCHACHTGXY3JC2\DOKuStarDispatchData\Repositories\Standard\Showelds.jc19.0000";
            string dataPoolFile = Path.Combine(repository, "29201_o69004_wj5_Rec.data");
            DataPool data = new DataPool();
            data.Load(dataPoolFile);

            ScriptingUtilities.ShowAllFields(data, "ShowAll");
            data.Save(Path.Combine(repository, "xx.data"));
        }

        [TestMethod]
        public void ExifExtraction()
        {
            string image = @"C:\01_Work\01_OCC\dev\OCC_Head\occ_b\Samples\ScriptingUtilities\SampleDocuments\ExifData.JPG";
            //string image = @"C:\01_Work\01_OCC\dev\OCC_Head\occ_b\Samples\ScriptingUtilities\SampleDocuments\BlankPage.docx";

            IImageMetadataExtraction extractor = ImageMetadataExtraction_Manager.GetExtractor();
            var x1 = extractor.Extract(image);

            string repository = @"\\JSCHACHTGXY3JC2\DOKuStarDispatchData\Repositories\Standard\ImagData.jc28.0000";
            string dataPoolFile = Path.Combine(repository, "35401_o86406_wj5_Rec.data");
            DataPool data = new DataPool();
            data.Load(dataPoolFile);

            Document doc = data.RootNode.Documents[0];
            Source source = doc.Sources[0];
            ImageSourceInstance isi = (ImageSourceInstance)source.Instance.ParentInstance;
            string inputFile =  isi.Url;

            x1 = extractor.Extract(inputFile);

            string x2 = ScriptingUtilities.GetImageMetaData(doc, "Exif SubIFD", "Exposure Time");

        }
    }
}
