﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using DOKuStar.Data.Xml;
using DOKuStar.Runtime.ScriptingHost;
using DOKuStar.Diagnostics.Tracing;
using DOKuStar.Extraction.Result;
using System.Xml;

namespace ScriptingUtilities
{
    public partial class ScriptingUtilities
    {
        #region Construction
        private static readonly ITrace trace = TraceManager.GetTracer(typeof(ScriptingUtilities));

        public ScriptingUtilities()
        {
        }
        #endregion

        #region Imprint
        public static string Imprint(
            DataPool pool, string spec, bool allPages, int nofDigits, bool refImage, bool ocrImage,
            int orientation, int position, int offsetX, int offsetY)
        {
            NameSpecParser nsp = new NameSpecParser()
            {
                BatchId = "BatchId", // Replace so it as "no operation"
                DocumentNumber = "DocumentNumber", // Replace so it as "no operation"
                ValueList = new List<KeyValuePair<string, string>>(),
            };
            List<NameSpecParser.SubstituteItem> lsi = nsp.Parse(spec);
            string result = string.Empty;

            foreach (Document doc in pool.RootNode.Documents)
            {
                // Initialize field values for ValueList spec parser
                foreach (Field f in doc.Fields) nsp.ValueList.Add(new KeyValuePair<string, string>(f.Name, f.Value));

                int pagenumber = 0;
                foreach (ImageSource ims in doc.Sources)
                {
                    nsp.PageNumber = pagenumber++.ToString().PadLeft(nofDigits, '0');
                    nsp.SetValues(lsi);
                    string imprint = nsp.ComposeResultString(lsi);
                    if (pagenumber == 1) result = imprint;


                    if (refImage)
                    {
                        SourceInstance si = ims.SelectInstance("ref");
                        int refRotation = (si as ImageVariantInstance).Orientation;
                        im_doImprint(si, refRotation, imprint, orientation, position, offsetX, offsetX);
                    }
                    if (ocrImage)
                    {
                        SourceInstance si = ims.SelectInstance("ocr");
                        im_doImprint(si, 0, imprint, orientation, position, offsetX, offsetX);
                    }
                }
            }
            return result;
        }

        private static void im_doImprint(
            SourceInstance si, int imageOrientation, string text,
            int orientation, int position, int offsetX, int offsetY)
        {
            string cacheFolder = DataPool.CacheFolder;

            string cacheFile = Path.Combine(cacheFolder, "su_" + Path.GetFileName(si.Url));
            File.Copy(si.Url, cacheFile);
            si.Url = cacheFile;
            si.IsArchived = false;

            OCCSinglePage sp = new OCCSinglePage(si.Url, imageOrientation);
            sp.AddTextToImage(text, 0, orientation, position, offsetX, offsetX);
            sp.Save(si.Url);
        }
        #endregion

        #region Redaction
        /// The redaction function wipes out zones on all pages that happen to host
        /// a field that needs redaction. It is not untypical that several fields reside
        /// on the same page. Therefore all affected pages are kept in memory till
        /// redaction finishes. In order to tilt memory only affected pages are loaded
        /// and b/w and color page are handled independently in two runs.

        public static void Redact(DataPool pool, string color, List<string> fieldnames = null)
        {
            foreach (Document doc in pool.RootNode.Documents)
            {
                // Independent run for each document and each page type
                rd_redactFields(doc, "ocr", "Black", fieldnames);
                rd_redactFields(doc, "ref", color, fieldnames);
            }
        }

        // Caching structure to keep pages in memory
        // Index to dictionary is the source id of the source attached to the field
        private struct Page
        {
            public string Filename { get; set; }
            public OCCSinglePage Bitmap { get; set; }
        }
        private static Dictionary<string, Page> pageMap = new Dictionary<string, Page>();

        private static void rd_redactFields(Document doc, string variant, string color, List<string> fieldnames = null)
        {
            pageMap.Clear(); // will be filled as side effect of redaction

            // Redact all fields
            foreach (Field f in doc.Fields)
            {
                if (f is Table) continue;
                if (fieldnames != null && !isInFieldList(f.Name, fieldnames)) continue;

                // Regular field
                rd_redactField(doc, variant, f, color);
                // List fields: get color from master field
                foreach (Field sf in f.Fields) rd_redactField(doc, variant, sf, color);
            }

            // store all pages
            foreach (Page page in pageMap.Values)
            {
                page.Bitmap.RemoveInputText();
                page.Bitmap.Save(page.Filename);
            }
        }

