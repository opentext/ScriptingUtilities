﻿using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using DOKuStar.Data.Xml;
using DOKuStar.Runtime.Sitemap;
using DOKuStar.Runtime.ScriptingHost;
using DOKuStar.Link;
using DOKuStar.Link.Import;

namespace ScriptingUtilities
{
    public class ScriptingUtilitiesProfileExtension : Captaris.Services.BaseCustomProfileExtension
    {
        #region OCC Validation functions
        public override bool FieldChangingBySce(
           IField field,
           ref string value,
           ref Rectangle zone,
           Rectangle sceRawZone,
           ref bool handled
        )
        {
            if (field is Table) return true;
            SqlVerification(field.OwnerDataPool);
            if (field.Annotations["SCERawZone"] != null) zone = sceRawZone;
            return true;
        }

        public override void FieldDeactivated( IField field, bool sce, ref bool handled)
        {
            if (field is Table) return;
            SqlVerification(field.OwnerDataPool);
        }

        public override void FieldChanged(IField field, ref bool handled)
        {
            if (field is Table) return;
            SqlVerification(field.OwnerDataPool);
        }
        #endregion

        #region OCC transform
        public override XmlDocument transform(XmlDocument data, IParameters parameters)
        {
            base.transform(data, parameters);
            DataPool pool = new DataPool(data);

            switch (ScriptingManager.GetScriptName(pool))
            {
                case ScriptingManager.prepareImportName: return prepareImportSequence((pool));
                case ScriptingManager.importName: break;
                case ScriptingManager.separationName: break;
                case ScriptingManager.classificationName: break;
                case ScriptingManager.indexingName: return indexingSequence(pool);
                case ScriptingManager.exportName: break;
                case ScriptingManager.afterPDFGenerationName: return pdfCompression(pool);
                case ScriptingManager.afterExportName: break;
                case ScriptingManager.mainRecognitionExceptionName: return recognitionExceptionSequence(pool);
            }
            return data;
        }

        private XmlDocument prepareImportSequence(DataPool pool)
        {
            return getSchema(pool);
        }

        private XmlDocument indexingSequence(DataPool pool)
        {
            XmlDocument xd;
            xd = SqlVerification(pool);
            return tableColumnWise(new DataPool(xd));
        }

        // Store schema filename in annotation to be used by ScriptingUtilities
        private XmlDocument getSchema(DataPool pool)
        {
            pool.RootNode.Annotations.Add(new Annotation(pool, 
                ScriptingUtilities.SchemaFileName_AnnotationName, getSchemaFileName()));
            return pool.XmlDocument;
        }

        private XmlDocument recognitionExceptionSequence(DataPool pool)
        {
            return pool.XmlDocument;
        }
        #endregion

        #region PDF compression
        // PDF compression uses a PdfCompression object to compress the PDF
        // The compression setting can change from dodument to document 
        private IPdfCompressor pdfCompressor = null;
        private string PdfCompressionConfigParameter = string.Empty;
        private const string CompressionAnnotationName = "SU_PdfCompression";

        private XmlDocument pdfCompression(DataPool pool)
        {
            // Get compressor and return if not usable
            if (pdfCompressor == null) pdfCompressor = PdfCompressor_Manager.GetCompressor();
            if (!pdfCompressor.CanCompress) return pool.XmlDocument;

            foreach (Document doc in pool.RootNode.Documents)
            {
                if (doc.Annotations[CompressionAnnotationName] != null)
                {
                    if (doc.Annotations[CompressionAnnotationName].Value != PdfCompressionConfigParameter)
                    {
                        // Change in configuration -> get new PdfCompression object 
                        PdfCompressionConfigParameter = doc.Annotations[CompressionAnnotationName].Value;

                        // Get content of configuration file and store as local file
                        string tmpFile = Path.GetTempFileName();
                        using (Stream stream = this.OpenFileStream(Path.GetFileName(PdfCompressionConfigParameter)))   
                            using (FileStream filestream = new System.IO.FileStream(tmpFile, FileMode.Create))
                                stream.CopyTo(filestream);

                        // Initialize compression objec and delete tmp file
                        pdfCompressor.Initialize(tmpFile);
                        File.Delete(tmpFile);
                    }

                    // Call the compressor using a temporary file
                    Source pdfSource = doc.GetExportPdfSource();
                    string tmp = Path.GetTempFileName() + ".pdf";
                    pdfCompressor.Compress(doc.GetExportPdfSource().Url, tmp);
                    doc.SetExportPdfSource(pool.CreatePdfSource(tmp));
                    File.Delete(tmp);
                }
            }
            return pool.XmlDocument;
        }
        #endregion

        #region Sql Verification

        // We maintain a cache of SqlVerificationSepc and reuse them from batch to batch.
        // Sql connections can be expensive.
        private List<SqlVerificationSepc> specs = new List<SqlVerificationSepc>();

        private XmlDocument SqlVerification(DataPool pool)
        {
            Annotation verify = pool.RootNode.Annotations[ScriptingUtilities.SqlVerifyAnnotationName];
            if (verify != null) // is sql verification demanded?
            {
                pool.RootNode.Annotations.Remove(verify); // reset annotation, it should be done only for this step

                SqlVerificationSepc spec = specs.Where(n => n.Name == verify.Value).FirstOrDefault();
                if (spec == null) // do we have this SqlSpec not yet in cache?
                {
                    // Read SqlSpec from annotation
                    Annotation specsAnnotation = pool.RootNode.Annotations[ScriptingUtilities.SqlVerificationSpecsAnnotationName];
                    if (specsAnnotation == null)
                        throw new Exception("No SqlVerificationSpecs found");
                    specs = SIEESerializer.StringToObject(specsAnnotation.Value) as List<SqlVerificationSepc>;
                    spec = specs.Where(n => n.Name == verify.Value).FirstOrDefault();
                    if (spec == null)
                        throw new Exception("SqlVerificationSpec " + verify.Value + " not found");
                }
                sqlVerify(pool, spec); // do it...
            }
            return pool.XmlDocument;
        }

