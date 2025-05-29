using System;
using System.Linq;
using RV.InvNew.Common;

public static class PurchaseExtensions
{
    /// <summary>
    /// Returns true if the absolute difference between two double values is
    /// within the given tolerance.
    /// </summary>
    public static bool AreApproximatelyEqual(
        this double value1,
        double value2,
        double tolerance = 0.001
    )
    {
        return Math.Abs(value1 - value2) <= tolerance;
    }

    // Rule 1: PackSize must be greater than zero.
    public static (bool Valid, string Error) ValidatePackSize(this Purchase purchase)
    {
        if (purchase.PackSize <= 0)
            return (false, "Rule 1: PackSize must be greater than zero.");
        return (true, "Rule 1: PackSize is valid.");
    }

    // Rule 2: PackQuantity must be greater than zero.
    public static (bool Valid, string Error) ValidatePackQuantity(this Purchase purchase)
    {
        if (purchase.PackQuantity <= 0)
            return (false, "Rule 2: PackQuantity must be greater than zero.");
        return (true, "Rule 2: PackQuantity is valid.");
    }

    // Rule 3: ReceivedAsUnitQuantity must be non-negative.
    public static (bool Valid, string Error) ValidateReceivedAsUnitQuantity(this Purchase purchase)
    {
        if (purchase.ReceivedAsUnitQuantity < 0)
            return (false, "Rule 3: ReceivedAsUnitQuantity cannot be negative.");
        return (true, "Rule 3: ReceivedAsUnitQuantity is valid.");
    }

    // Rule 4: FreeUnits must be non-negative.
    public static (bool Valid, string Error) ValidateFreeUnits(this Purchase purchase)
    {
        if (purchase.FreeUnits < 0)
            return (false, "Rule 4: FreeUnits cannot be negative.");
        return (true, "Rule 4: FreeUnits is valid.");
    }

    // Rule 5: CostPerUnit and CostPerPack must be non-negative.
    public static (bool Valid, string Error) ValidateCosts(this Purchase purchase)
    {
        if (purchase.CostPerUnit < 0)
            return (false, "Rule 5: CostPerUnit cannot be negative.");
        if (purchase.CostPerPack < 0)
            return (false, "Rule 5: CostPerPack cannot be negative.");
        return (true, "Rule 5: Costs are valid.");
    }

    // Rule 6: DiscountAbsolute must be non-negative and not exceed computed gross total.
    public static (bool Valid, string Error) ValidateDiscount(this Purchase purchase)
    {
        if (purchase.DiscountAbsolute < 0)
            return (false, "Rule 6: DiscountAbsolute cannot be negative.");

        double computedGrossTotal =
            (purchase.PackQuantity * purchase.CostPerPack)
            + (purchase.ReceivedAsUnitQuantity * purchase.CostPerUnit);
        if (purchase.DiscountAbsolute > computedGrossTotal)
            return (
                false,
                $"Rule 6: DiscountAbsolute ({purchase.DiscountAbsolute}) cannot exceed computed gross total ({computedGrossTotal})."
            );

        return (true, "Rule 6: Discount is valid.");
    }

    // Rule 7: VAT values must be non-negative.
    public static (bool Valid, string Error) ValidateVAT(this Purchase purchase)
    {
        if (purchase.VatAbsolute < 0)
            return (false, "Rule 7: VatAbsolute cannot be negative.");
        if (purchase.VatPercentage < 0)
            return (false, "Rule 7: VatPercentage cannot be negative.");

        return (true, "Rule 7: VAT values are valid.");
    }

    // Validate computed summation fields.
    public static (bool Valid, string Error) ValidateSummationFields(this Purchase purchase)
    {
        // Rule 8: TotalUnits must match:
        double computedTotalUnits =
            ((purchase.PackQuantity + purchase.FreePacks) * purchase.PackSize)
            + purchase.ReceivedAsUnitQuantity
            + purchase.FreeUnits;
        if (!purchase.TotalUnits.AreApproximatelyEqual(computedTotalUnits))
            return (
                false,
                $"Rule 8: TotalUnits ({purchase.TotalUnits}) does not match computed total units ({computedTotalUnits})."
            );

        // Rule 9: GrossTotal must match:
        double computedGrossTotal =
            (purchase.PackQuantity * purchase.CostPerPack)
            + (purchase.ReceivedAsUnitQuantity * purchase.CostPerUnit);
        if (!purchase.GrossTotal.AreApproximatelyEqual(computedGrossTotal))
            return (
                false,
                $"Rule 9: GrossTotal ({purchase.GrossTotal}) does not match computed gross total ({computedGrossTotal})."
            );

        // Rule 10: NetTotalPrice must equal GrossTotal - DiscountAbsolute.
        double computedNetTotalPrice = computedGrossTotal - purchase.DiscountAbsolute;
        if (!purchase.NetTotalPrice.AreApproximatelyEqual(computedNetTotalPrice))
            return (
                false,
                $"Rule 10: NetTotalPrice ({purchase.NetTotalPrice}) does not match computed net total price ({computedNetTotalPrice})."
            );

        // Rule 11: TotalAmountDue must equal NetTotalPrice + VatAbsolute.
        double computedTotalAmountDue = computedNetTotalPrice + purchase.VatAbsolute;
        if (!purchase.TotalAmountDue.AreApproximatelyEqual(computedTotalAmountDue))
            return (
                false,
                $"Rule 11: TotalAmountDue ({purchase.TotalAmountDue}) does not match computed total amount due ({computedTotalAmountDue})."
            );

        // Rule 12: GrossCostPerUnit should equal GrossTotal / TotalUnits (if TotalUnits > 0).
        double computedGrossCostPerUnit =
            computedTotalUnits > 0 ? computedGrossTotal / computedTotalUnits : 0.0;
        if (!purchase.GrossCostPerUnit.AreApproximatelyEqual(computedGrossCostPerUnit))
            return (
                false,
                $"Rule 12: GrossCostPerUnit ({purchase.GrossCostPerUnit}) does not match computed gross cost per unit ({computedGrossCostPerUnit})."
            );

        // Rule 13: NetCostPerUnit should equal:
        double computedNetCostPerUnit =
            computedTotalUnits > 0
                ? (
                    purchase.IsVatADisallowedInputTax
                        ? computedTotalAmountDue / computedTotalUnits
                        : computedNetTotalPrice / computedTotalUnits
                )
                : 0.0;
        if (!purchase.NetCostPerUnit.AreApproximatelyEqual(computedNetCostPerUnit))
            return (
                false,
                $"Rule 13: NetCostPerUnit ({purchase.NetCostPerUnit}) does not match computed net cost per unit ({computedNetCostPerUnit})."
            );

        // Rule 14: NetTotalCost should equal (if VAT is disallowed) TotalAmountDue, else NetTotalPrice.
        double computedNetTotalCost = purchase.IsVatADisallowedInputTax
            ? computedTotalAmountDue
            : computedNetTotalPrice;
        if (!purchase.NetTotalCost.AreApproximatelyEqual(computedNetTotalCost))
            return (
                false,
                $"Rule 14: NetTotalCost ({purchase.NetTotalCost}) does not match computed net total cost ({computedNetTotalCost})."
            );

        return (true, "Summation fields are valid.");
    }