        private static bool isInFieldList(string fieldname, List<string> fieldlist)
        {
            foreach (string f in fieldlist)
                if (new Regex(f).Match("^" + fieldname + "$").Success) return true;

            return false;
        }

        private static void rd_redactField(Document doc, string variant, Field f, string color)
        {
            if (f.State == DataState.Empty) return;
            if (f.Zone.Height == 0 && f.Zone.Width == 0) return;

            // We need to locate the sources at the document, 
            // only they contain the variants as well as the rotation
            foreach (ImageSource ims in doc.Sources)
            {
                if (ims.Url != f.Sources[0].Url) continue;  // find page
                if (pageMap.ContainsKey(f.Sources[0].Id)) break; // page already loaded

                pageMap[f.Sources[0].Id] = rd_createPage(ims.SelectInstance(variant));
            }

            // Redact the zone
            OCCSinglePage sp = pageMap[f.Sources[0].Id].Bitmap;
            sp.FillRectangle(new SolidBrush(rd_GetColor(color)), f.Zone);
        }

        // We must not modify the files in the repository. 
        // Therefore we create a copy of the file in the cache folder. 
        private static Page rd_createPage(SourceInstance si)
        {
            string cacheFolder = DataPool.CacheFolder;

            string cacheFile = Path.Combine(cacheFolder, "su_" + Path.GetFileName(si.Url));
            File.Copy(si.Url, cacheFile);
            si.Url = cacheFile;
            si.IsArchived = false;

            return new Page()
            {
                Filename = cacheFile,
                Bitmap = new OCCSinglePage(cacheFile, (si as ImageVariantInstance).Orientation)
            };
        }

        public static Color rd_GetColor(string val)
        {
            if (Enum.IsDefined(typeof(KnownColor), val)) return Color.FromName(val);
            Color col;
            try { col = ColorTranslator.FromHtml(val); }
            catch { return Color.FromName("Black"); }
            return col;
        }
        #endregion

        #region Get snippet
        public static void CutImage(DataPool pool, Field sourceField, Document targetDocument)
        {
            if (sourceField.State == DataState.Empty) return;
            if (sourceField.Zone.Height == 0 && sourceField.Zone.Width == 0) return;
            OCCSinglePage pageRef = null;
            OCCSinglePage pageOcr = null;

            foreach (ImageSource ims in sourceField.ParentDocument.Sources)
            {
                if (ims.Url != sourceField.Sources[0].Url) continue;
                pageRef = new OCCSinglePage(ims.SelectInstance("ref").Url,
                    (ims.SelectInstance("ref") as ImageVariantInstance).Orientation);
                pageOcr = new OCCSinglePage(ims.SelectInstance("ocr").Url,
                    (ims.SelectInstance("ocr") as ImageVariantInstance).Orientation);
            }

            byte[] tmpImageRef = pageRef.GetSnippet(sourceField.Zone);
            byte[] tmpImageOcr = pageOcr.GetSnippet(sourceField.Zone);
            string name = sourceField.Name;
            string extension = "tif";
            string cacheFolder = DataPool.CacheFolder;

            // Save image twice
            // 1) als original
            string destFileName = Path.Combine(cacheFolder, String.Format("{0}.{1}", name, extension));
            File.WriteAllBytes(destFileName, tmpImageRef);
            string destFileNameRef = Path.Combine(cacheFolder, String.Format("{0}_REF.{1}", name, extension));
            File.WriteAllBytes(destFileNameRef, tmpImageRef);
            // 2) als default ("ocr") to bring it into the PDF
            string destFileNameOcr = Path.Combine(cacheFolder, String.Format("{0}_OCR.{1}", name, extension));
            File.WriteAllBytes(destFileNameOcr, tmpImageOcr);

            ImageSourceInstance instance = new ImageSourceInstance(targetDocument.OwnerDataPool, destFileName);
            targetDocument.OwnerDataPool.RootNode.SourceInstances.Add(instance);
            PageInstance pageInstance = instance.CreatePageInstance(1);
            ImageVariantInstance variantRef = new ImageVariantInstance(targetDocument.OwnerDataPool, destFileNameRef);
            variantRef.VariantName = "ref";
            pageInstance.Variants.Add(variantRef);
            ImageVariantInstance varianOcr = new ImageVariantInstance(targetDocument.OwnerDataPool, destFileNameOcr);
            varianOcr.VariantName = "ocr";
            pageInstance.Variants.Add(varianOcr);
            targetDocument.Sources.Add(pageInstance.GetSource());
        }
        #endregion

