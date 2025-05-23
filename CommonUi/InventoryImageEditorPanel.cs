using Eto.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using MySkiaApp;

namespace CommonUi
{
    public class InventoryImageEditorPanel: Panel
    {
        public delegate string? ImageRetriever(long? itemcode, long imageid);
        public delegate long? ImageSaver(long? itemcode, long imageid, string Image);
        string? NewImageBase64 = null;
        public string LoadLogoImage(long? _, long _2)
        {
            var orig = SKBitmap.Decode("logo.png");
            var resized = orig.ResizeToMaxDimension(500);
            var resizedBase64 = resized.ToJpegBase64String(50);
            return resizedBase64;
        }
        public InventoryImageEditorPanel(long? itemcode, ImageRetriever? IR, ImageSaver? IS, long imageid=0)
        {
            if (IR == null) IR = LoadLogoImage;
            Button[] Buttons = Array.Empty<Button>();
            Button OpenButton = new Button()
            {
                Text = "Open"
            };
            Button SaveButton = new Button()
            {
                Text = "Save"
            };
            Button DownloadImageButton = new Button()
            {
                Text = "Download"
            };
            Button PopoutImageButton = new Button()
            {
                Text = "Pop out"
            };
            ImageView CurrentImageViewer = new ImageView() { Size = new Eto.Drawing.Size(150, 150)};
            ImageView NewImageViewer = new ImageView() { Size = new Eto.Drawing.Size(150, 150) };
            string? RetrievedImage = IR(itemcode, imageid);
            if (itemcode != null && RetrievedImage != null)
            {
                byte[] CurrentImage = Convert.FromBase64String(RetrievedImage);
                using (var MSCurrentImage = new MemoryStream(CurrentImage)) {
                    Eto.Drawing.Image image = new Eto.Drawing.Bitmap(MSCurrentImage);
                    CurrentImageViewer.Image = image;
                }
            }
            OpenButton.Click += (_, _) => {
                var OID = new OpenFileDialog();
                OID.Filters.Concat(new List<FileFilter> { new FileFilter("Image files", ["*.png","*.jpg","*.jpeg"]) });
                OID.ShowDialog(this);
                if(OID.FileName != null) {
                    var orig = SKBitmap.Decode(OID.FileName);
                    var resized = orig.ResizeToMaxDimension(500);
                    var resizedBase64 = resized.ToJpegBase64String(100);
                    NewImageBase64 = resizedBase64;
                    byte[] CurrentImage = Convert.FromBase64String(NewImageBase64);
                    using (var MSCurrentImage = new MemoryStream(CurrentImage))
                    {
                        Eto.Drawing.Image image = new Eto.Drawing.Bitmap(MSCurrentImage);
                        NewImageViewer.Image = image;
                    }
                }
            };
            PopoutImageButton.Click += (_, _) =>
            {
                if(itemcode != null && RetrievedImage != null)
                {
                    var PopoutWindow = new Form() { 
                        Content = new StackLayout(new Label() { Text = $"{itemcode}" }, new ImageView() { Image = CurrentImageViewer.Image}) { 
                            VerticalContentAlignment = VerticalAlignment.Stretch,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        },
                        Maximizable = false
                    };
                    PopoutWindow.Show();
                }
            };
            SaveButton.Click += (_, _) => {
                if(NewImageBase64 != null)
                    IS(itemcode, 0, NewImageBase64);
            };
            DownloadImageButton.Click += (_, _) => { };
            var ButtonsPanel = new StackLayout(OpenButton, SaveButton, DownloadImageButton, PopoutImageButton) {Padding = 1, Spacing = 2, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            Content = new GroupBox() {  Content = new StackLayout(ButtonsPanel, new Label() { Text = $"Current: {RetrievedImage?.Length??0}B" }, CurrentImageViewer, new Label() { Text = $"New: {NewImageBase64?.Length??0}" }, NewImageViewer) { VerticalContentAlignment = VerticalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch }, Text = "Image/Photo" };
            

        }
    }
}
