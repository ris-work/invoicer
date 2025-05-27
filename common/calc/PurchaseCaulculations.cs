namespace RV.InvNew.Common
{
    public static class PurchaseCalculationExtensions
    {
        /// <summary>
        /// Calculates the total units and net total price.
        /// The formula used is:
        ///   TotalUnits = (PackQuantity * PackSize) + ReceivedAsUnitQuantity + (FreePacks * PackSize) + FreeUnits
        ///   NetTotalPrice = (CostPerUnit × TotalUnits) – DiscountAbsolute
        /// This method updates the Purchase and returns it.
        /// </summary>
        public static Purchase CalculateNetTotal(this Purchase p)
        {
            // Calculate total units using the standard formula.
            double calculatedTotalUnits = (p.PackQuantity * p.PackSize)
                + p.ReceivedAsUnitQuantity
                + (p.FreePacks * p.PackSize)
                + p.FreeUnits;
            p.TotalUnits = calculatedTotalUnits;

            // Net total price is the cost (per unit times total units) minus any absolute discount.
            p.NetTotalPrice = (p.CostPerUnit * calculatedTotalUnits) - p.DiscountAbsolute;

            return p;
        }

        /// <summary>
        /// Calculates the VAT and TotalAmountDue.
        /// Assumes VatAbsolute is computed as:
        ///   VatAbsolute = NetTotalPrice × (VatPercentage / 100)
        /// and then the final amount payable is:
        ///   TotalAmountDue = NetTotalPrice + VatAbsolute.
        /// This method first ensures that NetTotalPrice is up-to-date, then updates the VAT-related
        /// properties and returns the updated Purchase.
        /// </summary>
        public static Purchase CalculateTotalAmountDue(this Purchase p)
        {
            // First, update NetTotalPrice (and TotalUnits) in case they need refreshing.
            p.CalculateNetTotal();

            // Calculate the VAT amount.
            p.VatAbsolute = p.NetTotalPrice * (p.VatPercentage / 100.0);

            // Total amount due is the net plus the VAT.
            p.TotalAmountDue = p.NetTotalPrice + p.VatAbsolute;

            return p;
        }
    }
}