        #region Remove blank page
        public static void RemoveBlankPage(DataPool pool, string fieldname, string valueForEmpty = "empty")
        {
            foreach (Document doc in pool.RootNode.Documents)
            {
                IField f = doc.Fields[fieldname];
                if (f == null) continue;

                List<IField> fl = new List<IField>() { f };
                foreach (IField altField in f.Alternatives) fl.Add(altField);

                foreach (IField ff in fl)
                    if (ff.Value == valueForEmpty)
                    {
                        Source toBeRemoved = null;
                        foreach (Source s in doc.Sources)
                            if (s.Guid == ff.Sources[0].Guid) toBeRemoved = s;
                        doc.Sources.Remove(toBeRemoved);
                        ff.Sources.RemoveAt(0);
                    }
            }
        }
        #endregion

        #region Add document / field annotations
        public static void AddDocumentAnnotation(DataPool pool, string annotationName, string value = null)
        {
            foreach (Document doc in pool.RootNode.Documents)
            {
                Annotation a = new Annotation(pool, annotationName);
                if (value != null) a.Value = value;
                doc.Annotations.Add(a);
            }
        }

        public static void AddFieldAnnotation(Document doc, string annotationName, string value = null)
        {
            foreach (Field f in doc.Fields)
            {
                Annotation a = new Annotation(doc.OwnerDataPool, annotationName);
                if (value != null) a.Value = value;
                doc.Annotations.Add(a);
            }
        }
        #endregion

        #region Email to PDF

        private static IEmailToPdf emailToPdf = null;

        public static void ConvertEmailToPDF(DataPool pool, int position = -1)
        {
            // Get email converter and return if not usable
            if (emailToPdf == null) emailToPdf = EmailToPDF_Manager.GetEmailToPDF();
            if (!emailToPdf.CanConvert) return;

            // Ignore the batch if not derived from an email
            IField bodyText = pool.RootNode.Fields["BodyText"];
            if (bodyText == null) return;

            // Convert the HTML Body text of the email into an image source via intermediate temp file

            string tmpFile = Path.GetTempFileName().Replace(".tmp", ".pdf");
            emailToPdf.Convert(bodyText.Value, tmpFile);
            ImageSource source = new ImageSource(pool, tmpFile);
            source.MakeEmbedded();
            File.Delete(tmpFile);

            // Create document and add image source
            Document doc = new Document(pool, "Unknown");
            doc.Sources.Add(source);

            // Insert new document into data pool
            if (position == -1)
                pool.RootNode.Documents.Add(doc);
            else
            {
                //if (position > doc.Sources.Count) position = doc.Sources.Count;
                pool.RootNode.Documents.Insert(Math.Min(position, pool.RootNode.Documents.Count), doc);
            }
        }
        #endregion

        #region Join documents
        public static void JoinDocuments(DataPool pool, int parent, params int[] childs)
        {
            if (parent >= pool.RootNode.Documents.Count) return;

            // No child means all childs
            childs = Enumerable.Range(0, pool.RootNode.Documents.Count).Where(n => n != parent).ToArray();

            // Copy pages from child to parent
            foreach (int child in childs.Distinct())
            {
                if (child >= pool.RootNode.Documents.Count) continue;
                while (pool.RootNode.Documents[child].Sources.Count > 0)
                    pool.RootNode.Documents[parent].Sources.Add(pool.RootNode.Documents[child].Sources[0]);
            }

            // Delete childs
            foreach (int child in childs.OrderByDescending(n => n).Distinct())
            {
                if (child < pool.RootNode.Documents.Count)
                    pool.RootNode.Documents.RemoveAt(child);
            }
        }
        #endregion

