using RV.InvNew.Common;
using System;
using System.Collections.Generic;
using System.Linq;

public static class ReceivedInvoiceExtensions
{
    /// <summary>
    /// Returns true if the absolute difference between two double values
    /// is within the specified tolerance.
    /// </summary>
    /*public static bool AreApproximatelyEqual(this double value1, double value2, double tolerance = 0.001)
    {
        return Math.Abs(value1 - value2) <= tolerance;
    }*/

    //-------------------------------------------------------------------------
    // Basic field validations (supplier info and nonnegative monetary values)
    //-------------------------------------------------------------------------
    public static (bool Valid, string Error) ValidateBasicFields(this ReceivedInvoice invoice)
    {
        var errors = new List<string>();

        // Basic supplier and text checks.
        if (invoice.SupplierId <= 0)
            errors.Add("Rule R0.1: SupplierId must be positive.");
        if (string.IsNullOrWhiteSpace(invoice.SupplierName))
            errors.Add("Rule R0.2: SupplierName is required.");
        if (string.IsNullOrWhiteSpace(invoice.Remarks))
            errors.Add("Rule R0.3: Remarks are required.");
        if (string.IsNullOrWhiteSpace(invoice.Reference))
            errors.Add("Rule R0.4: Reference is required.");

        // Nonnegative monetary fields.
        if (invoice.GrossTotal < 0)
            errors.Add("Rule R1: GrossTotal cannot be negative.");
        if (invoice.TransportCharges < 0)
            errors.Add("Rule R2: TransportCharges cannot be negative.");
        if (invoice.EffectiveDiscount < 0)
            errors.Add("Rule R3: EffectiveDiscount cannot be negative.");
        if (invoice.WholeInvoiceDiscount < 0)
            errors.Add("Rule R4: WholeInvoiceDiscount cannot be negative.");
        if (invoice.DiscountPercentage < 0)
            errors.Add("Rule R5: DiscountPercentage cannot be negative.");

        return errors.Any()
            ? (false, string.Join("; ", errors))
            : (true, "Basic invoice fields are valid.");
    }

    //-------------------------------------------------------------------------
    // Totals validation: check that header totals agree with the Purchase list.
    //-------------------------------------------------------------------------
    public static (bool Valid, string Error) ValidateTotals(this ReceivedInvoice invoice, List<Purchase> purchases)
    {
        // Rule T1: The invoice GrossTotal must equal the sum of Purchase.GrossTotal.
        double computedPurchaseGrossTotal = purchases.Sum(p => p.GrossTotal);
        if (!invoice.GrossTotal.AreApproximatelyEqual(computedPurchaseGrossTotal))
        {
            return (false, $"Rule T1: Invoice GrossTotal ({invoice.GrossTotal}) does not equal sum of purchase GrossTotal ({computedPurchaseGrossTotal}).");
        }

        // Other totals such as TransportCharges come directly from the header.

        return (true, "Totals validation passed.");
    }

    //-------------------------------------------------------------------------
    // Discount validations (applied to the entire invoice)
    //-------------------------------------------------------------------------
    public static (bool Valid, string Error) ValidateDiscounts(this ReceivedInvoice invoice, List<Purchase> purchases)
    {
        var errors = new List<string>();

        // Rule WI1: EffectiveDiscount must equal the sum of purchase-level discounts.
        // (Assuming each Purchase has a DiscountAbsolute field representing its itemwise discount.)
        double computedEffectiveDiscount = purchases.Sum(p => p.DiscountAbsolute);
        if (!invoice.EffectiveDiscount.AreApproximatelyEqual(computedEffectiveDiscount))
        {
            errors.Add($"Rule WI1: EffectiveDiscount on invoice ({invoice.EffectiveDiscount}) does not equal sum of purchase discounts ({computedEffectiveDiscount}).");
        }

        // Total before any overall discount.
        double totalBeforeDiscount = invoice.GrossTotal + invoice.TransportCharges;

        // Rule WI2: WholeInvoiceDiscount must not exceed (GrossTotal + TransportCharges – EffectiveDiscount).
        if (invoice.WholeInvoiceDiscount > (totalBeforeDiscount - invoice.EffectiveDiscount))
        {
            errors.Add($"Rule WI2: WholeInvoiceDiscount ({invoice.WholeInvoiceDiscount}) exceeds allowable amount ({totalBeforeDiscount - invoice.EffectiveDiscount}).");
        }

        // Rule WI3: DiscountPercentage must equal ((EffectiveDiscount + WholeInvoiceDiscount) / (GrossTotal + TransportCharges)) * 100.
        double overallDiscount = invoice.EffectiveDiscount + invoice.WholeInvoiceDiscount;
        double expectedDiscountPercentage = (totalBeforeDiscount > 1e-6)
                ? (overallDiscount / totalBeforeDiscount) * 100.0
                : 0.0;
        if (!invoice.DiscountPercentage.AreApproximatelyEqual(expectedDiscountPercentage))
        {
            errors.Add($"Rule WI3: DiscountPercentage ({invoice.DiscountPercentage}) does not match expected value ({expectedDiscountPercentage}).");
        }

        return errors.Any()
            ? (false, string.Join("; ", errors))
            : (true, "Discount fields are valid.");
    }

    //-------------------------------------------------------------------------
    // Aggregation methods
    //-------------------------------------------------------------------------
    /// <summary>
    /// Runs all individual validations and returns an array of (bool, string) tuples.
    /// </summary>
    public static (bool ValidationStatus, string errorDescription)[] ValidateAll(this ReceivedInvoice invoice, List<Purchase> purchases)
    {
        return new (bool, string)[]
        {
            invoice.ValidateBasicFields(),
            invoice.ValidateTotals(purchases),
            invoice.ValidateDiscounts(purchases)
        };
    }

    /// <summary>
    /// Aggregates all validations into one overall result.
    /// </summary>
    public static (bool Valid, string Error) Validate(this ReceivedInvoice invoice, List<Purchase> purchases)
    {
        var validations = invoice.ValidateAll(purchases);
        bool overallValid = validations.All(v => v.ValidationStatus);
        string errorDescription = string.Join("; ", validations.Where(v => !v.ValidationStatus)
                                                                 .Select(v => v.errorDescription));
        return (overallValid, string.IsNullOrWhiteSpace(errorDescription)
            ? "All validations passed."
            : errorDescription);
    }
}
