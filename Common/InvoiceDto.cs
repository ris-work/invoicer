using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RV.InvNew.Common
{
    public class InvoiceDto
    {
        public DateTime InvoiceTime { get; set; } = DateTime.Now;
        public long? Customer { get; set; }
        public long SalesPersonId { get; set; }
        public string? CurrencyCode { get; set; }
        public string? InvoiceHumanFriendly { get; set; }

        public double SubTotal { get; set; }
        public double DiscountTotal { get; set; }
        public double EffectiveDiscountPercentage { get; set; }
        public double TaxTotal { get; set; }
        public double GrandTotal { get; set; }

        [JsonIgnore]
        public Pii? CustomerInfo { get; set; }
        [JsonIgnore]
        public AccountsInformation? DefaultCashAccount { get; set; }

        public List<Sale> SaleItems { get; set; } = new List<Sale>();
        public List<Payment> Payments { get; set; } = new List<Payment>();
        public List<LoyaltyPoint> LoyaltyPointsToIssue { get; set; } = new List<LoyaltyPoint>();
        public List<LoyaltyPointsRedemption> LoyaltyPointsToRedeem { get; set; } = new List<LoyaltyPointsRedemption>();

        public (bool IsValid, string ConsolidatedErrorList) ValidateInputs()
        {
            var errors = new List<string>();
            if (SaleItems.Count == 0) errors.Add("No items in invoice.");
            if (SalesPersonId <= 0) errors.Add("Invalid Sales Person.");
            if (Payments.Sum(p => p.NetAmount) < GrandTotal - 0.01) errors.Add($"Payments insufficient. Paid: {Payments.Sum(p => p.NetAmount):F2}, Due: {GrandTotal:F2}");
            return (errors.Count == 0, string.Join(", ", errors));
        }
    }

    public static class SalesProcessor
    {
        public static InvoiceDto ApplyDiscounts(
            this InvoiceDto dto,
            IQueryable<Inventory> inventoryQuery,
            IQueryable<Catalogue> catalogueQuery,
            IQueryable<VolumeDiscount> volumeDiscountQuery,
            IQueryable<Pii> piiQuery,
            IQueryable<AccountsInformation> accountsQuery)
        {
            // 1. Process Sales Items
            var processedSales = dto.SaleItems.AsQueryable()
                .Join(inventoryQuery, s => new { s.Itemcode, s.Batchcode }, i => new { i.Itemcode, i.Batchcode }, (s, i) => new { Sale = s, Inv = i })
                .Join(catalogueQuery, x => x.Sale.Itemcode, c => c.Itemcode, (x, c) => new { x.Sale, x.Inv, Cat = c })
                .GroupJoin(volumeDiscountQuery, x => x.Sale.Itemcode, vd => vd.Itemcode, (x, vds) => new { x.Sale, x.Inv, x.Cat, Vols = vds })
                .SelectMany(x => x.Vols.Where(v => x.Sale.Quantity >= v.StartFrom).OrderByDescending(v => v.StartFrom).Take(1).DefaultIfEmpty(), (x, v) => new { x.Sale, x.Inv, x.Cat, Vol = v })
                .GroupJoin(piiQuery, x => dto.Customer, p => p.Id, (x, ps) => new { x.Sale, x.Inv, x.Cat, x.Vol, Pii = ps.FirstOrDefault() })

                // Projection 1: Extract base values and rates
                .Select(x => new
                {
                    x.Sale,
                    x.Inv,
                    x.Vol,
                    x.Pii,
                    BaseUnitPrice = x.Sale.SellingPrice > 0 ? x.Sale.SellingPrice : x.Inv.SellingPrice,
                    Qty = x.Sale.Quantity,
                    MinPriceTotal = x.Inv.MinPrice * x.Sale.Quantity,
                    VatRate = x.Sale.VatRatePercentage,
                    VolDiscPerUnit = x.Vol == null ? 0.0 : x.Vol.DiscountPerUnit,
                    MultRateInv = x.Inv.MultiplicativeDiscountPercentage,
                    MultRatePii = x.Pii == null ? 0.0 : x.Pii.DiscountRateMultiplicativePercentage,
                    AddRateInv = x.Inv.AdditiveDiscountPercentage,
                    AddRatePii = x.Pii == null ? 0.0 : x.Pii.DiscountRateAdditivePercentage,
                    LpRatePii = x.Pii == null ? 0.0 : x.Pii.LoyaltyPointsRateMultiplicativePercentage + x.Pii.LoyaltyPointsRateAdditivePercentage,
                    LpRateDefault = dto.DefaultCashAccount == null ? 0.0 : dto.DefaultCashAccount.LoyaltyBaseMultiplicativePointsPercentage
                })

                // Projection 2: Calculate intermediate totals
                .Select(x => new
                {
                    x.Sale,
                    x.BaseUnitPrice,
                    x.Qty,
                    x.MinPriceTotal,
                    x.VatRate,
                    x.VolDiscPerUnit,
                    x.LpRatePii,
                    x.LpRateDefault,
                    GrossTotal = x.BaseUnitPrice * x.Qty,
                    EffectiveMultRate = (1.0 - (1.0 - x.MultRateInv / 100.0) * (1.0 - x.MultRatePii / 100.0)) * 100.0,
                    TotalAddRate = x.AddRateInv + x.AddRatePii
                })

                // Projection 3: Calculate final values (Net, Discount, VAT) and Explanation
                .Select(x => new
                {
                    x.Sale,
                    x.GrossTotal,
                    x.VolDiscPerUnit,
                    x.EffectiveMultRate,
                    x.TotalAddRate,
                    x.MinPriceTotal, // Included here to fix the error
                    x.VatRate,
                    x.LpRatePii,
                    x.LpRateDefault,

                    // Calculate Raw Net Total (before Min Price clamp)
                    RawNetTotal = x.GrossTotal - (x.VolDiscPerUnit * x.Qty) - (x.GrossTotal * ((x.EffectiveMultRate / 100.0) + (x.TotalAddRate / 100.0))),

                    // Explanation Construction
                    Explanation = "Vol:" + (x.VolDiscPerUnit > 0 ? x.VolDiscPerUnit.ToString("F2") : "0")
                                + "; Mult:" + x.EffectiveMultRate.ToString("F2") + "%"
                                + "; Add:" + x.TotalAddRate.ToString("F2") + "%"
                })

                // Projection 4: Apply Min Price Clamp, Calculate Final Totals, VAT, and LP
                .Select(x => new
                {
                    x.Sale,
                    // Apply Min Price Clamp
                    FinalNetTotal = x.RawNetTotal < x.MinPriceTotal ? x.MinPriceTotal : x.RawNetTotal,
                    x.VatRate,
                    x.LpRatePii,
                    x.LpRateDefault,
                    x.Explanation,

                    // Calculate Final Discount
                    FinalDiscount = x.GrossTotal - (x.RawNetTotal < x.MinPriceTotal ? x.MinPriceTotal : x.RawNetTotal),

                    // Flag for clamping
                    IsClamped = x.RawNetTotal < x.MinPriceTotal
                })

                // Projection 5: Calculate VAT and Loyalty Points
                .Select(x => new
                {
                    x.Sale,
                    Discount = x.FinalDiscount,
                    VatAsCharged = x.FinalNetTotal * (x.VatRate / 100.0),
                    TotalEffectiveSellingPrice = x.FinalNetTotal + (x.FinalNetTotal * (x.VatRate / 100.0)),
                    LoyalityPointsIssued = x.FinalNetTotal * ((x.LpRatePii > 0 ? x.LpRatePii : x.LpRateDefault) / 100.0),
                    Explanation = x.Explanation + (x.IsClamped ? "; ClampedToMin" : "")
                })

                // Projection 6: Cap Loyalty Points
                .Select(x => new
                {
                    x.Sale,
                    x.Discount,
                    x.VatAsCharged,
                    x.TotalEffectiveSellingPrice,
                    // Cap LP: Cannot exceed Net Revenue (FinalNetTotal)
                    LoyalityPointsIssued = x.LoyalityPointsIssued > (x.TotalEffectiveSellingPrice - x.VatAsCharged)
                                            ? (x.TotalEffectiveSellingPrice - x.VatAsCharged)
                                            : x.LoyalityPointsIssued,
                    Explanation = x.Explanation + (x.LoyalityPointsIssued > (x.TotalEffectiveSellingPrice - x.VatAsCharged) ? "; LPCapped" : "")
                })
                .ToList();

            foreach (var p in processedSales)
            {
                p.Sale.Discount = p.Discount;
                p.Sale.VatAsCharged = p.VatAsCharged;
                p.Sale.TotalEffectiveSellingPrice = p.TotalEffectiveSellingPrice;
                p.Sale.LoyalityPointsIssued = p.LoyalityPointsIssued;
            }

            // 2. Process Payments & Surcharges
            var processedPayments = dto.Payments.AsQueryable()
                .Join(accountsQuery, p => p.DebitAccountId, a => a.AccountNo, (p, a) => new { Payment = p, Acc = a })
                .Select(x => new
                {
                    x.Payment,
                    SurchargeMult = x.Acc.AccountSurchargesMultiplicativePercentage,
                    SurchargeAdd = x.Acc.AccountSurchargesAdditiveFee
                })
                .Select(x => new
                {
                    x.Payment,
                    FeeAmount = (x.Payment.Amount * (x.SurchargeMult / 100.0)) + x.SurchargeAdd,
                    NetAmount = x.Payment.Amount - ((x.Payment.Amount * (x.SurchargeMult / 100.0)) + x.SurchargeAdd),
                    Explanation = "Surcharge: " + x.SurchargeMult.ToString("F2") + "% + " + x.SurchargeAdd.ToString("F2")
                })
                .ToList();

            foreach (var p in processedPayments)
            {
                p.Payment.FeeAmount = p.FeeAmount;
                p.Payment.NetAmount = p.NetAmount;
            }

            // 3. Calculate Totals
            dto.SubTotal = dto.SaleItems.Sum(s => s.Quantity * s.SellingPrice);
            dto.DiscountTotal = dto.SaleItems.Sum(s => s.Discount);
            dto.TaxTotal = dto.SaleItems.Sum(s => s.VatAsCharged);
            dto.GrandTotal = dto.SaleItems.Sum(s => s.TotalEffectiveSellingPrice);
            dto.EffectiveDiscountPercentage = dto.SubTotal > 0 ? (dto.DiscountTotal / dto.SubTotal) * 100 : 0;

            return dto;
        }

        public static void TestApplyDiscounts()
        {
            Console.WriteLine("--- Testing SalesProcessor.ApplyDiscounts ---");

            var invList = new List<Inventory> {
                new Inventory { Itemcode = -1000, Batchcode = -2000, SellingPrice = 100.0, MinPrice = 80.0, MultiplicativeDiscountPercentage = 10.0, AdditiveDiscountPercentage = 5.0, MeasurementUnit = "pcs" }
            };
            var catList = new List<Catalogue> {
                new Catalogue { Itemcode = -1000, Description = "Test Item", ProcessDiscounts = true }
            };
            var volList = new List<VolumeDiscount> {
                new VolumeDiscount { Itemcode = -1000, StartFrom = 5, DiscountPerUnit = 2.0 }
            };
            var piiList = new List<Pii> {
                new Pii { Id = -4000, Name = "Test Customer", DiscountRateMultiplicativePercentage = 5.0, DiscountRateAdditivePercentage = 0.0, LoyaltyPointsRateMultiplicativePercentage = 10.0 }
            };
            var accList = new List<AccountsInformation> {
                new AccountsInformation { AccountNo = -5000, AccountName = "Test Card", AccountSurchargesMultiplicativePercentage = 2.0, AccountSurchargesAdditiveFee = 1.0, LoyaltyBaseMultiplicativePointsPercentage = 1.0 }
            };

            var dto = new InvoiceDto
            {
                Customer = -4000,
                DefaultCashAccount = accList.First(),
                SaleItems = new List<Sale> {
                    new Sale { Itemcode = -1000, Batchcode = -2000, Quantity = 10, SellingPrice = 100.0, VatRatePercentage = 10.0 }
                },
                Payments = new List<Payment> {
                    new Payment { DebitAccountId = -5000, Amount = 1000.0 }
                }
            };

            var result = dto.ApplyDiscounts(
                invList.AsQueryable(),
                catList.AsQueryable(),
                volList.AsQueryable(),
                piiList.AsQueryable(),
                accList.AsQueryable()
            );

            var sale = result.SaleItems.First();
            Console.WriteLine($"Item: {sale.Itemcode}, Qty: {sale.Quantity}");
            Console.WriteLine($"Base Price: {sale.SellingPrice}, SubTotal: {sale.SellingPrice * sale.Quantity}");
            Console.WriteLine($"Total Discount: {sale.Discount}");
            Console.WriteLine($"Net Price (Pre-VAT): {sale.TotalEffectiveSellingPrice - sale.VatAsCharged}");
            Console.WriteLine($"VAT: {sale.VatAsCharged}");
            Console.WriteLine($"Total: {sale.TotalEffectiveSellingPrice}");
            Console.WriteLine($"Loyalty Points: {sale.LoyalityPointsIssued}");

            var pay = result.Payments.First();
            Console.WriteLine($"Payment: {pay.Amount}, Fee: {pay.FeeAmount}, Net: {pay.NetAmount}");
        }

        public static void TestAllBranchesApplyDiscountsAndSurcharges()
        {
            // Branch Variables Arrays
            bool[] flags = { false, true };
            double[] z10 = { 0.0, 10.0 };
            double[] z05 = { 0.0, 5.0 };
            double[] z02 = { 0.0, 2.0 };
            double[] z01 = { 0.0, 1.0 };
            double[] z150 = { 0.0, 150.0 }; // Used for LP Cap test

            Console.WriteLine("Vol\tInvM\tInvA\tPii\tPiiM\tPiiA\tPiiL\tDef\tDefL\tMin\tCap\tPayM\tPayA\tNet\tDisc");

            foreach (var v in flags) // Has Volume Discount
            {
                foreach (var im in z10) // Inventory Mult %
                {
                    foreach (var ia in z05) // Inventory Add %
                    {
                        foreach (var p in flags) // Has Pii (Customer)
                        {
                            foreach (var pm in z05) // Pii Mult %
                            {
                                foreach (var pa in z02) // Pii Add %
                                {
                                    foreach (var pl in z10) // Pii Loyalty % (Normal)
                                    {
                                        foreach (var d in flags) // Has Default Cash Account
                                        {
                                            foreach (var dl in z01) // Default Loyalty %
                                            {
                                                foreach (var m in flags) // Min Price Scenario (Forces Clamp)
                                                {
                                                    foreach (var c in flags) // LP Cap Scenario (Forces Cap)
                                                    {
                                                        foreach (var pym in z02) // Pay Surcharge Mult %
                                                        {
                                                            foreach (var pya in z01) // Pay Surcharge Add Fee
                                                            {
                                                                // Apply Logic Masks
                                                                double pM_val = p ? pm : 0.0;
                                                                double pA_val = p ? pa : 0.0;
                                                                double pL_val = p ? (c ? 150.0 : pl) : 0.0; // If Cap, force 150
                                                                double dL_val = d ? dl : 0.0;

                                                                // Setup Entities
                                                                var inv = new List<Inventory> {
                                                                    new Inventory {
                                                                        Itemcode = 1, Batchcode = 1, SellingPrice = 100,
                                                                        MinPrice = m ? 900 : 0, // High MinPrice to trigger clamp if discounts are high
                                                                        MultiplicativeDiscountPercentage = im,
                                                                        AdditiveDiscountPercentage = ia
                                                                    }
                                                                };
                                                                var cat = new List<Catalogue> { new Catalogue { Itemcode = 1 } };
                                                                var vol = v ? new List<VolumeDiscount> { new VolumeDiscount { Itemcode = 1, StartFrom = 1, DiscountPerUnit = 10 } } : new List<VolumeDiscount>();
                                                                var pii = p ? new List<Pii> {
                                                                    new Pii {
                                                                        Id = 1,
                                                                        DiscountRateMultiplicativePercentage = pM_val,
                                                                        DiscountRateAdditivePercentage = pA_val,
                                                                        LoyaltyPointsRateMultiplicativePercentage = pL_val
                                                                    }
                                                                } : new List<Pii>();
                                                                var def = d ? new AccountsInformation { AccountNo = 1, LoyaltyBaseMultiplicativePointsPercentage = dL_val } : null;
                                                                var acc = new List<AccountsInformation> {
                                                                    new AccountsInformation {
                                                                        AccountNo = 2,
                                                                        AccountSurchargesMultiplicativePercentage = pym,
                                                                        AccountSurchargesAdditiveFee = pya
                                                                    }
                                                                };

                                                                var dto = new InvoiceDto
                                                                {
                                                                    DefaultCashAccount = def,
                                                                    SaleItems = new List<Sale> {
                                                                        new Sale {
                                                                            Itemcode = 1, Batchcode = 1, Quantity = 10,
                                                                            SellingPrice = 100, VatRatePercentage = 0
                                                                        }
                                                                    },
                                                                    Payments = new List<Payment> {
                                                                        new Payment { DebitAccountId = 2, Amount = 1000 }
                                                                    }
                                                                };

                                                                // Execute
                                                                var res = dto.ApplyDiscounts(inv.AsQueryable(), cat.AsQueryable(), vol.AsQueryable(), pii.AsQueryable(), acc.AsQueryable());
                                                                var s = res.SaleItems.First();
                                                                var pay = res.Payments.First();

                                                                // Output
                                                                Console.WriteLine($"{(v ? "Y" : "N")}\t{im}\t{ia}\t{(p ? "Y" : "N")}\t{pM_val}\t{pA_val}\t{pL_val}\t{(d ? "Y" : "N")}\t{dL_val}\t{(m ? "Y" : "N")}\t{(c ? "Y" : "N")}\t{pym}\t{pya}\t{s.TotalEffectiveSellingPrice:F1}\t{s.Discount:F1}");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}