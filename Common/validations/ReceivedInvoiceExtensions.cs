using System;
using System.Collections.Generic;
using System.Linq;

namespace RV.InvNew.Common
{
    public static class InvoiceExtensions
    {
        /// <summary>
        /// Aggregates header values from a list of Purchase items. Only modifies the header.
        /// </summary>
        /// <param name="purchases">List of Purchase items.</param>
        /// <param name="invoice">Invoice header to update.</param>
        /// <returns>The updated invoice header.</returns>
        /// <remarks>
        /// Rule CALC-1: GrossTotal = Sum(Purchase.GrossTotal).<br/>
        /// Rule CALC-2: Discount totals are computed as: <br/>
        /// &nbsp;&nbsp;- EffectiveDiscountAbsoluteFromEnteredItems = Sum(Purchase.DiscountAbsolute).<br/>
        /// &nbsp;&nbsp;- EffectiveDiscountPercentageFromEnteredItems is computed as (total discountAbs / GrossTotal)*100.<br/>
        /// Rule CALC-3: Total effective discount equals the user-entered value plus the computed item discount.<br/>
        /// Rule CALC-4 (VAT): VatTotal = Sum(Purchase.VatAbsolute) and <br/>
        /// &nbsp;&nbsp;EffectiveVatPercentage = (VatTotal / (GrossTotal - total discountAbs))*100.
        /// </remarks>
        public static ReceivedInvoice CalculateInvoice(this List<Purchase> purchases, ReceivedInvoice invoice)
        {
            if (purchases == null)
                throw new ArgumentNullException(nameof(purchases));
            if (invoice == null)
                throw new ArgumentNullException(nameof(invoice));

            // CALC-1: Aggregated GrossTotal.
            invoice.GrossTotal = purchases.Sum(p => p.GrossTotal);

            // CALC-2: Discount calculations.
            double totalDiscountAbs = purchases.Sum(p => p.DiscountAbsolute);
            invoice.EffectiveDiscountAbsoluteFromEnteredItems = totalDiscountAbs;
            invoice.EffectiveDiscountPercentageFromEnteredItems = invoice.GrossTotal > 0
                ? (totalDiscountAbs / invoice.GrossTotal) * 100
                : 0;

            invoice.EffectiveDiscountAbsoluteTotal = invoice.WholeInvoiceDiscountAbsolute + invoice.EffectiveDiscountAbsoluteFromEnteredItems;
            invoice.EffectiveDiscountPercentageTotal = invoice.WholeInvoiceDiscountPercentage + invoice.EffectiveDiscountPercentageFromEnteredItems;

            // CALC-4: VAT calculations.
            double totalVat = purchases.Sum(p => p.VatAbsolute);
            invoice.VatTotal = totalVat;
            double taxableBase = invoice.GrossTotal - totalDiscountAbs;
            invoice.EffectiveVatPercentage = taxableBase > 0
                ? (totalVat / taxableBase) * 100
                : 0;

            return invoice;
        }

