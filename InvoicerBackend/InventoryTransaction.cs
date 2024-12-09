using RV.InvNew.Common;

namespace InvoicerBackend
{
    public interface InventoryTransaction
    {
        public void DoPrimaryEntry(NewinvContext ctx);
        public void DoAccountingJournalEntries(NewinvContext ctx);
        public void DoInventoryAdjustments(NewinvContext ctx) ;
        public void DoLogging(NewinvContext ctx);
    }
}
