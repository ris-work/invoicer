using System;
using System.Collections.Generic;
using System.Linq;
using RV.InvNew.Common;

namespace EtoFE.Validation
{
    public static class SalesValidation
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

        // Rule 1: Customer must be selected
        public static (bool Valid, string Error) ValidateCustomer(this IssuedInvoice invoice)
        {
            if (invoice.Customer <= 0)
                return (false, "Rule 1: Customer must be selected.");
            return (true, "Rule 1: Customer is valid.");
        }

        // Rule 2: Sales person must be selected
        public static (bool Valid, string Error) ValidateSalesPerson(this IssuedInvoice invoice)
        {
            if (invoice.SalesPersonId <= 0)
                return (false, "Rule 2: Sales person must be selected.");
            return (true, "Rule 2: Sales person is valid.");
        }

        // Rule 3: Currency must be specified
        public static (bool Valid, string Error) ValidateCurrency(this IssuedInvoice invoice)
        {
            if (string.IsNullOrWhiteSpace(invoice.CurrencyCode))
                return (false, "Rule 3: Currency must be specified.");
            return (true, "Rule 3: Currency is valid.");
        }

        // Rule 4: Invoice date must be valid
        public static (bool Valid, string Error) ValidateInvoiceDate(this IssuedInvoice invoice)
        {
            if (invoice.InvoiceTime == default)
                return (false, "Rule 4: Invoice date must be valid.");
            return (true, "Rule 4: Invoice date is valid.");
        }

        // Rule 5: At least one sale item must be added
        public static (bool Valid, string Error) ValidateSalesItems(this List<Sale> sales)
        {
            if (sales == null || sales.Count == 0 || (sales.Count == 1 && sales[0].Itemcode <= 0))
                return (false, "Rule 5: At least one sale item must be added.");
            return (true, "Rule 5: Sales items are valid.");
        }

        // Rule 6: Sale item must have valid quantity
        public static (bool Valid, string Error) ValidateSaleQuantity(this Sale sale)
        {
            if (sale.Quantity <= 0)
                return (false, "Rule 6: Sale item must have valid quantity.");
            return (true, "Rule 6: Sale item quantity is valid.");
        }

        // Rule 7: Sale item must have valid price
        public static (bool Valid, string Error) ValidateSalePrice(this Sale sale)
        {
            if (sale.SellingPrice <= 0)
                return (false, "Rule 7: Sale item must have valid price.");
            return (true, "Rule 7: Sale item price is valid.");
        }

        // Rule 8: Sale item must have valid batch
        public static (bool Valid, string Error) ValidateSaleBatch(this Sale sale)
        {
            if (sale.Batchcode <= 0)
                return (false, "Rule 8: Sale item must have valid batch.");
            return (true, "Rule 8: Sale item batch is valid.");
        }

        // Rule 9: Receipt amount must be positive
        public static (bool Valid, string Error) ValidateReceiptAmount(this Receipt receipt)
        {
            if (receipt.Amount <= 0)
                return (false, "Rule 9: Receipt amount must be positive.");
            return (true, "Rule 9: Receipt amount is valid.");
        }

        // Rule 10: Receipt account must be valid
        public static (bool Valid, string Error) ValidateReceiptAccount(this Receipt receipt)
        {
            if (receipt.AccountId <= 0)
                return (false, "Rule 10: Receipt account must be valid.");
            return (true, "Rule 10: Receipt account is valid.");
        }

        // Rule 11: Total receipts must match or exceed grand total
        public static (bool Valid, string Error) ValidateReceiptsTotal(this IssuedInvoice invoice, List<Receipt> receipts)
        {
            double receiptsTotal = receipts?.Sum(r => r.Amount) ?? 0;
            if (receiptsTotal < invoice.GrandTotal)
                return (false, "Rule 11: Total receipts must match or exceed grand total.");
            return (true, "Rule 11: Receipts total is valid.");
        }

        /// <summary>
        /// Runs all individual validations for invoice and returns an array of (bool, string) tuples.
        /// </summary>
        public static (bool ValidationStatus, string errorDescription)[] ValidateAll(this IssuedInvoice invoice)
        {
            return new (bool, string)[]
            {
                invoice.ValidateCustomer(),
                invoice.ValidateSalesPerson(),
                invoice.ValidateCurrency(),
                invoice.ValidateInvoiceDate(),
            };
        }

        /// <summary>
        /// Runs all individual validations for sales and returns an array of (bool, string) tuples.
        /// </summary>
        public static (bool ValidationStatus, string errorDescription)[] ValidateAll(this List<Sale> sales)
        {
            var results = new List<(bool, string)>();

            // Check if there are any sales
            results.Add(sales.ValidateSalesItems());

            // Validate each sale item
            foreach (var sale in sales)
            {
                if (sale.Itemcode <= 0) continue; // Skip empty placeholder

                results.Add(sale.ValidateSaleQuantity());
                results.Add(sale.ValidateSalePrice());
                results.Add(sale.ValidateSaleBatch());
            }

            return results.ToArray();
        }

        /// <summary>
        /// Runs all individual validations for receipts and returns an array of (bool, string) tuples.
        /// </summary>
        public static (bool ValidationStatus, string errorDescription)[] ValidateAll(this List<Receipt> receipts)
        {
            var results = new List<(bool, string)>();

            foreach (var receipt in receipts)
            {
                if (receipt.ReceiptId <= 0) continue; // Skip empty placeholder

                results.Add(receipt.ValidateReceiptAmount());
                results.Add(receipt.ValidateReceiptAccount());
            }

            return results.ToArray();
        }

        /// <summary>
        /// Aggregates all validations into one overall result. The error message contains
        /// concatenated descriptions for any failed rules.
        /// </summary>
        public static (bool Valid, string Error) Validate(this IssuedInvoice invoice, List<Sale> sales, List<Receipt> receipts)
        {
            var validations = new List<(bool, string)>();

            // Add invoice validations
            validations.AddRange(invoice.ValidateAll());

            // Add sales validations
            validations.AddRange(sales.ValidateAll());

            // Add receipts validations
            validations.AddRange(receipts.ValidateAll());

            // Add receipts total validation
            validations.Add(invoice.ValidateReceiptsTotal(receipts));

            bool overallValid = validations.All(v => v.Item1);
            string errorDescription = string.Join(
                "; ",
                validations.Where(v => !v.Item1).Select(v => v.Item2)
            );
            return (
                overallValid,
                string.IsNullOrWhiteSpace(errorDescription)
                    ? "All validations passed."
                    : errorDescription
            );
        }
    }
}