        #region Export attachments
        public static void ExportAttachments(DataPool pool, int masterDocument)
        {
            if (masterDocument >= pool.RootNode.Documents.Count) return;

            foreach (Source s in pool.RootNode.Sources)
            {
                Document doc = pool.RootNode.Documents[masterDocument].Clone() as Document;
                doc.SetExportPdfSource(s.Clone() as Source);
                pool.RootNode.Documents.Add(doc);
            }
        }
        #endregion

        #region Render attachments

        private static IRenderer renderer = null;
        private static List<string> supportedAttachments = new List<string>()
        {
            "doc", "docx", "xls", "xlsx", "ppt", "pptx",
        };

        public static void RenderAttachments(DataPool pool, string exchangeFolder, int position = -1, string documentClass = "Unknown")
        {
            // Get renderer and return if not usable
            if (renderer == null) renderer = Renderer_Manager.GetRenderer();
            if (!renderer.CanRender) return;

            string cacheFolder = DataPool.CacheFolder;

            if (position >= pool.RootNode.Documents.Count) position = -1;

            foreach (Source s in pool.RootNode.Sources)
            {
                if (!supportedAttachments.Contains(s.SourceType)) continue;

                // Convert the HTML Body text of the email into an image source via intermediate temp file
                string resultFile = Path.Combine(
                    cacheFolder,
                    Path.GetFileName(Path.GetTempFileName()).Replace(".tmp", ".pdf")
                );

                // Handle embedded and not embedded documents
                string inputFile = s.Url;
                if (s.IsEmbedded)
                {
                    inputFile = Path.GetTempFileName() + "." + s.SourceType;
                    File.WriteAllBytes(inputFile, s.ReadBytes());
                }

                // Do the rendition
                renderer.Render(exchangeFolder, inputFile, resultFile);

                if (s.IsEmbedded) File.Delete(inputFile);

                // Create document and add image source
                Document doc = new Document(pool, documentClass);
                ImageSource source = new ImageSource(pool, resultFile) { IsArchived = false };
                doc.Sources.Add(source);

                // Insert new document into data pool
                if (position == -1)
                    pool.RootNode.Documents.Add(doc);
                else
                {
                    //if (position > doc.Sources.Count) position = doc.Sources.Count;
                    pool.RootNode.Documents.Insert(Math.Min(position++, pool.RootNode.Documents.Count), doc);
                }
            }
        }
        #endregion

        #region Remove uncertain rows
        public class UncertainRowsDefinition
        {
            public bool Top { get; set; } = false;
            public List<string> TopFields { get; set; }
            public bool Bottom { get; set; } = false;
            public List<string> BottomFields { get; set; }
        }

        public static void RemoveUncertainRows(DataPool pool, string tableFieldname, UncertainRowsDefinition ucrd = null)
        {
            if (ucrd == null) ucrd = new UncertainRowsDefinition() { Bottom = true };

            foreach (Document doc in pool.RootNode.Documents)
            {
                List<TableRow> topRows = new List<TableRow>();
                List<TableRow> bottomRows = new List<TableRow>();
                bool certainRowFound = false;

                if (doc.Fields[tableFieldname] == null)
                    throw new Exception("Field " + tableFieldname + " not found in document");

                Table table = doc.Fields[tableFieldname] as Table;
                int nofRows = table.Rows.Count;

                foreach (TableRow tr in table.Rows)
                {
                    if (tr.State != DataState.Uncertain)
                    {
                        certainRowFound = true;
                        bottomRows.Clear();
                        continue;
                    }
                    if (!certainRowFound)
                        topRows.Add(tr);
                    else
                        bottomRows.Add(tr);
                }
                if (ucrd.Top) removeRows(table, topRows, ucrd.TopFields);
                if (ucrd.Bottom) removeRows(table, bottomRows, ucrd.BottomFields);
            }
        }

