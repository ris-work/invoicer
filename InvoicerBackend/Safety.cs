using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace InvoicerBackend
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// Ensures that the DbContext is currently inside a transaction with Serializable isolation level.
        /// <para>
        /// <b>CRITICAL:</b> This check is required to prevent data corruption in ERP/Inventory systems.
        /// Failure to use Serializable transactions can result in:
        /// <list type="bullet">
        ///     <item><description>LOST ACCOUNTING ENTRIES</description></item>
        ///     <item><description>INCORRECT INVENTORY COUNTS</description></item>
        ///     <item><description>PHANTOM READS / DOUBLE SPENDING</description></item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="context">The DbContext to check.</param>
        /// <exception cref="InvalidOperationException">Thrown if no transaction exists or isolation level is not Serializable.</exception>
        public static void EnsureSerializableTransaction(this DbContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            IDbContextTransaction? currentTransaction = context.Database.CurrentTransaction;

            // 1. Check for Transaction Existence
            if (currentTransaction == null)
            {
                throw new InvalidOperationException(
                    "╔══════════════════════════════════════════════════════════════╗\n" +
                    "║ CRITICAL DATA INTEGRITY ERROR: NO TRANSACTION DETECTED      ║\n" +
                    "╠══════════════════════════════════════════════════════════════╣\n" +
                    "║ This operation REQUIRES a transaction.                      ║\n" +
                    "║ Running without a transaction risks:                        ║\n" +
                    "║   > LOST ACCOUNTING ENTRIES                                 ║\n" +
                    "║   > ORPHANED INVENTORY RECORDS                              ║\n" +
                    "║   > DATA CORRUPTION                                         ║\n" +
                    "╚══════════════════════════════════════════════════════════════╝"
                );
            }

            // 2. Check Isolation Level
            IsolationLevel isolationLevel = currentTransaction.GetDbTransaction().IsolationLevel;

            if (isolationLevel != IsolationLevel.Serializable)
            {
                throw new InvalidOperationException(
                    "╔══════════════════════════════════════════════════════════════╗\n" +
                    "║ CRITICAL DATA INTEGRITY ERROR: INSUFFICIENT ISOLATION       ║\n" +
                    "╠══════════════════════════════════════════════════════════════╣\n" +
                    $"║ Current Level: {isolationLevel,-43}║\n" +
                    "║ Required Level: Serializable                                ║\n" +
                    "║                                                              ║\n" +
                    "║ Non-Serializable transactions risk:                         ║\n" +
                    "║   > PHANTOM READS (Inventory counts changing mid-process)   ║\n" +
                    "║   > NON-REPEATABLE READS (Financial calculation errors)    ║\n" +
                    "║   > LOST UPDATES (Overwriting other users' changes)        ║\n" +
                    "╚══════════════════════════════════════════════════════════════╝"
                );
            }
        }
    }
}
