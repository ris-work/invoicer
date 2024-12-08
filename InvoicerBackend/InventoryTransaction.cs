using RV.InvNew.Common;

namespace InvoicerBackend
{
    public interface InventoryTransaction
    {
        public void DoAccountingJournalEntries(NewinvContext ctx);
        public void DoInventoryAdjustments(NewinvContext ctx) ;
    }
}
