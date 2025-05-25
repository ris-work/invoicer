using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class InventoryImage
{
    public long Itemcode { get; set; }

    public long Imageid { get; set; }

    public string ImageBase64 { get; set; } = null!;
}