        /// <summary>
        /// Validates the Purchase list.
        /// </summary>
        /// <param name="purchases">The list of Purchase items.</param>
        /// <returns>A tuple (bool Valid, string Error) indicating overall validity.</returns>
        /// <remarks>
        /// Rule P1: Purchase list must not be empty.<br/>
        /// Rule P2: Each Purchase must have non-negative GrossTotal, NetTotalPrice, and VatAbsolute.<br/>
        /// Rule P3: ExpiryDate must not be before AddedDate.
        /// </remarks>
        public static (bool Valid, string Error) Validate(this List<Purchase> purchases)
        {
            if (purchases == null || purchases.Count == 0)
                return (false, "Rule P1: Purchase list is empty.");

            foreach (var p in purchases)
            {
                if (p.GrossTotal < 0)
                    return (false, $"Rule P2: Negative GrossTotal in Purchase (Line {p.LineNumber}).");
                if (p.NetTotalPrice < 0)
                    return (false, $"Rule P2: Negative NetTotalPrice in Purchase (Line {p.LineNumber}).");
                if (p.ExpiryDate < p.AddedDate)
                    return (false, $"Rule P3: ExpiryDate precedes AddedDate in Purchase (Line {p.LineNumber}).");
                if (p.VatAbsolute < 0)
                    return (false, $"Rule P2: Negative VatAbsolute in Purchase (Line {p.LineNumber}).");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Validates the invoice header.
        /// </summary>
        /// <param name="invoice">The invoice header.</param>
        /// <returns>A tuple (bool Valid, string Error) indicating overall validity.</returns>
        /// <remarks>
        /// Rule I1: SupplierId must be positive and SupplierName is required.<br/>
        /// Rule I2: GrossTotal must be non-negative.<br/>
        /// Rule I3: WholeInvoiceDiscount values must be non-negative.<br/>
        /// Rule I6: VatTotal must be non-negative.
        /// </remarks>
        public static (bool Valid, string Error) Validate(this ReceivedInvoice invoice)
        {
            if (invoice == null)
                return (false, "Rule I1: Invoice header is null.");
            if (invoice.SupplierId <= 0)
                return (false, "Rule I1: SupplierId must be positive.");
            if (string.IsNullOrWhiteSpace(invoice.SupplierName))
                return (false, "Rule I1: SupplierName is required.");
            if (invoice.GrossTotal < 0)
                return (false, "Rule I2: GrossTotal cannot be negative.");
            if (invoice.WholeInvoiceDiscountAbsolute < 0 || invoice.WholeInvoiceDiscountPercentage < 0)
                return (false, "Rule I3: WholeInvoiceDiscount values must be non-negative.");
            if (invoice.VatTotal < 0)
                return (false, "Rule I6: VatTotal cannot be negative.");

            return (true, string.Empty);
        }

        /// <summary>
        /// Aggregates validations for both the Purchase list and invoice header.
        /// The matching tolerance for computed fields is configurable via matchThreshold.
        /// </summary>
        /// <param name="invoice">The invoice header.</param>
        /// <param name="purchases">The list of Purchase items.</param>
        /// <param name="matchThreshold">Threshold for numeric mismatches (default: 0.01).</param>
        /// <returns>A tuple (bool Valid, string Error) indicating overall validity.</returns>
        /// <remarks>
        /// Rule I4: Validate computed aggregates against the header: GrossTotal, effective discount totals, and VAT totals.
        /// </remarks>
        public static (bool Valid, string Error) ValidateInvoice(this ReceivedInvoice invoice, List<Purchase> purchases, double matchThreshold = 0.01)
        {
            var purchaseValid = purchases.Validate();
            if (!purchaseValid.Valid)
                return purchaseValid;

            var invoiceValid = invoice.Validate();
            if (!invoiceValid.Valid)
                return invoiceValid;

            // GrossTotal check.
            double computedGross = purchases.Sum(p => p.GrossTotal);
            if (Math.Abs(invoice.GrossTotal - computedGross) > matchThreshold)
                return (false, "Rule I4: GrossTotal mismatch.");

            // Discount totals.
            double totalDiscountAbs = purchases.Sum(p => p.DiscountAbsolute);
            double computedDiscountAbs = invoice.WholeInvoiceDiscountAbsolute + totalDiscountAbs;
            if (Math.Abs(invoice.EffectiveDiscountAbsoluteTotal - computedDiscountAbs) > matchThreshold)
                return (false, "Rule I4: EffectiveDiscountAbsoluteTotal mismatch.");

            double computedDiscountPct = invoice.WholeInvoiceDiscountPercentage +
                (computedGross > 0 ? (totalDiscountAbs / computedGross) * 100 : 0);
            if (Math.Abs(invoice.EffectiveDiscountPercentageTotal - computedDiscountPct) > matchThreshold)
                return (false, "Rule I4: EffectiveDiscountPercentageTotal mismatch.");

            // VAT totals.
            double computedVatTotal = purchases.Sum(p => p.VatAbsolute);
            if (Math.Abs(invoice.VatTotal - computedVatTotal) > matchThreshold)
                return (false, "Rule I4: VatTotal mismatch.");

            double taxableBase = computedGross - totalDiscountAbs;
            double computedEffectiveVat = taxableBase > 0
                ? (computedVatTotal / taxableBase) * 100
                : 0;
            if (Math.Abs(invoice.EffectiveVatPercentage - computedEffectiveVat) > matchThreshold)
                return (false, "Rule I4: EffectiveVatPercentage mismatch.");

            return (true, string.Empty);
        }
    }
}
