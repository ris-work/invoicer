using RV.InvNew.Common;
using System;
using System.Collections.Generic;

namespace SampleDataGenerator
{
    public static class SampleDataGenerator
    {
        /// <summary>
        /// Returns sample valid purchase items.
        /// Two sets are provided; by default (set==1) group 1 is returned.
        /// Each group returns a list of 4 purchases.
        /// </summary>
        public static List<Purchase> GetSampleValidPurchases(int set = 1)
        {
            if (set == 2)
            {
                List<Purchase> list = new List<Purchase>();

                // ------------------ Group 2 Purchases ------------------
                // Purchase 1 (Group 2)
                Purchase p1 = new Purchase();
                p1.ReceivedInvoiceId = 2002;
                p1.Itemcode = 201;
                p1.PackSize = 15;
                p1.PackQuantity = 4;
                p1.FreePacks = 0;
                p1.ReceivedAsUnitQuantity = 2;
                p1.FreeUnits = 1;
                p1.DiscountAbsolute = 10;
                p1.CostPerPack = 150;
                p1.CostPerUnit = 12;
                // TotalUnits = (4 * 15) + 2 + 1 = 60 + 3 = 63.
                p1.TotalUnits = 63;
                // GrossTotal = (4 * 150) + (2 * 12) = 600 + 24 = 624.
                p1.GrossTotal = 624;
                p1.NetTotalPrice = 624 - p1.DiscountAbsolute; // 624 - 10 = 614.
                p1.VatAbsolute = 25;
                p1.TotalAmountDue = p1.NetTotalPrice + p1.VatAbsolute; // 614 + 25 = 639.
                p1.GrossCostPerUnit = 624.0 / 63.0;
                p1.IsVatADisallowedInputTax = false;
                p1.NetCostPerUnit = 614.0 / 63.0;
                p1.NetTotalCost = p1.NetTotalPrice;
                double markupPercentage = 22.0;
                p1.SellingPrice = p1.NetCostPerUnit * (1 + markupPercentage / 100.0);
                p1.GrossMarkupAbsolute = p1.SellingPrice - p1.NetCostPerUnit;
                p1.GrossMarkupPercentage = p1.NetCostPerUnit > 0 ? (p1.GrossMarkupAbsolute / p1.NetCostPerUnit) * 100.0 : 0.0;
                p1.VatPercentage = 12;
                p1.VatCategory = 1;
                p1.VatCategoryName = "Standard";
                p1.ProductName = "P1";
                p1.ManufacturerBatchId = "P1";

                // Purchase 2 (Group 2)
                Purchase p2 = new Purchase();
                p2.ReceivedInvoiceId = 2002;
                p2.Itemcode = 202;
                p2.PackSize = 7;
                p2.PackQuantity = 8;
                p2.FreePacks = 2;
                p2.ReceivedAsUnitQuantity = 1;
                p2.FreeUnits = 0;
                p2.DiscountAbsolute = 6;
                p2.CostPerPack = 60;
                p2.CostPerUnit = 7;
                // TotalUnits = ((8+2)*7) + 1 = (10*7) + 1 = 70 + 1 = 71.
                p2.TotalUnits = 71;
                // GrossTotal = (8 * 60) + (1 * 7) = 480 + 7 = 487.
                p2.GrossTotal = 487;
                p2.NetTotalPrice = 487 - p2.DiscountAbsolute; // 487 - 6 = 481.
                p2.VatAbsolute = 15;
                p2.TotalAmountDue = p2.NetTotalPrice + p2.VatAbsolute; // 481 + 15 = 496.
                p2.GrossCostPerUnit = 487.0 / 71.0;
                p2.IsVatADisallowedInputTax = false;
                p2.NetCostPerUnit = 481.0 / 71.0;
                p2.NetTotalCost = p2.NetTotalPrice;
                markupPercentage = 18.0;
                p2.SellingPrice = p2.NetCostPerUnit * (1 + markupPercentage / 100.0);
                p2.GrossMarkupAbsolute = p2.SellingPrice - p2.NetCostPerUnit;
                p2.GrossMarkupPercentage = p2.NetCostPerUnit > 0 ? (p2.GrossMarkupAbsolute / p2.NetCostPerUnit) * 100.0 : 0.0;
                p2.VatPercentage = 12;
                p2.VatCategory = 1;
                p2.VatCategoryName = "Standard";
                p2.ProductName = "P1";
                p2.ManufacturerBatchId = "P1";

                // Purchase 3 (Group 2)
                Purchase p3 = new Purchase();
                p3.ReceivedInvoiceId = 2002;
                p3.Itemcode = 203;
                p3.PackSize = 10;
                p3.PackQuantity = 3;
                p3.FreePacks = 1;
                p3.ReceivedAsUnitQuantity = 4;
                p3.FreeUnits = 2;
                p3.DiscountAbsolute = 8;
                p3.CostPerPack = 90;
                p3.CostPerUnit = 11;
                // TotalUnits = ((3+1)*10) + 4 + 2 = 40 + 6 = 46.
                p3.TotalUnits = 46;
                // GrossTotal = (3 * 90) + (4 * 11) = 270 + 44 = 314.
                p3.GrossTotal = 314;
                p3.NetTotalPrice = 314 - p3.DiscountAbsolute; // 314 - 8 = 306.
                p3.VatAbsolute = 12;
                p3.TotalAmountDue = p3.NetTotalPrice + p3.VatAbsolute; // 306 + 12 = 318.
                p3.GrossCostPerUnit = 314.0 / 46.0;
                p3.IsVatADisallowedInputTax = false;
                p3.NetCostPerUnit = 306.0 / 46.0;
                p3.NetTotalCost = p3.NetTotalPrice;
                markupPercentage = 16.0;
                p3.SellingPrice = p3.NetCostPerUnit * (1 + markupPercentage / 100.0);
                p3.GrossMarkupAbsolute = p3.SellingPrice - p3.NetCostPerUnit;
                p3.GrossMarkupPercentage = p3.NetCostPerUnit > 0 ? (p3.GrossMarkupAbsolute / p3.NetCostPerUnit) * 100.0 : 0.0;
                p3.VatPercentage = 12;
                p3.VatCategory = 1;
                p3.VatCategoryName = "Standard";

                // Purchase 4 (Group 2)
                Purchase p4 = new Purchase();
                p4.ReceivedInvoiceId = 2002;
                p4.Itemcode = 204;
                p4.PackSize = 6;
                p4.PackQuantity = 10;
                p4.FreePacks = 0;
                p4.ReceivedAsUnitQuantity = 0;
                p4.FreeUnits = 0;
                p4.DiscountAbsolute = 15;
                p4.CostPerPack = 40;
                p4.CostPerUnit = 0; // no unit cost since no individual units
                // TotalUnits = (10 * 6) = 60.
                p4.TotalUnits = 60;
                // GrossTotal = 10 * 40 = 400.
                p4.GrossTotal = 400;
                p4.NetTotalPrice = 400 - p4.DiscountAbsolute; // 400 - 15 = 385.
                p4.VatAbsolute = 20;
                p4.TotalAmountDue = p4.NetTotalPrice + p4.VatAbsolute; // 385 + 20 = 405.
                p4.GrossCostPerUnit = 400.0 / 60.0;
                p4.IsVatADisallowedInputTax = false;
                p4.NetCostPerUnit = 385.0 / 60.0;
                p4.NetTotalCost = p4.NetTotalPrice;
                markupPercentage = 20.0;
                p4.SellingPrice = p4.NetCostPerUnit * (1 + markupPercentage / 100.0);
                p4.GrossMarkupAbsolute = p4.SellingPrice - p4.NetCostPerUnit;
                p4.GrossMarkupPercentage = p4.NetCostPerUnit > 0 ? (p4.GrossMarkupAbsolute / p4.NetCostPerUnit) * 100.0 : 0.0;
                p4.VatPercentage = 12;
                p4.VatCategory = 1;
                p4.VatCategoryName = "Standard";

                list.Add(p1);
                list.Add(p2);
                list.Add(p3);
                list.Add(p4);
                return list;
            }
            else // default to group 1 if set != 2
            {
                List<Purchase> list = new List<Purchase>();

                // ------------------ Group 1 Purchases ------------------
                // Purchase 1 (Group 1)
                Purchase p1 = new Purchase();
                p1.ReceivedInvoiceId = 2001;
                p1.Itemcode = 101;
                p1.PackSize = 10;
                p1.PackQuantity = 2;
                p1.FreePacks = 0;
                p1.ReceivedAsUnitQuantity = 1;
                p1.FreeUnits = 0;
                p1.DiscountAbsolute = 5;
                p1.CostPerPack = 100;
                p1.CostPerUnit = 12;
                // TotalUnits = (2 * 10) + 1 = 20 + 1 = 21.
                p1.TotalUnits = 21;
                // GrossTotal = (2 * 100) + (1 * 12) = 200 + 12 = 212.
                p1.GrossTotal = 212;
                p1.NetTotalPrice = 212 - p1.DiscountAbsolute; // 212 - 5 = 207.
                p1.VatAbsolute = 15;
                p1.TotalAmountDue = p1.NetTotalPrice + p1.VatAbsolute; // 207 + 15 = 222.
                p1.GrossCostPerUnit = 212.0 / 21.0;
                p1.IsVatADisallowedInputTax = false;
                p1.NetCostPerUnit = 207.0 / 21.0;
                p1.NetTotalCost = p1.NetTotalPrice;
                double markupPercentage = 20.0;
                p1.SellingPrice = p1.NetCostPerUnit * (1 + markupPercentage / 100.0);
                p1.GrossMarkupAbsolute = p1.SellingPrice - p1.NetCostPerUnit;
                p1.GrossMarkupPercentage = p1.NetCostPerUnit > 0 ? (p1.GrossMarkupAbsolute / p1.NetCostPerUnit) * 100.0 : 0.0;
                p1.VatPercentage = 15;
                p1.VatCategory = 1;
                p1.VatCategoryName = "Standard";
                p1.ProductName = "P1";
                p1.ManufacturerBatchId = "P1";
                p1.ManufacturingDate = DateTime.Now;
                p1.ExpiryDate = DateTime.Now;

                // Purchase 2 (Group 1)
                Purchase p2 = new Purchase();
                p2.ReceivedInvoiceId = 2001;
                p2.Itemcode = 102;
                p2.PackSize = 5;
                p2.PackQuantity = 4;
                p2.FreePacks = 1;
                p2.ReceivedAsUnitQuantity = 2;
                p2.FreeUnits = 1;
                p2.DiscountAbsolute = 8;
                p2.CostPerPack = 50;
                p2.CostPerUnit = 8;
                // TotalUnits = ((4+1)*5) + 2 + 1 = (5*5) + 3 = 25 + 3 = 28.
                p2.TotalUnits = 28;
                // GrossTotal = (4 * 50) + (2 * 8) = 200 + 16 = 216.
                p2.GrossTotal = 216;
                p2.NetTotalPrice = 216 - p2.DiscountAbsolute; // 216 - 8 = 208.
                p2.VatAbsolute = 10;
                p2.TotalAmountDue = p2.NetTotalPrice + p2.VatAbsolute; // 208 + 10 = 218.
                p2.GrossCostPerUnit = 216.0 / 28.0;
                p2.IsVatADisallowedInputTax = false;
                p2.NetCostPerUnit = 208.0 / 28.0;
                p2.NetTotalCost = p2.NetTotalPrice;
                markupPercentage = 15.0;
                p2.SellingPrice = p2.NetCostPerUnit * (1 + markupPercentage / 100.0);
                p2.GrossMarkupAbsolute = p2.SellingPrice - p2.NetCostPerUnit;
                p2.GrossMarkupPercentage = p2.NetCostPerUnit > 0 ? (p2.GrossMarkupAbsolute / p2.NetCostPerUnit) * 100.0 : 0.0;
                p2.VatPercentage = 10;
                p2.VatCategory = 2;
                p2.VatCategoryName = "Reduced";

                // Purchase 3 (Group 1)
                Purchase p3 = new Purchase();
                p3.ReceivedInvoiceId = 2001;
                p3.Itemcode = 103;
                p3.PackSize = 12;
                p3.PackQuantity = 3;
                p3.FreePacks = 0;
                p3.ReceivedAsUnitQuantity = 3;
                p3.FreeUnits = 0;
                p3.DiscountAbsolute = 10;
                p3.CostPerPack = 110;
                p3.CostPerUnit = 12;
                // TotalUnits = (3 * 12) + 3 = 36 + 3 = 39.
                p3.TotalUnits = 39;
                // GrossTotal = (3 * 110) + (3 * 12) = 330 + 36 = 366.
                p3.GrossTotal = 366;
                p3.NetTotalPrice = 366 - p3.DiscountAbsolute; // 366 - 10 = 356.
                p3.VatAbsolute = 20;
                p3.TotalAmountDue = p3.NetTotalPrice + p3.VatAbsolute; // 356 + 20 = 376.
                p3.GrossCostPerUnit = 366.0 / 39.0;
                p3.IsVatADisallowedInputTax = false;
                p3.NetCostPerUnit = 356.0 / 39.0;
                p3.NetTotalCost = p3.NetTotalPrice;
                markupPercentage = 18.0;
                p3.SellingPrice = p3.NetCostPerUnit * (1 + markupPercentage / 100.0);
                p3.GrossMarkupAbsolute = p3.SellingPrice - p3.NetCostPerUnit;
                p3.GrossMarkupPercentage = p3.NetCostPerUnit > 0 ? (p3.GrossMarkupAbsolute / p3.NetCostPerUnit) * 100.0 : 0.0;
                p3.VatPercentage = 12;
                p3.VatCategory = 1;
                p3.VatCategoryName = "Standard";

                // Purchase 4 (Group 1)
                Purchase p4 = new Purchase();
                p4.ReceivedInvoiceId = 2001;
                p4.Itemcode = 104;
                p4.PackSize = 8;
                p4.PackQuantity = 6;
                p4.FreePacks = 2;
                p4.ReceivedAsUnitQuantity = 0;
                p4.FreeUnits = 4;
                p4.DiscountAbsolute = 12;
                p4.CostPerPack = 40;
                p4.CostPerUnit = 5;
                // TotalUnits = ((6+2)*8) + 4 = (8*8) + 4 = 64 + 4 = 68.
                p4.TotalUnits = 68;
                // GrossTotal = (6 * 40) = 240.
                p4.GrossTotal = 240;
                p4.NetTotalPrice = 240 - p4.DiscountAbsolute; // 240 - 12 = 228.
                p4.VatAbsolute = 18;
                p4.TotalAmountDue = p4.NetTotalPrice + p4.VatAbsolute; // 228 + 18 = 246.
                p4.GrossCostPerUnit = 240.0 / 68.0;
                p4.IsVatADisallowedInputTax = false;
                p4.NetCostPerUnit = 228.0 / 68.0;
                p4.NetTotalCost = p4.NetTotalPrice;
                markupPercentage = 25.0;
                p4.SellingPrice = p4.NetCostPerUnit * (1 + markupPercentage / 100.0);
                p4.GrossMarkupAbsolute = p4.SellingPrice - p4.NetCostPerUnit;
                p4.GrossMarkupPercentage = p4.NetCostPerUnit > 0 ? (p4.GrossMarkupAbsolute / p4.NetCostPerUnit) * 100.0 : 0.0;
                p4.VatPercentage = 8;
                p4.VatCategory = 1;
                p4.VatCategoryName = "Standard";

                list.Add(p1);
                list.Add(p2);
                list.Add(p3);
                list.Add(p4);
                return list;
            }
        }

