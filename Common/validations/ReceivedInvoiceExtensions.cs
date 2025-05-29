using System;
using System.Collections.Generic;
using System.Linq;

namespace RV.InvNew.Common
{
    public static class InvoiceExtensions
    {
        /// <summary>
        /// Recalculates the computed fields for ReceivedInvoice based on the list of purchases.
        /// Updates GrossTotal, discount totals, VatTotal, EffectiveVatPercentage, TotalAmountDue and LastSavedAt.
        /// </summary>
        /// <param name="purchases">List of Purchase entries.</param>
        /// <param name="invoice">The invoice header to update.</param>
        /// <returns>The updated invoice header.</returns>
        public static ReceivedInvoice CalculateInvoice(this List<Purchase> purchases, ReceivedInvoice invoice)
        {
            if (purchases == null)
                throw new ArgumentNullException(nameof(purchases));
            if (invoice == null)
                throw new ArgumentNullException(nameof(invoice));

            // Calculate aggregated values from the purchase list.
            double computedGrossTotal = purchases.Sum(p => p.GrossTotal);
            double computedTotalDiscountAbsolute = purchases.Sum(p => p.DiscountAbsolute);
            double weightedDiscountPctFromItems = computedGrossTotal > 0
                ? (computedTotalDiscountAbsolute / computedGrossTotal) * 100
                : 0;
            double computedVatTotal = purchases.Sum(p => p.VatAbsolute);
            double computedTotalAmountDue = purchases.Sum(p => p.TotalAmountDue);

            // Update computed fields.
            invoice.GrossTotal = computedGrossTotal;
            invoice.EffectiveDiscountAbsoluteFromEnteredItems = computedTotalDiscountAbsolute;
            invoice.EffectiveDiscountPercentageFromEnteredItems = weightedDiscountPctFromItems;
            invoice.EffectiveDiscountAbsoluteTotal = invoice.WholeInvoiceDiscountAbsolute + computedTotalDiscountAbsolute;
            invoice.EffectiveDiscountPercentageTotal = invoice.WholeInvoiceDiscountPercentage + weightedDiscountPctFromItems;
            invoice.VatTotal = computedVatTotal;

            double taxableBase = computedGrossTotal - computedTotalDiscountAbsolute;
            invoice.EffectiveVatPercentage = taxableBase > 0
                ? (computedVatTotal / taxableBase) * 100
                : 0;

            invoice.TotalAmountDue = computedTotalAmountDue;

            // Set LastSavedAt to now.
            invoice.LastSavedAt = DateTime.Now;

            return invoice;
        }

        /// <summary>
        /// Validates the global invoice header.
        /// Checks supplier data, non-negative amounts in computed fields, and consistency of date fields.
        /// </summary>
        /// <param name="invoice">The invoice header.</param>
        /// <returns>A tuple (bool Valid, string Error) indicating overall validity.</returns>
        public static (bool Valid, string Error) Validate(this ReceivedInvoice invoice)
        {
            if (invoice == null)
                return (false, "Invoice is null.");
            if (invoice.SupplierId <= 0)
                return (false, "SupplierId must be positive.");
            if (string.IsNullOrWhiteSpace(invoice.SupplierName))
                return (false, "SupplierName is required.");
            if (invoice.GrossTotal < 0)
                return (false, "GrossTotal cannot be negative.");
            if (invoice.TransportCharges < 0)
                return (false, "TransportCharges cannot be negative.");
            if (invoice.WholeInvoiceDiscountAbsolute < 0 || invoice.WholeInvoiceDiscountPercentage < 0)
                return (false, "WholeInvoiceDiscount values cannot be negative.");
            if (invoice.EffectiveDiscountAbsoluteTotal < 0 || invoice.EffectiveDiscountPercentageTotal < 0)
                return (false, "EffectiveDiscountTotal values cannot be negative.");
            if (invoice.VatTotal < 0)
                return (false, "VatTotal cannot be negative.");
            if (invoice.EffectiveVatPercentage < 0)
                return (false, "EffectiveVatPercentage cannot be negative.");
            if (invoice.IsPosted && !invoice.PostedAt.HasValue)
                return (false, "Invoice is marked as posted but PostedAt is null.");
            if (invoice.LastSavedAt < invoice.CreatedAt)
                return (false, "LastSavedAt cannot be earlier than CreatedAt.");
            if (invoice.TotalAmountDue < 0)
                return (false, "TotalAmountDue cannot be negative.");
            return (true, string.Empty);
        }

