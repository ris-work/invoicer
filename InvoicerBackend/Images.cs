using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class Images
    {
        public static void AddCatalogueDefaultImageEndpoints (this WebApplication app){
            app.AddEndpointWithBearerAuth<long>("CatalogueDefaultImageGet", (AS, P) => {
                long ItemCode = ((long)AS);
                InventoryImage? Image  = null;
                using(var ctx = new NewinvContext())
                {
                    var ImageQ = ctx.InventoryImages.Where(e => e.Itemcode == ItemCode && e.Imageid == 0);
                    if(ImageQ.Count() > 0)
                    {
                        Image = ImageQ.First();
                    }
                }
                return Image;
            }, "Refresh");
            app.AddEndpointWithBearerAuth<InventoryImage>("CatalogueDefaultImageSet", ( AS, P) => {
                InventoryImage InputImage = (InventoryImage)AS;
                using(var ctx = new NewinvContext())
                {
                    var Image = ctx.InventoryImages.Where(e => e.Itemcode == InputImage.Itemcode && e.Imageid == 0);
                    if(Image.Count() > 0)
                    {
                        var CurrentImage = Image.First();
                        CurrentImage.ImageBase64 = InputImage.ImageBase64;
                    }
                    else
                    {
                        ctx.InventoryImages.Add(new InventoryImage(){ Itemcode = InputImage.Itemcode, Imageid = 0, ImageBase64 = InputImage.ImageBase64});
                    }
                    ctx.SaveChanges();
                }
                return InputImage.Itemcode;
            }, "Refresh");
        }
    }
}