        private static void removeRows(Table table, List<TableRow> rows, List<string> fields)
        {
            foreach (TableRow tr in rows)
            {
                bool keep = false;
                if (fields != null)
                    foreach (string fieldname in fields)
                        if (tr.Columns[fieldname] != null && tr.Columns[fieldname].State == DataState.Ok)
                            keep = true;
                if (!keep) table.Rows.Remove(tr);
            }
        }

        #endregion

        #region Auto-skip validation
        public static void AutoSkipValidation(DataPool pool)
        {
            bool skip = true;
            foreach (Document doc in pool.RootNode.Documents)
            {
                foreach (Field f in doc.Fields)
                    if (f.State == DataState.Error)
                    {
                        skip = false;
                        break;
                    }
                if (skip == false) break;
            }
            Bool skipValidationField = pool.RootNode.Fields["cc_SkipValidation"] as Bool;
            skipValidationField.SetValue(skip);
            skipValidationField.State = DataState.Ok;
        }
        #endregion

        #region Sort documents
        public static void SortDocuments(DataPool pool, List<string> orderList = null)
        {
            List<KeyValuePair<Document, string>> docList = new List<KeyValuePair<Document, string>>();
            foreach (Document doc in pool.RootNode.Documents)
                docList.Add(new KeyValuePair<Document, string>(doc, doc.Name));

            docList = docList.OrderBy(n => n.Value).ToList();

            List<KeyValuePair<Document, string>> finalList = new List<KeyValuePair<Document, string>>();
            if (orderList == null)
                finalList = docList;
            else
            {
                foreach (string classname in orderList)
                    finalList.AddRange(docList.Where(n => n.Value == classname).ToList());
                foreach (KeyValuePair<Document, string> doc in docList)
                    if (!orderList.Contains(doc.Value))
                        finalList.Add(doc);
            }
            pool.RootNode.Documents.Clear();
            foreach (KeyValuePair<Document, string> doc in finalList)
                pool.RootNode.Documents.Add(doc.Key);
        }
        #endregion

        #region Show all fields
        public static void ShowAllFields(DataPool pool, string lookupListFieldname, bool showSubFields = true)
        {
            foreach (Document doc in pool.RootNode.Documents)
            {
                IField lookupList = doc.Fields[lookupListFieldname];
                lookupList.State = DataState.Ok;
                int idx = 0;
                foreach (IField f in doc.Fields)
                {
                    if (f == lookupList) continue;
                    if (f.Fields.Count > 0 && showSubFields)
                        foreach (IField sf in f.Fields) addZone(lookupList, sf, ref idx);
                    else
                        addZone(lookupList, f, ref idx);
                }
            }
        }

        private static void addZone(IField lookupList, IField f, ref int idx)
        {
            if (idx >= lookupList.Fields.Count) return;
            if (f.State == DataState.Empty) return;
            if (f.Zone.X == 0 && f.Zone.Y == 0 && f.Zone.Width == 0 && f.Zone.Height == 0) return;

            lookupList.Fields[idx].Zone = f.Zone;
            foreach (Source s in f.Sources)
                lookupList.Fields[idx].Sources.Add(s.Clone() as Source);
            lookupList.Fields[idx].Value = f.Name;
            lookupList.Fields[idx].State = DataState.Ok;
            idx++;
        }
        #endregion

        #region Table automation column-wise
        public const string TableColumnWise_AnnotationName = "SU_TableColumnWise";
        public const string TableColumnWiseMapping_AnnotationName = "SU_TableColumnWiseMapping";
        public static void TableColumnWise(DataPool pool, string tableName, Dictionary<string, string> nameMapping = null)
        {
            List<KeyValuePair<string, string>> mapping = null;
            if (nameMapping != null)
                mapping = nameMapping.ToList();

            foreach (Document doc in pool.RootNode.Documents)
            {
                doc.Annotations.Add(new Annotation(pool, TableColumnWise_AnnotationName, tableName));
                if (nameMapping != null)
                    doc.Annotations.Add(new Annotation(pool,
                        TableColumnWiseMapping_AnnotationName,
                        SIEESerializer.ObjectToString(mapping)));
            }
            //File.WriteAllText(@"c:\temp\tt.txt", TableColumnWiseMapping_AnnotationName + " -5- ");
        }
        #endregion

