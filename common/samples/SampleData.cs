using RV.InvNew.Common;
using System.Collections.Generic;

public static class SampleDataGenerators
{
    // Generates sample VatCategory records.
    public static List<VatCategory> GenerateVatCategories()
    {
        return new List<VatCategory>
        {
            new VatCategory
            {
                VatCategoryId = 1,
                VatPercentage = 18.0,
                VatName = "Standard VAT",
                Active = true
            },
            new VatCategory
            {
                VatCategoryId = 2,
                VatPercentage = 5.0,
                VatName = "Reduced VAT",
                Active = false
            },
            new VatCategory
            {
                VatCategoryId = 3,
                VatPercentage = 0.0,
                VatName = "Zero VAT",
                Active = true
            },
            new VatCategory
            {
                VatCategoryId = 4,
                VatPercentage = 12.5,
                VatName = "Intermediate VAT",
                Active = true
            },
            new VatCategory
            {
                VatCategoryId = 5,
                VatPercentage = 2.5,
                VatName = "Super Reduced VAT",
                Active = true
            },
            new VatCategory
            {
                VatCategoryId = 6,
                VatPercentage = 20.0,
                VatName = "High VAT",
                Active = false
            }
        };
    }

    // Generates sample SuggestedPrice records with multiple prices per item.
    public static List<SuggestedPrice> GenerateSuggestedPrices()
    {
        return new List<SuggestedPrice>
        {
            // Item 1001 has three suggested prices.
            new SuggestedPrice { Itemcode = 1001, Price = 20.5 },
            new SuggestedPrice { Itemcode = 1001, Price = 21.0 },
            new SuggestedPrice { Itemcode = 1001, Price = 19.75 },
            
            // Item 1002 has three suggested prices.
            new SuggestedPrice { Itemcode = 1002, Price = 30.0 },
            new SuggestedPrice { Itemcode = 1002, Price = 29.5 },
            new SuggestedPrice { Itemcode = 1002, Price = 30.25 },
            
            // Item 1003 has two suggested prices.
            new SuggestedPrice { Itemcode = 1003, Price = 15.75 },
            new SuggestedPrice { Itemcode = 1003, Price = 16.0 },
            
            // Item 1004 has two suggested prices.
            new SuggestedPrice { Itemcode = 1004, Price = 99.99 },
            new SuggestedPrice { Itemcode = 1004, Price = 100.0 },
            
            // Item 1005 has three suggested prices.
            new SuggestedPrice { Itemcode = 1005, Price = 50.0 },
            new SuggestedPrice { Itemcode = 1005, Price = 51.0 },
            new SuggestedPrice { Itemcode = 1005, Price = 49.5 },
            
            // Item 1006 has two suggested prices.
            new SuggestedPrice { Itemcode = 1006, Price = 75.0 },
            new SuggestedPrice { Itemcode = 1006, Price = 74.5 }
        };
    }

    // Generates sample Pii records.
    public static List<Pii> GeneratePiiList()
    {
        return new List<Pii>
        {
            new Pii
            {
                Id = 1,
                Name = "John Doe",
                IsCompany = false,
                Email = "john@doe.com",
                Telephone = "555-1234",
                Mobile = "555-5678",
                Title = "Mr.",
                Address = "123 Main St",
                Fax = "555-1111",
                Im = "john_doe",
                Sip = "sip:john_doe@example.com",
                Gender = "Male"
            },
            new Pii
            {
                Id = 2,
                Name = "Jane Smith",
                IsCompany = false,
                Email = "jane@smith.com",
                Telephone = "555-8765",
                Mobile = "555-4321",
                Title = "Ms.",
                Address = "456 Second St",
                Fax = "555-2222",
                Im = "jane_smith",
                Sip = "sip:jane_smith@example.com",
                Gender = "Female"
            },
            new Pii
            {
                Id = 3,
                Name = "Acme Corporation",
                IsCompany = true,
                Email = "contact@acme.com",
                Telephone = "555-0001",
                Mobile = "555-0003",
                Title = "Company",
                Address = "789 Corporate Blvd",
                Fax = "555-0002",
                Im = "acme_chat",
                Sip = "sip:acme@example.com",
                Gender = null
            },
            new Pii
            {
                Id = 4,
                Name = "Samuel Green",
                IsCompany = false,
                Email = "samuel@green.com",
                Telephone = "555-3333",
                Mobile = "555-4444",
                Title = "Dr.",
                Address = "321 Oak Ave",
                Fax = "555-5555",
                Im = "sam_green",
                Sip = "sip:sam_green@example.com",
                Gender = "Male"
            },
            new Pii
            {
                Id = 5,
                Name = "Alice Brown",
                IsCompany = false,
                Email = "alice@brown.com",
                Telephone = "555-6789",
                Mobile = "555-9876",
                Title = "Ms.",
                Address = "987 Pine Rd",
                Fax = "555-7766",
                Im = "alice_brown",
                Sip = "sip:alice_brown@example.com",
                Gender = "Female"
            },
            new Pii
            {
                Id = 6,
                Name = "Bob Johnson",
                IsCompany = false,
                Email = "bob@johnson.com",
                Telephone = "555-1112",
                Mobile = "555-2211",
                Title = "Mr.",
                Address = "654 Maple Ln",
                Fax = "555-3322",
                Im = "bob_johnson",
                Sip = "sip:bob_johnson@example.com",
                Gender = "Male"
            }
        };
    }

