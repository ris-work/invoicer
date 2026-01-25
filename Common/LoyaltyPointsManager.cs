using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;


namespace RV.InvNew.Common;
public static class LoyaltyPointsManager
{


    /// <summary>
    /// Gets all valid, non-empty loyalty points buckets for a customer
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="customerId">Customer ID</param>
    /// <returns>Collection of loyalty points with remaining amounts</returns>
    public static IEnumerable<(LoyaltyPoint Point, double RemainingPoints)> GetValidNonEmptyPointsBuckets(
        NewinvContext context,
        long customerId)
    {
        Log($"Getting valid points buckets for customer {customerId}");

        // Get all loyalty points for the customer
        var loyaltyPointsQuery = context.LoyaltyPoints
            .Where(lp => lp.CustId == customerId && lp.ValidUntil > DateTime.UtcNow)
            .Track();

        // For each loyalty point, calculate the remaining amount after redemptions
        // We need to use the Query property to access the IQueryable
        var pointsWithRemainingQuery = loyaltyPointsQuery.Query
            .Select(lp => new
            {
                Point = lp,
                RedeemedAmount = context.LoyaltyPointsRedemptions
                    .Where(lpr => lpr.LoyalityPointsId == lp.PointsId)
                    .Sum(lpr => lpr.Amount)
            })
            .Select(x => new
            {
                Point = x.Point,
                RemainingPoints = x.Point.Amount - x.RedeemedAmount
            })
            .Where(x => x.RemainingPoints > 0);

        // Convert to the required return format when the query is executed
        // This avoids using tuples in the expression tree
        return pointsWithRemainingQuery.AsEnumerable()
            .Select(x => (x.Point, x.RemainingPoints));
    }

    /// <summary>
    /// Gets the total valid points for a customer
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="customerId">Customer ID</param>
    /// <returns>Total valid points</returns>
    public static double GetTotalValidPoints(NewinvContext context, long customerId)
    {
        Log($"Getting total valid points for customer {customerId}");

        // Get valid points buckets with remaining amounts
        var validPointsBuckets = GetValidNonEmptyPointsBuckets(context, customerId);

        // Sum up all remaining points
        var totalPoints = validPointsBuckets
            .Select(x => x.RemainingPoints)
            .Sum();

        Log($"Total valid points for customer {customerId}: {totalPoints}");

        return totalPoints;
    }

    /// <summary>
    /// Redeems loyalty points for a customer
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="pointsToRedeem">Points to redeem</param>
    /// <param name="customerId">Customer ID</param>
    /// <param name="invoiceId">Invoice ID for the redemption</param>
    /// <param name="redeemedFor">Description of what points are redeemed for</param>
    /// <returns>Collection of loyalty points redemption entries to add</returns>
    public static IEnumerable<LoyaltyPointsRedemption> Redeem(
        NewinvContext context,
        double pointsToRedeem,
        long customerId,
        long invoiceId,
        string redeemedFor)
    {
        Log($"Redeeming {pointsToRedeem} points for customer {customerId}");

        if (pointsToRedeem <= 0)
        {
            Log("Points to redeem must be positive");
            yield break;
        }

        // Get valid points buckets sorted by expiry date (earliest first)
        var validPointsBuckets = GetValidNonEmptyPointsBuckets(context, customerId)
            .OrderBy(x => x.Point.ValidUntil);

        double remainingPointsToRedeem = pointsToRedeem;

        // Process each bucket until we've redeemed all points
        foreach (var (point, remainingPoints) in validPointsBuckets)
        {
            if (remainingPointsToRedeem <= 0)
                break;

            // Calculate how many points to redeem from this bucket
            double pointsFromThisBucket = Math.Min(remainingPointsToRedeem, remainingPoints);

            Log($"Redeeming {pointsFromThisBucket} points from bucket {point.PointsId} (expires {point.ValidUntil})");

            // Create a redemption entry
            yield return new LoyaltyPointsRedemption
            {
                CustId = customerId,
                InvoiceId = invoiceId,
                Amount = pointsFromThisBucket,
                TimeIssued = DateTimeOffset.UtcNow,
                LoyalityPointsId = point.PointsId,
                RedeemedFor = redeemedFor
            };

            // Update remaining points to redeem
            remainingPointsToRedeem -= pointsFromThisBucket;
        }

        if (remainingPointsToRedeem > 0)
        {
            Log($"Warning: Could not redeem all points. {remainingPointsToRedeem} points remaining.");
        }
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[LoyaltyPointsManager] {message}");
    }

