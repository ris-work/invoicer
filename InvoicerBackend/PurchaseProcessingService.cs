using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvoicerBackend
{
    // Result container for the simulation
    public class PurchaseProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ReceivedInvoice Header { get; set; }
        public List<Purchase> Items { get; set; }
        public List<JournalEntryResult> AccountingEntries { get; set; }
        public double GrandTotal { get; set; }
        public double TotalPaid { get; set; }
    }

    public static class PurchaseProcessingService
    {
        /// <summary>
        /// Runs the calculation engine and validation for a purchase invoice.
        /// </summary>
        public static PurchaseProcessResult ProcessPurchase(
            NewinvContext ctx,
            ReceivedInvoice header,
            List<Purchase> items)
        {
            // 1. Run Reactive Calculations per Item
            foreach (var item in items)
            {
                // A. Quantities
                item.TotalUnits = ((item.PackQuantity + item.FreePacks) * item.PackSize)
                                  + item.ReceivedAsUnitQuantity
                                  + item.FreeUnits;

                // B. Costs & Gross
                item.GrossTotal = (item.PackQuantity * item.CostPerPack)
                                  + (item.ReceivedAsUnitQuantity * item.CostPerUnit);

                // C. Discounts
                // Note: If Percentage is provided, Absolute takes precedence in our logic usually, 
                // but here we assume the UI sends us the calculated Absolute.
                // We re-calc % just to be sure or vice-versa. 
                // For backend, we trust the Absolute from UI but verify logic.
                if (item.GrossTotal > 0 && item.DiscountPercentage > 0 && item.DiscountAbsolute == 0)
                {
                    item.DiscountAbsolute = item.GrossTotal * (item.DiscountPercentage / 100.0);
                }

                // D. Net Price & VAT
                item.NetTotalPrice = item.GrossTotal - item.DiscountAbsolute;

                // VAT Logic
                if (item.VatPercentage > 0 && item.VatAbsolute == 0)
                {
                    item.VatAbsolute = item.NetTotalPrice * (item.VatPercentage / 100.0);
                }

                item.TotalAmountDue = item.NetTotalPrice + item.VatAbsolute;

                // E. Unit Costs
                item.GrossCostPerUnit = item.TotalUnits > 0 ? item.GrossTotal / item.TotalUnits : 0;

                // F. Net Cost Per Unit (Critical Logic)
                if (item.TotalUnits > 0)
                {
                    if (item.IsVatADisallowedInputTax)
                    {
                        // VAT is absorbed into cost
                        item.NetCostPerUnit = item.TotalAmountDue / item.TotalUnits;
                        item.NetTotalCost = item.TotalAmountDue;
                    }
                    else
                    {
                        item.NetCostPerUnit = item.NetTotalPrice / item.TotalUnits;
                        item.NetTotalCost = item.NetTotalPrice;
                    }
                }
                else
                {
                    item.NetCostPerUnit = 0;
                    item.NetTotalCost = 0;
                }

                // G. Markup
                if (item.SellingPrice > 0 && item.NetCostPerUnit > 0)
                {
                    item.GrossMarkupAbsolute = item.SellingPrice - item.NetCostPerUnit;
                    item.GrossMarkupPercentage = (item.GrossMarkupAbsolute / item.NetCostPerUnit) * 100.0;
                }
            }

            // 2. Aggregate Header
            items.CalculateInvoice(header);

            // 3. Validation
            var validation = header.ValidateInvoice(items);
            if (!validation.Valid)
            {
                return new PurchaseProcessResult
                {
                    Success = false,
                    Message = validation.Error,
                    Header = header,
                    Items = items
                };
            }

            return new PurchaseProcessResult
            {
                Success = true,
                Message = "OK",
                Header = header,
                Items = items
            };
        }
    }
}