        private void sqlVerify(DataPool pool, SqlVerificationSepc spec)
        {
            // Do verification for all documents if document class matches.
            spec.TableSpec.Connect();
            foreach (Document doc in pool.RootNode.Documents)
                if (doc.Name == spec.Classname)
                    sqlVerify(doc, spec);
        }

        private void sqlVerify(Document doc, SqlVerificationSepc spec)
        {
            // Create the Sql statement
            string tmp = string.Empty;
            foreach (SqlFieldMapping fh in spec.FieldMap)
            {
                if (tmp != string.Empty) tmp += ", ";
                tmp += ecscapeQuoteCharacters(fh.Column);
            }
            string sqlString = "Select " + tmp + " From " + spec.TableSpec.Table + " Where ";

            tmp = string.Empty;
            foreach (SqlFieldMapping fh in spec.FieldMap)
            {
                string fieldValue = doc.Fields[fh.OCCField].Value;
                if (string.IsNullOrEmpty(fieldValue)) continue;
                if (tmp != string.Empty) tmp += " AND ";
                tmp +=
                    ecscapeQuoteCharacters(fh.Column) + " = '" +
                    ecscapeQuoteCharacters(fieldValue) + "'";
            }
            sqlString += tmp;


            // Shoot sql statement off, leave confirmation field in case of error
            bool success = spec.TableSpec.Read(sqlString, spec.FieldMap);

            // Transfer results to data pool
            foreach (SqlFieldMapping fh in spec.FieldMap)
            {
                if (success)
                {
                    doc.Fields[fh.OCCField].Value = fh.Value;
                    doc.Fields[fh.OCCField].State = DataState.Ok;
                }
                else
                    doc.Fields[fh.OCCField].State = DataState.Error;
            }
        }

        private static string ecscapeQuoteCharacters(string value)
        {
            return value.Replace("'", "''");
        }
        #endregion

        #region Table automation column-wise
        private XmlDocument tableColumnWise(DataPool pool)
        {
            loadSchema(pool);

            foreach (Document doc in pool.RootNode.Documents)
            {
                Annotation tcwAnnoation = doc.Annotations[ScriptingUtilities.TableColumnWise_AnnotationName];
                if (tcwAnnoation == null) continue;
                string tableName = tcwAnnoation.Value;

                List<KeyValuePair<string, string>> mapping = null;
                Annotation tcwMappingAnnoation = doc.Annotations[ScriptingUtilities.TableColumnWiseMapping_AnnotationName];
                if (tcwMappingAnnoation != null)
                    mapping = (List<KeyValuePair<string, string>>)SIEESerializer.StringToObject(tcwMappingAnnoation.Value); 

                Table table = doc.Fields[tableName] as Table;
                table.Rows.Add("row");
                int nofRows = getNofRows(doc, table, mapping);
                for (int i = 0; i < nofRows-1; i++) table.Rows.Add("row");

                foreach (string column in getColumnNames(table.Rows[0] as TableRow))
                {
                    string fieldName = tcwMap(column, mapping);
                    if (fieldName == null) continue;

                    int i = 0;
                    foreach (Field f in getFieldAlternatives(doc, fieldName))
                        if (i++ < nofRows)
                        {
                            Field newField = f.Clone() as Field;
                            newField.Rename(column);
                            table.Rows[i - 1].Fields[column] = newField;
                        }
                }
            }
            return pool.XmlDocument;
        }

        private int getNofRows(Document doc, Table table, List<KeyValuePair<string, string>> mapping)
        {
            string fieldName = null;
            if (mapping == null)
                fieldName = table.Rows[0].Fields[0].Name;
            else
            {
                foreach (Field f in table.Rows[0].Fields)
                {
                    fieldName = tcwMap(f.Name, mapping);
                    if (fieldName != null) break;
                }
            }
            return doc.Fields[fieldName].Alternatives.Count + 1;
        }

        private string tcwMap(string name, List<KeyValuePair<string, string>> mapping)
        {
            if (mapping == null) return name;
            return mapping.Where(n => n.Key == name).Select(n => n.Value).FirstOrDefault();
        }

        private List<string> getColumnNames(TableRow row)
        {
            List<string> result = new List<string>();
            foreach (Field col in row.Fields) result.Add(col.Name);
            return result;
        }

        private List<IField> getFieldAlternatives(Document doc, string fieldname)
        {
            List<IField> result = new List<IField>();
            result.Add(doc.Fields[fieldname]);
            foreach (Field f in doc.Fields[fieldname].Alternatives)
                result.Add(f);
            return result;
        }

        private void loadSchema(DataPool pool)
        {
            if (base.processor != null)
            {
                string schemaFile = getSchemaFileName();
                DOKuStar.Data.Xml.Schema.DataSchema schema = new DOKuStar.Data.Xml.Schema.DataSchema(schemaFile);
                pool.DataSchema = schema;
            }
        }

        private string getSchemaFileName()
        {
            if (base.processor == null) throw new Exception("Cannot load schema, processor == null");
            
            //return = Path.Combine(base.processor.Sitemap.BasePath, base.processor.Sitemap.Schema);
            string path = new DirectoryInfo(base.processor.Sitemap.BasePath).Parent.FullName;
            return Path.Combine(path, "DataInput.xsd");
        }

        #endregion
    }
}
