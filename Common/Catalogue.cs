using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class Catalogue
{
    public long Itemcode { get; set; }

    public string Description { get; set; } = null!;

    public bool Active { get; set; }

    public DateTime CreatedOn { get; set; }

    public string DescriptionPos { get; set; } = null!;

    public string DescriptionWeb { get; set; } = null!;

    public long? DescriptionsOtherLanguages { get; set; }

    public long DefaultVatCategory { get; set; }

    public bool VatDependsOnUser { get; set; }

    public bool VatCategoryAdjustable { get; set; }

    public bool PriceManual { get; set; }

    public bool EnforceAboveCost { get; set; }

    public bool ActiveWeb { get; set; }

    public bool ExpiryTrackingEnabled { get; set; }

    public long PermissionsCategory { get; set; }
}