        /// <summary>
        /// Returns a sample valid ReceivedInvoice whose fields are consistent with the corresponding purchases.
        /// Two groups are defined; by default group 1 is returned if set is not 2.
        /// </summary>
        public static ReceivedInvoice GetSampleValidReceivedInvoice(int set = 1)
        {
            if (set == 2)
            {
                // ------------------ Group 2 Invoice ------------------
                ReceivedInvoice inv = new ReceivedInvoice();
                inv.ReceivedInvoiceNo = 3002;
                inv.IsPosted = false;
                inv.SupplierId = 502;
                inv.SupplierName = "Supplier Y";
                inv.Remarks = "Test invoice 2";
                inv.Reference = "INV-3002";
                // Invoice GrossTotal is the sum of the 4 group2 purchase GrossTotals:
                // 624 + 487 + 314 + 400 = 1825.
                inv.GrossTotal = 1825;
                inv.TransportCharges = 50;
                // EffectiveDiscount equals the sum of purchase-level DiscountAbsolute:
                // 10 + 6 + 8 + 15 = 39.
                inv.EffectiveDiscount = 39;
                // WholeInvoiceDiscount (provided on the invoice level) is 25.
                inv.WholeInvoiceDiscount = 25;
                // Total before discount = GrossTotal + TransportCharges = 1825 + 50 = 1875.
                // Overall discount = EffectiveDiscount + WholeInvoiceDiscount = 39 + 25 = 64.
                inv.DiscountPercentage = (64 / 1875.0) * 100.0;
                inv.IsSettled = false;
                inv.DefaultVatCategoryName = "Standard";
                inv.DefaultVatCategory = 1;
                inv.DefaultVatPercentage = 12;
                return inv;
            }
            else // default to Group 1
            {
                // ------------------ Group 1 Invoice ------------------
                ReceivedInvoice inv = new ReceivedInvoice();
                inv.ReceivedInvoiceNo = 3001;
                inv.IsPosted = false;
                inv.SupplierId = 501;
                inv.SupplierName = "Supplier X";
                inv.Remarks = "Test invoice 1";
                inv.Reference = "INV-3001";
                // Invoice GrossTotal is the sum of the 4 group1 purchase GrossTotals:
                // 212 + 216 + 366 + 240 = 1034.
                inv.GrossTotal = 1034;
                inv.TransportCharges = 30;
                // EffectiveDiscount = 5 + 8 + 10 + 12 = 35.
                inv.EffectiveDiscount = 35;
                // WholeInvoiceDiscount is set to 20.
                inv.WholeInvoiceDiscount = 20;
                // Total before discount = 1034 + 30 = 1064, overall discount = 35 + 20 = 55.
                inv.DiscountPercentage = (55 / 1064.0) * 100.0;
                inv.IsSettled = false;
                inv.DefaultVatCategoryName = "Standard";
                inv.DefaultVatCategory = 1;
                inv.DefaultVatPercentage = 10;
                return inv;
            }
        }
    }
}
