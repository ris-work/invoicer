using RV.InvNew.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rv.InvNew.Common
{
    /// <summary>
    /// Contains modular extension methods for validating Purchase and ReceivedInvoice objects.
    /// These rules are shared by both frontend and backend and include fuzzy percent‐based comparisons.
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Helper: compares two doubles using a relative tolerance.
        /// If expected is zero, a small absolute tolerance is used.
        /// </summary>
        private static bool AreApproximatelyEqual(double expected, double actual, double tolPercentage)
        {
            double tolerance = (Math.Abs(expected) > 1e-12)
                ? Math.Abs(expected) * (tolPercentage / 100.0)
                : 1e-6;
            return Math.Abs(expected - actual) <= tolerance;
        }

        /// <summary>
        /// [Internal Function] Calculates total unit quantity for a purchase.
        /// Formula: (PackQuantity * PackSize) + ReceivedAsUnitQuantity + (FreePacks * PackSize) + FreeUnits.
        /// This rule exists so that the stored TotalUnits can be checked for consistency.
        /// </summary>
        internal static double CalculateTotalUnitQuantity(this Purchase p)
        {
            return p.PackQuantity * p.PackSize +
                   p.ReceivedAsUnitQuantity +
                   p.FreePacks * p.PackSize +
                   p.FreeUnits;
        }

        // --------------------
        // PURCHASE VALIDATION
        // --------------------

        /// <summary>
        /// [1] Discount consistency rule.
        /// DiscountAbsolute should equal (CostPerUnit × TotalUnits) × (DiscountPercentage/100).
        /// This rule exists because discounts can be specified both in absolute and percentage forms.
        /// </summary>
        public static (bool, string) ValidateDiscountConsistency(this Purchase p, double tolPercentage)
        {
            double calculatedTotal = p.CalculateTotalUnitQuantity();
            double baseValue = p.CostPerUnit * calculatedTotal;
            double expectedDiscount = baseValue * (p.DiscountPercentage / 100.0);

            if (AreApproximatelyEqual(expectedDiscount, p.DiscountAbsolute, tolPercentage))
                return (true, null);
            else
                return (false, $"[1] Discount Consistency Error: Expected DiscountAbsolute = {expectedDiscount:F4} (base: {baseValue:F4} with DiscountPercentage = {p.DiscountPercentage:F2}%), but found {p.DiscountAbsolute:F4}.");
        }

        /// <summary>
        /// [2] Gross profit consistency rule.
        /// GrossProfitAbsolute should equal GrossCostPerUnit × (GrossProfitPercentage/100).
        /// This rule relates the two representations (absolute vs. percentage) of gross profit.
        /// </summary>
        public static (bool, string) ValidateGrossProfitConsistency(this Purchase p, double tolPercentage)
        {
            double expectedGP = p.GrossCostPerUnit * (p.GrossProfitPercentage / 100.0);
            if (AreApproximatelyEqual(expectedGP, p.GrossProfitAbsolute, tolPercentage))
                return (true, null);
            else
                return (false, $"[2] Gross Profit Consistency Error: Expected GrossProfitAbsolute = {expectedGP:F4} (from GrossCostPerUnit = {p.GrossCostPerUnit:F4} and GrossProfitPercentage = {p.GrossProfitPercentage:F2}%), but found {p.GrossProfitAbsolute:F4}.");
        }

        /// <summary>
        /// [3] VAT consistency rule.
        /// VatAbsolute should equal ([CostPerUnit × TotalUnits] – DiscountAbsolute) × (VatPercentage/100).
        /// This rule ensures the VAT amounts “match up” between percentage and absolute representations.
        /// </summary>
        public static (bool, string) ValidateVatConsistency(this Purchase p, double tolPercentage)
        {
            double calculatedTotal = p.CalculateTotalUnitQuantity();
            double baseValue = p.CostPerUnit * calculatedTotal - p.DiscountAbsolute;
            double expectedVat = baseValue * (p.VatPercentage / 100.0);

            if (AreApproximatelyEqual(expectedVat, p.VatAbsolute, tolPercentage))
                return (true, null);
            else
                return (false, $"[3] VAT Consistency Error: Expected VatAbsolute = {expectedVat:F4} (from net base: {baseValue:F4} with VatPercentage = {p.VatPercentage:F2}%), but found {p.VatAbsolute:F4}.");
        }

        /// <summary>
        /// [4] Total units consistency rule.
        /// TotalUnits should equal the computed sum from pack quantities, sizes, received units and free units.
        /// This rule ensures that rounding or data entry errors are minimized.
        /// </summary>
        public static (bool, string) ValidateTotalUnitsConsistency(this Purchase p, double tolPercentage)
        {
            double calculatedTotal = p.CalculateTotalUnitQuantity();
            if (AreApproximatelyEqual(calculatedTotal, p.TotalUnits, tolPercentage))
                return (true, null);
            else
                return (false, $"[4] Total Units Consistency Error: Expected TotalUnits = {calculatedTotal:F4}, but found {p.TotalUnits:F4}.");
        }

        /// <summary>
        /// [5] Non-negative quantities rule.
        /// Verifies that pack size, pack quantity, received units, free packs and free units are not negative.
        /// These values must be non-negative to be meaningful.
        /// </summary>
        public static (bool, string) ValidateNonNegativeQuantities(this Purchase p)
        {
            var errors = new List<string>();
            if (p.PackSize < 0) errors.Add("PackSize is negative.");
            if (p.PackQuantity < 0) errors.Add("PackQuantity is negative.");
            if (p.ReceivedAsUnitQuantity < 0) errors.Add("ReceivedAsUnitQuantity is negative.");
            if (p.FreePacks < 0) errors.Add("FreePacks is negative.");
            if (p.FreeUnits < 0) errors.Add("FreeUnits is negative.");

            if (errors.Count == 0)
                return (true, null);
            else
                return (false, $"[5] Non-Negative Quantities Error: {string.Join(" ", errors)}");
        }

        /// <summary>
        /// Aggregates all purchase validation rules in detailed form.
        /// Returns an array of (bool, string) where the bool is true if the rule passes (with null error).
        /// The rules are numbered as above.
        /// </summary>
        public static (bool, string)[] ValidateDetailed(this Purchase p, double tolPercentage = 0.1)
        {
            return new (bool, string)[]
            {
                p.ValidateDiscountConsistency(tolPercentage),
                p.ValidateGrossProfitConsistency(tolPercentage),
                p.ValidateVatConsistency(tolPercentage),
                p.ValidateTotalUnitsConsistency(tolPercentage),
                p.ValidateNonNegativeQuantities()
            };
        }

        /// <summary>
        /// Aggregates detailed purchase validation rules.
        /// Returns a single tuple with overall (bool, string), where ErrorDescription concatenates all individual failing rules.
        /// </summary>
        public static (bool ValidationStatus, string ErrorDescription) Validate(this Purchase p, double tolPercentage = 0.1)
        {
            var details = p.ValidateDetailed(tolPercentage);
            bool overall = true;
            var errorMessages = new List<string>();

            foreach (var result in details)
            {
                if (!result.Item1)
                {
                    overall = false;
                    errorMessages.Add(result.Item2);
                }
            }

            return overall ? (true, null) : (false, string.Join(" ", errorMessages));
        }

        // ---------------------------
        // HEADER (ReceivedInvoice) VALIDATION
        // ---------------------------

        /// <summary>
        /// [H1] Header discount consistency rule.
        /// The header's Discount should equal GrossTotal × (DiscountPercentage/100).
        /// This rule exists to make sure the overall header discount information is consistent.
        /// </summary>
        public static (bool, string) ValidateHeaderDiscountConsistency(this ReceivedInvoice header, double tolPercentage = 0.1)
        {
            double expectedDiscount = header.GrossTotal * (header.DiscountPercentage / 100.0);
            if (expectedDiscount == 0)
                expectedDiscount = 1e-6;
            if (Math.Abs(expectedDiscount - header.Discount) <= Math.Abs(expectedDiscount) * (tolPercentage / 100.0))
                return (true, null);
            else
                return (false, $"[H1] Header Discount Consistency Error: Expected Discount = {expectedDiscount:F4} (GrossTotal = {header.GrossTotal:F4} & DiscountPercentage = {header.DiscountPercentage:F2}%), but found {header.Discount:F4}.");
        }

        /// <summary>
        /// [H2] Aggregate purchase discount consistency rule.
        /// Verifies that the sum of all Purchase.DiscountAbsolute values equals header.Discount.
        /// This rule ensures that the header discount “adds up” to the individual purchase discounts.
        /// </summary>
        public static (bool, string) ValidateAggregatePurchaseDiscount(this ReceivedInvoice header, List<Purchase> purchases, double tolPercentage = 0.1)
        {
            double totalPurchaseDiscount = purchases.Sum(p => p.DiscountAbsolute);
            if (AreApproximatelyEqual(header.Discount, totalPurchaseDiscount, tolPercentage))
                return (true, null);
            else
                return (false, $"[H2] Aggregate Purchase Discount Error: Sum of Purchase DiscountAbsolute = {totalPurchaseDiscount:F4} does not match Header.Discount = {header.Discount:F4}.");
        }

        /// <summary>
        /// [H3] GrossTotal consistency rule.
        /// The expected GrossTotal is calculated as (Sum of purchase costs) – Discount + TransportCharges.
        /// Here, each purchase cost is (CostPerUnit × [calculated total units]).
        /// This rule ensures that the invoice header aggregates costs correctly.
        /// </summary>
        public static (bool, string) ValidateHeaderGrossTotalConsistency(this ReceivedInvoice header, List<Purchase> purchases, double tolPercentage = 0.1)
        {
            double sumPurchaseCost = purchases.Sum(p => p.CostPerUnit * p.CalculateTotalUnitQuantity());
            double expectedGrossTotal = sumPurchaseCost - header.Discount + header.TransportCharges;
            if (AreApproximatelyEqual(expectedGrossTotal, header.GrossTotal, tolPercentage))
                return (true, null);
            else
                return (false, $"[H3] Header GrossTotal Consistency Error: Expected GrossTotal = {expectedGrossTotal:F4} (from Purchase cost sum = {sumPurchaseCost:F4}, Discount = {header.Discount:F4}, TransportCharges = {header.TransportCharges:F4}), but found {header.GrossTotal:F4}.");
        }

        /// <summary>
        /// Aggregates all header (ReceivedInvoice) validation rules in detailed form.
        /// Returns an array of (bool, string) tuples (rules [H1]–[H3]).
        /// </summary>
        public static (bool, string)[] ValidateDetailed(this ReceivedInvoice header, List<Purchase> purchases, double tolPercentage = 0.1)
        {
            return new (bool, string)[]
            {
                header.ValidateHeaderDiscountConsistency(tolPercentage),
                header.ValidateAggregatePurchaseDiscount(purchases, tolPercentage),
                header.ValidateHeaderGrossTotalConsistency(purchases, tolPercentage)
            };
        }

        /// <summary>
        /// Aggregates header validation rules as well as all purchase validations.
        /// Returns a single tuple (bool, string) where ErrorDescription concatenates the errors (if any).
        /// </summary>
        public static (bool ValidationStatus, string ErrorDescription) Validate(this ReceivedInvoice header, List<Purchase> purchases, double tolPercentage = 0.1)
        {
            bool overall = true;
            var errors = new List<string>();

            // Validate header-level rules.
            foreach (var result in header.ValidateDetailed(purchases, tolPercentage))
            {
                if (!result.Item1)
                {
                    overall = false;
                    errors.Add(result.Item2);
                }
            }

            // Validate each purchase.
            for (int i = 0; i < purchases.Count; i++)
            {
                var purchaseResult = purchases[i].Validate(tolPercentage);
                if (!purchaseResult.ValidationStatus)
                {
                    overall = false;
                    errors.Add($"Purchase #{i + 1}: {purchaseResult.ErrorDescription}");
                }
            }

            return overall ? (true, null) : (false, string.Join(" ", errors));
        }
    }
}