        #region Image meta data
        public static string GetImageMetaData(Document doc, string group, string tag)
        {
            ImageSourceInstance isi = (ImageSourceInstance)doc.Sources[0].Instance.ParentInstance;
            string inputFile = isi.Url;

            IImageMetadataExtraction extractor = ImageMetadataExtraction_Manager.GetExtractor();
            try
            {
                Dictionary<string, string> tags = extractor.Extract(inputFile, group);
                trace.WriteInfo("tags.Count=" + tags.Count);
                if (tags.ContainsKey(tag)) return tags[tag];
                return string.Empty;
            }
            catch (Exception e)
            {
                trace.WriteError("extractor.Extract(" + inputFile + ", " + group + ") failed: " + e.Message);
                return string.Empty;
            }
        }
        #endregion

        #region Invoice Totals
        public const string SchemaFileName_AnnotationName = "SU_SchemaFileName";

        public static void GetInvoiceTotalsField(DataPool pool, string invoiceTotalsFieldName, string subField, string occField = null)
        {
            subField = "InvoiceTotals__" + subField;
            occField = occField == null ? subField : occField;

            foreach (Document doc in pool.RootNode.Documents)
            {
                Field newField = doc.Fields[invoiceTotalsFieldName].Fields[subField].Clone() as Field;
                newField.Rename(occField);
                doc.Fields[occField] = newField;
            }
        }

        public static void GetVatRates(DataPool pool,
            string invoiceTotalsFieldName,
            string tableFieldName,
            Dictionary<string, string> columnMameMapping = null)
        {
            List<string> columnNames = new List<string>() { "TotalAmount", "NetAmount", "VatAmount", "VatRate", "Currency" };
            Dictionary<string, string> mapping = new Dictionary<string, string>();
            foreach (string columnName in columnNames)
                mapping[columnName] = columnName;

            if (columnMameMapping != null) foreach (string key in columnMameMapping.Keys)
                    if (!mapping.ContainsKey(key))
                        throw new Exception("Unknown column name " + key);
                    else
                        mapping[key] = columnMameMapping[key];

            loadSchema(pool);

            foreach (Document doc in pool.RootNode.Documents)
            {
                Table from = doc.Fields[invoiceTotalsFieldName].Fields["InvoiceTotals__VatGroups"] as Table;
                Table to = doc.Fields[tableFieldName] as Table;

                for (int i = 0; i != from.Rows.Count; i++)
                {
                    to.Rows.Add("row");
                    foreach (string c in columnNames)
                        if (from.Rows[i].Fields[c].State == DataState.Ok)
                        {
                            Field newField = from.Rows[i].Fields[c].Clone() as Field;
                            newField.Rename(mapping[c]);
                            to.Rows[i].Fields[mapping[c]] = newField;
                        }
                }
            }

        }
        #endregion

        #region StoreTableAsCVS
        public static void StoreTableAsCVS(DataPool pool,
            string folderfield, string filefield,
            string TableFieldName, List<string> columnNames)
        {
            foreach (Document doc in pool.RootNode.Documents)
            {
                string result = string.Empty;
                foreach (string col in columnNames) result += col + ",";

                Table table = doc.Fields[TableFieldName] as Table;
                foreach (TableRow row in table.Rows)
                {
                    result += "\n";
                    foreach (string col in columnNames) result += row.Fields[col].Value + ",";
                }

                string basepath = doc.Annotations["FileOutputPath"].Value;
                string filepath = Path.Combine(basepath, doc.Fields[folderfield].Value, doc.Fields[filefield].Value);
                System.IO.FileInfo file = new System.IO.FileInfo(filepath);
                file.Directory.Create();
                File.WriteAllText(filepath, result);
            }
        }

        #endregion

        #region OcrDetails
        private static DataPool ocrDetailsPool;
        private static Dictionary<string, List<OcrCharacter>> ocrDetailsDResultCache = new Dictionary<string, List<OcrCharacter>>();

        public static void OcrDetails_SetDataPool(DataPool pool)
        {
            ocrDetailsPool = pool;
            ocrDetailsDResultCache.Clear();
        }

