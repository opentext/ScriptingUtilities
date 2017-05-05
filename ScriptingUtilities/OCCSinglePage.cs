using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ScriptingUtilities
{
    /// This class loads and stores a single page image file and executes image processing 
    /// function on that page.
    public class OCCSinglePage : IDisposable
    {
        #region Properties and members
        private Bitmap bitmap;      // here is the image of the page
        private Graphics graphics;  // this is the graphics object used to manipulate the bitmap
        private bool convertImage = false;      // indicates that the bitmap was converted
        private List<PropertyItem> properties;  // Saved image properties 

        private PixelFormat pixelFormat;
        public PixelFormat PixelFormat { get { return pixelFormat; } }
        public bool BlackAndWhite { get { return PixelFormat == PixelFormat.Format1bppIndexed; } }
        public int Width { get { return bitmap.Width; } }
        public int Height { get { return bitmap.Height; } }
        public float Hresolution { get {return bitmap.HorizontalResolution; } }
        public float Vresolution { get { return bitmap.VerticalResolution; } }
        #endregion

        #region Load and store
        public OCCSinglePage(string filename, int rotate = 0)
        {
            Bitmap tmp = null;
            try
            {
                bitmap = Bitmap.FromFile(filename) as Bitmap;
                pixelFormat = bitmap.PixelFormat;

                properties = new List<PropertyItem>();
                foreach (PropertyItem pItem in bitmap.PropertyItems) properties.Add(pItem);

                // Indexed images need to be converted
                convertImage = 
                    PixelFormat == PixelFormat.Format1bppIndexed || 
                    PixelFormat == PixelFormat.Format8bppIndexed;

                if (convertImage)
                    tmp = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
                else
                    tmp = new Bitmap(bitmap);

                tmp.SetResolution(Hresolution, Vresolution);
                graphics = Graphics.FromImage(tmp);

                if (convertImage) graphics.DrawImage(bitmap, 0, 0);

                // Adapt graphics object to given rotation
                int xOffset = 0;
                int yOffset = 0;
                switch (rotate)
                {
                    case 90:
                        xOffset = bitmap.Width;
                        yOffset = 0;
                        break;
                    case 180:
                        xOffset = bitmap.Width;
                        yOffset = bitmap.Height;
                        break;
                    case 270:
                        xOffset = 0;
                        yOffset = bitmap.Height;
                        break;
                }
                graphics.TranslateTransform(xOffset, yOffset);
                graphics.RotateTransform(rotate);

                bitmap.Dispose();
                bitmap = tmp;
                tmp = null;
            }
            finally
            {
                if (tmp != null) tmp.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (bitmap != null) { bitmap.Dispose(); bitmap = null; }
                if (graphics != null) { graphics.Dispose(); graphics = null; }
            }
        }

        public void Save(string filename)
        {
            Bitmap tmp;

            if (convertImage && PixelFormat == PixelFormat.Format1bppIndexed)
            {
                // the default palette used when setting indexed pixelformat results in poor quality for 256 color and gray scale images
                // ==> leave 8-bit palette images untouched and archive as RGB
                tmp = bitmap.Clone(new Rectangle(0, 0, Width, Height), PixelFormat);
                bitmap.Dispose();
                bitmap = tmp;
                tmp = null;
            }
            ImageCodecInfo codecInfo = ImageCodecInfo.GetImageEncoders().First(i => i.MimeType == "image/tiff");
            EncoderParameters iparams = new EncoderParameters(1);
            Encoder iparam = Encoder.Compression;
            EncoderParameter iparamPara = new EncoderParameter(iparam,
                PixelFormat == PixelFormat.Format1bppIndexed ?
                (long)(EncoderValue.CompressionCCITT4) :
                (long)(EncoderValue.CompressionLZW));
            iparams.Param[0] = iparamPara;
            bitmap.SetResolution(Hresolution, Vresolution);
            foreach (PropertyItem pItem in properties)
                bitmap.SetPropertyItem(pItem);
            bitmap.Save(filename, codecInfo, iparams);
        }
        #endregion

        #region Unrelated
        public void RemoveInputText()
        {
            List<PropertyItem> toBeRemoved = properties.Where(n => n.Id >= 33025 && n.Id <= 33028).ToList();
            foreach (PropertyItem pi in toBeRemoved) properties.Remove(pi);
        }
        #endregion

        #region Imprinting
        public void AddTextToImage(string text, int imageRotation, int orientation, int position, int offsetX, int offsetY)
        {
            int xOffset = mm2pixel(offsetX, (int)Hresolution);
            int yOffset = mm2pixel(offsetY, (int)Vresolution);
            // Set string format - orientation.
            System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();
            if (orientation == 1)
            {
                drawFormat.FormatFlags = System.Drawing.StringFormatFlags.DirectionVertical;
            }

            using (System.Drawing.Font arialFont = new System.Drawing.Font("Arial", 14))
            {
                // Measure string
                System.Drawing.PointF origin = new System.Drawing.PointF(0.0f, 0.0f);
                System.Drawing.SizeF stringSize = graphics.MeasureString(text, arialFont, origin, drawFormat);

                float posX = xOffset; // top left
                float posY = yOffset;

                if (orientation != 1) // horizontal
                {
                    switch (position)
                    {
                        case 1: // top right
                            posX = Width - stringSize.Width - xOffset;
                            break;

                        case 2: // bottom left
                            posY = Height - stringSize.Height - yOffset;
                            break;

                        case 3: // bottom right
                            posX = Width - stringSize.Width - xOffset;
                            posY = Height - stringSize.Height - yOffset;
                            break;
                    }
                }
                else //vertical
                {
                    switch (position)
                    {
                        case 1: // top right
                            posX = Width - stringSize.Width - xOffset;
                            break;

                        case 2: // bottom left
                            posY = Height - stringSize.Height - yOffset;
                            break;

                        case 3: // bottom right
                            posX = Width - stringSize.Width - xOffset;
                            posY = Height - stringSize.Height - yOffset;
                            break;
                    }
                }

                // Set point for upper-left corner of string.
                System.Drawing.PointF ulCorner = new System.Drawing.PointF(posX, posY);
                graphics.DrawString(text, arialFont, System.Drawing.Brushes.Black, ulCorner, drawFormat);
            }
        }
        #endregion

        #region Redaction
        public void FillRectangle(Brush color, Rectangle zone)
        {
            graphics.FillRectangle(color, mm2pixel(zone));
        }

        private Rectangle mm2pixel(Rectangle zone)
        {
            int addon = 2;
            return new Rectangle(
                mm2pixel(zone.X, (int)Hresolution),
                mm2pixel(zone.Y, (int)Vresolution),
                mm2pixel(zone.Width, (int)Hresolution) + addon,
                mm2pixel(zone.Height, (int)Hresolution) + addon
            );
        }

        // Convert 1/10th mm to pixels
        private int mm2pixel(int tenthMM, int resolution)
        {
            return (int)(tenthMM / 254.0 * resolution);
        }
        #endregion

        #region Get snippet
        public byte[] GetSnippet(Rectangle zone)
        {
            Rectangle r = mm2pixel(zone);

            Bitmap newBmp = new Bitmap(r.Width, r.Height);
            newBmp.SetResolution(Hresolution, Vresolution);
            Graphics g = Graphics.FromImage(newBmp);
            g.DrawImage(bitmap, 0, 0, r, GraphicsUnit.Pixel);

            Bitmap save = bitmap;
            bitmap = newBmp;
            string tmpFile = Path.GetTempFileName();
            Save(tmpFile);
            byte[] result = File.ReadAllBytes(tmpFile);
            File.Delete(tmpFile);
            bitmap = save;
            return result;
        }
        #endregion
    }
}