        /// <summary>
        /// Aggregates and cross-validates invoice header fields against values computed from the purchases.
        /// Uses a configurable numeric tolerance (matchThreshold) for floating-point comparisons.
        /// </summary>
        /// <param name="invoice">The invoice header.</param>
        /// <param name="purchases">The list of Purchase entries.</param>
        /// <param name="matchThreshold">Allowed tolerance (default 0.01).</param>
        /// <returns>A tuple (bool Valid, string Error) indicating overall validity.</returns>
        public static (bool Valid, string Error) ValidateInvoice(this ReceivedInvoice invoice, List<Purchase> purchases, double matchThreshold = 0.01)
        {
            // Assume that List<Purchase> has its own Validate() extension.
            //var purchaseValidation = purchases.V();
            //if (!purchaseValidation.Valid)
                //return purchaseValidation;

            var invoiceValidation = invoice.Validate();
            if (!invoiceValidation.Valid)
                return invoiceValidation;

            double computedGrossTotal = purchases.Sum(p => p.GrossTotal);
            if (Math.Abs(invoice.GrossTotal - computedGrossTotal) > matchThreshold)
                return (false, "GrossTotal mismatch.");

            double computedDiscountAbs = purchases.Sum(p => p.DiscountAbsolute);
            if (Math.Abs(invoice.EffectiveDiscountAbsoluteFromEnteredItems - computedDiscountAbs) > matchThreshold)
                return (false, "EffectiveDiscountAbsoluteFromEnteredItems mismatch.");

            double computedDiscountPct = computedGrossTotal > 0 ? (computedDiscountAbs / computedGrossTotal) * 100 : 0;
            if (Math.Abs(invoice.EffectiveDiscountPercentageFromEnteredItems - computedDiscountPct) > matchThreshold)
                return (false, "EffectiveDiscountPercentageFromEnteredItems mismatch.");

            double computedEffectiveDiscountAbsoluteTotal = invoice.WholeInvoiceDiscountAbsolute + computedDiscountAbs;
            if (Math.Abs(invoice.EffectiveDiscountAbsoluteTotal - computedEffectiveDiscountAbsoluteTotal) > matchThreshold)
                return (false, "EffectiveDiscountAbsoluteTotal mismatch.");

            double computedEffectiveDiscountPercentageTotal = invoice.WholeInvoiceDiscountPercentage + computedDiscountPct;
            if (Math.Abs(invoice.EffectiveDiscountPercentageTotal - computedEffectiveDiscountPercentageTotal) > matchThreshold)
                return (false, "EffectiveDiscountPercentageTotal mismatch.");

            double computedVatTotal = purchases.Sum(p => p.VatAbsolute);
            if (Math.Abs(invoice.VatTotal - computedVatTotal) > matchThreshold)
                return (false, "VatTotal mismatch.");

            double taxableBase = computedGrossTotal - computedDiscountAbs;
            double computedEffectiveVatPercentage = taxableBase > 0 ? (computedVatTotal / taxableBase) * 100 : 0;
            if (Math.Abs(invoice.EffectiveVatPercentage - computedEffectiveVatPercentage) > matchThreshold)
                return (false, "EffectiveVatPercentage mismatch.");

            double computedTotalAmountDue = purchases.Sum(p => p.TotalAmountDue);
            if (Math.Abs(invoice.TotalAmountDue - computedTotalAmountDue) > matchThreshold)
                return (false, "TotalAmountDue mismatch.");

            return (true, string.Empty);
        }
    }
}