    // Test function
    public static void TestLoyaltyPoints()
    {
        Console.WriteLine("=== Loyalty Points Test ===");

        // Create a mock context for testing
        using (var context = new NewinvContext())
        {
            // Test parameters
            const long testCustomerId = -1000;
            const double initialPoints = 200;
            const double pointsToRedeem = 100;
            const long testInvoiceId = -1;
            const string redeemedFor = "Test Redemption";

            try
            {
                // Clean up any existing test data
                /*Console.WriteLine("Cleaning up any existing test data...");
                var existingPoints = context.LoyaltyPoints.Where(lp => lp.CustId == testCustomerId).ToList();
                if (existingPoints.Any())
                {
                    context.LoyaltyPoints.RemoveRange(existingPoints);
                    var existingRedemptions = context.LoyaltyPointsRedemptions
                        .Where(lpr => lpr.CustId == testCustomerId).ToList();
                    if (existingRedemptions.Any())
                    {
                        context.LoyaltyPointsRedemptions.RemoveRange(existingRedemptions);
                    }
                    context.SaveChanges();
                }*/

                // Add loyalty points for test user
                Console.WriteLine($"Adding {initialPoints} loyalty points for customer {testCustomerId}...");
                var loyaltyPoint = new LoyaltyPoint
                {
                    CustId = testCustomerId,
                    Amount = initialPoints,
                    ValidFrom = DateTime.UtcNow,
                    ValidUntil = DateTime.UtcNow.AddMonths(12), // Valid for 12 months
                    InvoiceId = testInvoiceId
                };
                context.LoyaltyPoints.Add(loyaltyPoint);
                context.SaveChanges();

                // Check total valid points
                Console.WriteLine("Checking total valid points...");
                var totalPoints = LoyaltyPointsManager.GetTotalValidPoints(context, testCustomerId);
                Console.WriteLine($"Total valid points for customer {testCustomerId}: {totalPoints}");

                // Get valid points buckets
                Console.WriteLine("Getting valid points buckets...");
                var validBuckets = LoyaltyPointsManager.GetValidNonEmptyPointsBuckets(context, testCustomerId).ToList();
                Console.WriteLine($"Found {validBuckets.Count} valid points buckets:");
                foreach (var (point, remaining) in validBuckets)
                {
                    Console.WriteLine($"  - Bucket {point.PointsId}: {remaining} points (expires {point.ValidUntil})");
                }

                // Redeem points
                Console.WriteLine($"Redeeming {pointsToRedeem} points...");
                var redemptions = LoyaltyPointsManager.Redeem(
                    context,
                    pointsToRedeem,
                    testCustomerId,
                    testInvoiceId,
                    redeemedFor
                ).ToList();

                // Add redemptions to database
                if (redemptions.Any())
                {
                    context.LoyaltyPointsRedemptions.AddRange(redemptions);
                    context.SaveChanges();

                    Console.WriteLine($"Created {redemptions.Count} redemption entries:");
                    foreach (var redemption in redemptions)
                    {
                        Console.WriteLine($"  - {redemption.Amount} points from bucket {redemption.LoyalityPointsId}");
                    }
                }
                else
                {
                    Console.WriteLine("No redemption entries created.");
                }

                // Check remaining points after redemption
                Console.WriteLine("Checking remaining points after redemption...");
                var remainingBuckets = LoyaltyPointsManager.GetValidNonEmptyPointsBuckets(context, testCustomerId).ToList();
                var totalRemaining = LoyaltyPointsManager.GetTotalValidPoints(context, testCustomerId);

                Console.WriteLine($"Total remaining points: {totalRemaining}");
                Console.WriteLine("Remaining points buckets:");
                foreach (var (point, remaining) in remainingBuckets)
                {
                    Console.WriteLine($"  - Bucket {point.PointsId}: {remaining} points (expires {point.ValidUntil})");
                }

                // Verify the redemption worked correctly
                if (Math.Abs(totalRemaining - (initialPoints - pointsToRedeem)) < 0.01)
                {
                    Console.WriteLine("✅ Test passed: Points redemption worked correctly!");
                }
                else
                {
                    Console.WriteLine($"❌ Test failed: Expected {initialPoints - pointsToRedeem} remaining points, got {totalRemaining}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during test: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        Console.WriteLine("=== Loyalty Points Test Complete ===");
    }
}