        public static OcrCharacter[] OcrDetails_GetDetails(int docIndex, string fieldname)
        {
            Field f = ocrDetailsPool.RootNode.Documents[docIndex].Fields[fieldname] as Field;
            string dokustarResultFile = ocrDetails_findPage(f.Sources[0]);

            if (!ocrDetailsDResultCache.ContainsKey(dokustarResultFile))
                ocrDetailsDResultCache[dokustarResultFile] = loadDokustarResult(dokustarResultFile);

            OcrCharacter[] result =
                ocrDetailsDResultCache[dokustarResultFile]
                .Where(n => n.Box.X >= f.Zone.X)
                .Where(n => n.Box.Y >= f.Zone.Y)
                .Where(n => n.Box.X + n.Box.Width <= f.Zone.X + f.Zone.Width)
                .Where(n => n.Box.Y + n.Box.Height <= f.Zone.Y + f.Zone.Height)
                .ToArray();

            return result;
        }

        public static string OcrCharactersToString(OcrCharacter[] ocrCharacters, bool spaces = true)
        {
            string result = string.Empty;

            int medianWidths = 0;
            int[] widths = ocrCharacters.Select(n => n.Box.Width).OrderBy(n => n).ToArray();
            if (widths.Length <= 1)
                spaces = false;
            else
                medianWidths = widths[widths.Length / 2];

            for (int i = 0; i < ocrCharacters.Length; i++)
            {
                if (spaces && i > 0)
                {
                    Rectangle prev = ocrCharacters[i-1].Box;
                    Rectangle curr = ocrCharacters[i].Box;
                    if (curr.X - (prev.X + prev.Width) > medianWidths / 1.5)
                        result += " ";
                }
                result += ocrCharacters[i].Choices[0].Character;
            }
            //foreach (OcrCharacter ochar in ocrCharacters)
            //{
            //    result += ochar.Choices[0].Character;
            //}
            return result;
        }

        private static List<OcrCharacter> loadDokustarResult(string filename)
        {
            List<OcrCharacter> result = new List<OcrCharacter>();

            DResult dr = new DResult();
            dr.FileOpen(filename);
            XmlElement sceXml = dr.Sce.ThisXmlNode as XmlElement;

            foreach (XmlElement character in sceXml.SelectNodes("ocr/characterdetails/line/geoword/logword/char"))
            {
                string box = character.SelectNodes("box")[0].InnerText;
                string ocr = character.SelectNodes("choices")[0].InnerText;

                string[] rectVals = box.Split(' ');
                Rectangle r = new Rectangle(
                    int.Parse(rectVals[0]), int.Parse(rectVals[1]),
                    int.Parse(rectVals[2]), int.Parse(rectVals[3]));

                List<Choice> choices = new List<Choice>();
                string[] choicesVals = ocr.Split(' ').Skip(1).ToArray();
                for (int i = 0; i < choicesVals.Length; i = i + 2)
                    choices.Add(new Choice() { Character = choicesVals[i][0], Confidence = int.Parse(choicesVals[1]) });

                result.Add(new OcrCharacter() { Box = r, Choices = choices });
            }
            return result;
        }

        public struct Choice
        {
            public char Character { get; set; }
            public int Confidence { get; set; }
        }
        public struct OcrCharacter
        {
            public Rectangle Box { get; set; }
            public List<Choice> Choices {get; set;}
        }

        private static string ocrDetails_findPage(Source source)
        {
            foreach (ImageSourceInstance isi in ocrDetailsPool.RootNode.SourceInstances)
                foreach (PageInstance pi in isi.Pages)
                    if (pi.Guid == source.Guid)
                        foreach (VariantSourceInstance ivi in pi.Variants)
                            if (ivi is FullTextVariantInstance)
                                return ivi.Url;
            return null;
        }
        #endregion

        #region Utlilities
        private static void loadSchema(DataPool pool)
        {
            string schemaFile = pool.RootNode.Annotations[SchemaFileName_AnnotationName].Value;
            DOKuStar.Data.Xml.Schema.DataSchema schema = new DOKuStar.Data.Xml.Schema.DataSchema(schemaFile);
            pool.DataSchema = schema;
        }
        #endregion
    }
}