    // Generates sample AccountsInformation records.
    public static List<AccountsInformation> GenerateAccountsInformation()
    {
        return new List<AccountsInformation>
        {
            new AccountsInformation
            {
                AccountType = 1,
                AccountNo = 111111,
                AccountName = "Main Account",
                AccountPii = 1,
                AccountI18nLabel = 10,
                AccountMin = 100.0,
                AccountMax = 10000.0,
                HumanFriendlyId = "ACC-001"
            },
            new AccountsInformation
            {
                AccountType = 2,
                AccountNo = 222222,
                AccountName = "Secondary Account",
                AccountPii = 2,
                AccountI18nLabel = 20,
                AccountMin = 50.0,
                AccountMax = 5000.0,
                HumanFriendlyId = "ACC-002"
            },
            new AccountsInformation
            {
                AccountType = 1,
                AccountNo = 333333,
                AccountName = "Savings Account",
                AccountPii = 3,
                AccountI18nLabel = 30,
                AccountMin = 200.0,
                AccountMax = 15000.0,
                HumanFriendlyId = "ACC-003"
            },
            new AccountsInformation
            {
                AccountType = 2,
                AccountNo = 444444,
                AccountName = "Investment Account",
                AccountPii = 4,
                AccountI18nLabel = 40,
                AccountMin = 500.0,
                AccountMax = 25000.0,
                HumanFriendlyId = "ACC-004"
            },
            new AccountsInformation
            {
                AccountType = 1,
                AccountNo = 555555,
                AccountName = "Business Account",
                AccountPii = 5,
                AccountI18nLabel = 50,
                AccountMin = 1000.0,
                AccountMax = 50000.0,
                HumanFriendlyId = "ACC-005"
            },
            new AccountsInformation
            {
                AccountType = 2,
                AccountNo = 666666,
                AccountName = "International Account",
                AccountPii = 6,
                AccountI18nLabel = 60,
                AccountMin = 2000.0,
                AccountMax = 75000.0,
                HumanFriendlyId = "ACC-006"
            }
        };
    }

    // Generates sample I18nLabel records.
    // Note: The I18nLabel class is already defined as a partial class elsewhere.
    public static List<I18nLabel> GenerateI18nLabels()
    {
        return new List<I18nLabel>
        {
            new I18nLabel { Id = 1, Lang = "en", Value = "Standard description" },
            new I18nLabel { Id = 2, Lang = "fr", Value = "Description standard" },
            new I18nLabel { Id = 3, Lang = "es", Value = "Descripción estándar" },
            new I18nLabel { Id = 4, Lang = "de", Value = "Standardbeschreibung" },
            new I18nLabel { Id = 5, Lang = "it", Value = "Descrizione standard" },
            new I18nLabel { Id = 6, Lang = "pt", Value = "Descrição padrão" },
            new I18nLabel { Id = 7, Lang = "ja", Value = "標準説明" },
            new I18nLabel { Id = 8, Lang = "zh", Value = "标准描述" }
        };
    }
}
