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
        public double TotalExpenses { get; set; } // NEW
        public double TotalPaid { get; set; } // NEW
        public double Balance { get; set; } // NEW
        public List<JournalEntryResult> AccountingEntries { get; set; } // For preview if needed
    }

    public static class PurchaseProcessingService
    {
        public static PurchaseProcessResult ProcessPurchase(
            NewinvContext ctx,
            ReceivedInvoice header,
            List<Purchase> items,
            List<PurchaseExpense> expenses, // NEW
            List<PaymentEntry> payments
        ) // NEW)
        {
            foreach (var item in items)
            {
                // 1. Total Units (Paid + Free)
                item.TotalUnits = ((item.PackQuantity + item.FreePacks) * item.PackSize)
                                  + item.ReceivedAsUnitQuantity
                                  + item.FreeUnits;

                // 2. Gross Total (Sum of independent costs)
                // FIX: Do not derive CostPerUnit from CostPerPack or vice versa.
                item.GrossTotal = (item.PackQuantity * item.CostPerPack)
                                  + (item.ReceivedAsUnitQuantity * item.CostPerUnit);

                // 3. Discounts
                item.NetTotalPrice = item.GrossTotal - item.DiscountAbsolute;

                // 4. VAT
                item.TotalAmountDue = item.NetTotalPrice + item.VatAbsolute;

                // 5. Net Total Cost (Accounting for Disallowed VAT)
                item.NetTotalCost = item.IsVatADisallowedInputTax ? item.TotalAmountDue : item.NetTotalPrice;

                // 6. Effective Unit Cost (NetCostPerUnit)
                // FIX: This is the key metric for management. 
                // It spreads the NetTotalCost over ALL units (Paid + Free).
                item.GrossCostPerUnit = item.TotalUnits > 0 ? item.GrossTotal / item.TotalUnits : 0;
                item.NetCostPerUnit = item.TotalUnits > 0 ? item.NetTotalCost / item.TotalUnits : 0;

                // 7. Markup
                if (item.SellingPrice > 0 && item.NetCostPerUnit > 0)
                {
                    item.GrossMarkupAbsolute = item.SellingPrice - item.NetCostPerUnit;
                    item.GrossMarkupPercentage = (item.GrossMarkupAbsolute / item.NetCostPerUnit) * 100.0;
                }
            }

            // Header Aggregation
            items.CalculateInvoice(header);

            // 3. Process Expenses
            double totalExpenses = 0;
            if (expenses != null)
            {
                totalExpenses = expenses.Sum(e => e.Amount);
                // Add to Invoice Header (using TransportCharges as the bucket for now)
                header.TransportCharges = totalExpenses;
            }

            // 4. Adjust Grand Total
            // TotalAmountDue was calculated from items. Add Expenses.
            // Note: Header.TotalAmountDue from CalculateInvoice might just be items.
            // We rely on the object being updated. Let's re-calc explicitly to be safe.
            double itemsTotal = header.TotalAmountDue;
            double grandTotal = itemsTotal + totalExpenses;

            // Update header for consistency (TotalAmountDue includes transport now)
            header.TotalAmountDue = grandTotal;

            // 5. Process Payments
            double totalPaid = 0;
            if (payments != null)
            {
                totalPaid = payments.Sum(p => p.Amount);
            }

            double balance = grandTotal - totalPaid;


            // Validation
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