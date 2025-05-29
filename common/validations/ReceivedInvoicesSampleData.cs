using System;
using System.Collections.Generic;
using RV.InvNew.Common; // Assumes Purchase, ReceivedInvoice and extension methods are defined here

namespace SampleDataGenerator
{
    public static class SampleDataGenerator
    {
        /// <summary>
        /// Returns a valid purchase list with 4 entries.
        /// </summary>
        /// <param name="variant">Variant 1 is default; passing 2 returns a second valid list.</param>
        /// <returns>Valid List of Purchase</returns>
        public static List<Purchase> GetSampleValidPurchases(int variant = 1)
        {
            var purchases = new List<Purchase>();
            // R1: Create 4 valid purchase items.
            for (int i = 1; i <= 4; i++)
            {
                purchases.Add(
                    new Purchase
                    {
                        ReceivedInvoiceId = variant,
                        Itemcode = 1000 + i,
                        PackSize = 10,
                        PackQuantity = 2,
                        ReceivedAsUnitQuantity = 20, // Unchanged since it is 'received as units'
                        FreePacks = 0,
                        FreeUnits = 0,
                        AddedDate = DateTimeOffset.Now,
                        ExpiryDate = DateTimeOffset.Now.AddMonths(6), // R3: ExpiryDate is after AddedDate
                        ManufacturingDate = DateTimeOffset.Now.AddMonths(-1),
                        ManufacturerBatchId = "Batch-" + i,
                        ProductName = "Product " + i,
                        DiscountPercentage = 0, // Not used—weighted discount will be computed
                        DiscountAbsolute = 5 * i, // Valid non-negative discount absolute
                        CostPerUnit = 10,
                        CostPerPack = 100,
                        GrossCostPerUnit = 11,
                        SellingPrice = 15,
                        VatPercentage = 15,
                        VatCategory = 1,
                        VatAbsolute = 2 * i, // Sample increasing VAT
                        VatCategoryName = "Standard VAT",
                        TotalUnits = 20,
                        NetTotalPrice = 200, // R2: Must be non-negative
                        TotalAmountDue = 200,
                        GrossTotal = 210, // R2: Valid gross total
                        NetTotalCost = 190,
                        GrossMarkupPercentage = 10,
                        GrossMarkupAbsolute = 20,
                        IsVatADisallowedInputTax = false,
                        NetCostPerUnit = 9,
                        LineNumber = i,
                    }
                );
            }
            return purchases;
        }

        /// <summary>
        /// Returns a purchase list where one entry is invalid.
        /// </summary>
        /// <remarks>
        /// R3: For example, one purchase has ExpiryDate earlier than AddedDate.
        /// </remarks>
        public static List<Purchase> GetSampleInvalidPurchases()
        {
            var purchases = GetSampleValidPurchases();
            if (purchases.Count >= 2)
            {
                // Invalidate the second purchase (violates Rule P3)
                purchases[1].ExpiryDate = purchases[1].AddedDate.AddDays(-1);
            }
            return purchases;
        }

        /// <summary>
        /// Returns a valid invoice header aggregated from a valid purchase list.
        /// </summary>
        /// <param name="variant">1 is default; 2 returns a second valid variant.</param>
        public static ReceivedInvoice GetSampleValidInvoice(int variant = 1)
        {
            var purchases = GetSampleValidPurchases(variant);
            var invoice = new ReceivedInvoice
            {
                ReceivedInvoiceNo = variant,
                IsPosted = true,
                SupplierId = variant == 1 ? 1 : 2,
                SupplierName = variant == 1 ? "Supplier A" : "Supplier B",
                Remarks = "Valid invoice header " + variant,
                Reference = "INV-" + variant,
                TransportCharges = 20,
                DefaultVatPercentage = 15,
                DefaultVatCategory = 1,
                WholeInvoiceDiscountAbsolute = 10,
                WholeInvoiceDiscountPercentage = 5,
                IsSettled = false,
                DefaultVatCategoryName = "Standard VAT",
            };
            // R1, R2: Compute aggregates (GrossTotal, Discount totals, VAT totals) from purchases.
            invoice = purchases.CalculateInvoice(invoice);
            return invoice;
        }

        /// <summary>
        /// Returns an invoice header with invalid header data (only header is invalid, purchases remain valid).
        /// </summary>
        /// <param name="variant">Use variant 1 for this sample.</param>
        public static ReceivedInvoice GetSampleInvalidInvoice(int variant = 1)
        {
            var purchases = GetSampleValidPurchases(variant);
            var invoice = new ReceivedInvoice
            {
                ReceivedInvoiceNo = 100 + variant,
                IsPosted = false,
                SupplierId = 0, // R: Invalid because SupplierId must be > 0
                SupplierName = "", // R: Invalid because SupplierName is required
                Remarks = "Invalid invoice header variant 1",
                Reference = "INV-INVALID-" + variant,
                TransportCharges = 20,
                DefaultVatPercentage = 15,
                DefaultVatCategory = 1,
                WholeInvoiceDiscountAbsolute = 10,
                WholeInvoiceDiscountPercentage = 5,
                IsSettled = false,
                DefaultVatCategoryName = "Standard VAT",
            };
            invoice = purchases.CalculateInvoice(invoice);
            return invoice;
        }

        /// <summary>
        /// Returns an invoice header that is invalid and is aggregated with a purchase list that contains an invalid entry.
        /// </summary>
        /// <param name="variant">Use variant 2 in this sample.</param>
        public static ReceivedInvoice GetSampleInvalidInvoiceWithInvalidPurchase(int variant = 2)
        {
            var purchases = GetSampleInvalidPurchases(); // Contains one invalid purchase entry (see above)
            var invoice = new ReceivedInvoice
            {
                ReceivedInvoiceNo = 200 + variant,
                IsPosted = false,
                SupplierId = 0, // R: Invalid header field
                SupplierName = "", // R: Invalid header field
                Remarks = "Invalid invoice header with invalid purchase entry",
                Reference = "INV-INVALID-2",
                TransportCharges = 20,
                DefaultVatPercentage = 15,
                DefaultVatCategory = 1,
                WholeInvoiceDiscountAbsolute = 10,
                WholeInvoiceDiscountPercentage = 5,
                IsSettled = false,
                DefaultVatCategoryName = "Standard VAT",
            };
            invoice = purchases.CalculateInvoice(invoice);
            return invoice;
        }
    }
}