    // Validate SellingPrice and markup fields.
    public static (bool Valid, string Error) ValidateSellingPriceMargin(
        this Purchase purchase,
        double tolerance = 0.001
    )
    {
        // Rule 15: SellingPrice is per unit.
        // If SellingPrice is approximately equal to NetCostPerUnit then margins must be zero.
        if (purchase.SellingPrice.AreApproximatelyEqual(purchase.NetCostPerUnit, tolerance))
        {
            if (
                !purchase.GrossMarkupAbsolute.AreApproximatelyEqual(0, tolerance)
                || !purchase.GrossMarkupPercentage.AreApproximatelyEqual(0, tolerance)
            )
            {
                return (
                    false,
                    $"Rule 15: When SellingPrice ({purchase.SellingPrice}) equals NetCostPerUnit ({purchase.NetCostPerUnit}), both GrossMarkupAbsolute and GrossMarkupPercentage must be 0."
                );
            }
        }
        else
        {
            // SellingPrice should not be less than NetCostPerUnit.
            if (purchase.SellingPrice < purchase.NetCostPerUnit)
                return (
                    false,
                    $"Rule 15: SellingPrice ({purchase.SellingPrice}) cannot be less than NetCostPerUnit ({purchase.NetCostPerUnit})."
                );

            // Check that margin values are consistent.
            double expectedMarkupAbsolute = purchase.SellingPrice - purchase.NetCostPerUnit;
            if (
                !purchase.GrossMarkupAbsolute.AreApproximatelyEqual(
                    expectedMarkupAbsolute,
                    tolerance
                )
            )
                return (
                    false,
                    $"Rule 15: GrossMarkupAbsolute ({purchase.GrossMarkupAbsolute}) does not equal SellingPrice - NetCostPerUnit ({expectedMarkupAbsolute})."
                );

            double expectedMarkupPercentage =
                purchase.NetCostPerUnit > 0
                    ? (expectedMarkupAbsolute / purchase.NetCostPerUnit) * 100.0
                    : 0.0;
            if (
                !purchase.GrossMarkupPercentage.AreApproximatelyEqual(
                    expectedMarkupPercentage,
                    tolerance
                )
            )
                return (
                    false,
                    $"Rule 15: GrossMarkupPercentage ({purchase.GrossMarkupPercentage}) does not match expected value ({expectedMarkupPercentage})."
                );
        }
        return (true, "Rule 15: SellingPrice and margins are valid.");
    }

    /// <summary>
    /// Runs all individual validations and returns an array of (bool, string) tuples.
    /// </summary>
    public static (bool ValidationStatus, string errorDescription)[] ValidateAll(
        this Purchase purchase
    )
    {
        return new (bool, string)[]
        {
            purchase.ValidatePackSize(),
            purchase.ValidatePackQuantity(),
            purchase.ValidateReceivedAsUnitQuantity(),
            purchase.ValidateFreeUnits(),
            purchase.ValidateCosts(),
            purchase.ValidateDiscount(),
            purchase.ValidateVAT(),
            purchase.ValidateSummationFields(),
            purchase.ValidateSellingPriceMargin(),
        };
    }

    /// <summary>
    /// Aggregates all validations into one overall result. The error message contains
    /// concatenated descriptions for any failed rules.
    /// </summary>
    public static (bool Valid, string Error) Validate(this Purchase purchase)
    {
        var validations = purchase.ValidateAll();
        bool overallValid = validations.All(v => v.ValidationStatus);
        string errorDescription = string.Join(
            "; ",
            validations.Where(v => !v.ValidationStatus).Select(v => v.errorDescription)
        );
        return (
            overallValid,
            string.IsNullOrWhiteSpace(errorDescription)
                ? "All validations passed."
                : errorDescription
        );
    }
}
