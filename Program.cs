using Extreme.Mathematics;
using Extreme.Statistics.Tests;
using static System.Console;
using static Auxiliary;

// *load data from files*
Product[] products = fileLines("Products.txt").Select(line => new Product(line)).ToArray();
User[] users = fileLines("Users.txt").Select(line => new User(line, products)).ToArray();
foreach (string line in fileLines("CurrentUserSession.txt")) User.LoadCurrentUserSession(line, products, users);

// *part one of challenge*
foreach (Product product in products) product.CalculateNumberOfPurchases(users);
foreach (Product product in products) product.CalculatePopularity();
Product[] productsByPopularity = products.OrderByDescending(product => product.Popularity).ToArray();

// output
WriteLine("\nVelkommen til InnoflowFlix! De mest populære film lige nu er");
for (int i = 0; i < Math.Min(3, productsByPopularity.Count()); i++)
{
    WriteLine($" {productsByPopularity[i].Name}");
}
WriteLine();

// *part two of challenge*
Product.CalculateDescriptiveStats(products);

// calculate score component 5
foreach (Product product in products)
{
    product.CalculateBuyersInCommonScore(products, users);
}

foreach (User user in users)
{
    if (user.CurrentSession is not null) // also this check above?
    {
        // calculate score component 2
        user.DeterminePriceSensitivity(products, Product.MeanPrice, Product.PriceStandardDeviation);
        user.CalculatePriceScore(products, Product.MaxPrice, Product.MinPrice);

        // calculate score component 3
        user.DetermineYearSensitivity(products, Product.MeanYear, Product.YearStandardDeviation);
        user.CalculateYearScore(products, Product.MaxYear, Product.MinYear);

        // calculate score component 4
        user.CalculateGenrePreference(Product.Genres);
        user.CalculateGenreScore(products);

        // calculate overall score
        user.CalculateOverallScore(products);
        Product[] productsByUserScore = products
            .Where(product => !user.Purchased.Contains(product) && user.CurrentSession != product)
            .OrderByDescending(product => user.OverallScore![product])
            .ToArray();

        // output
        WriteLine($"Hej {user.Name}! Hvis du er interesseret i {user.CurrentSession.Name}, skulle du også ta' at tjekke disse film ud:");
        for (int i = 0; i < Math.Min(3, productsByUserScore.Count()); i++)
        {
            WriteLine($"  {productsByUserScore[i].Name}");
        }
        WriteLine();
    }
}

public class Product // check conventions for order of members
{
    public int ID, Year, NumberOfPurchases;

    public string Name;

    public decimal Price;

    public double Rating, Popularity;

    public Dictionary<Product, double>? ViewersInCommonScore;

    public string[] Keywords;

    public static int MaxNumberOfPurchases, MaxYear, MinYear;

    public static double MeanPrice, PriceStandardDeviation, MeanYear, YearStandardDeviation;

    public static decimal MaxPrice, MinPrice;

    public static string[] Genres = new string[] { };

    public Product(string line)
    {
        string[] input = lineToArray(line);
        try
        {
            ID = int.Parse(input[0]);
            Name = input[1];
            Year = int.Parse(input[2]);
            Keywords = new string[] { input[3], input[4], input[5], input[6], input[7] }.Where(k => k != "").ToArray();
            Rating = double.Parse(input[8]);
            Price = decimal.Parse(input[9]);
        }
        catch (Exception)
        {
            throw new FileLoadException("Invalid product file");
        }
    }

    public void CalculateNumberOfPurchases(User[] users)
    {
        foreach (User user in users)
        {
            if (user.Purchased.Contains(this)) NumberOfPurchases++;
        }
        if (NumberOfPurchases > MaxNumberOfPurchases) MaxNumberOfPurchases = NumberOfPurchases;
    }

    public void CalculatePopularity()
    {
        if (MaxNumberOfPurchases > 0)
        {
            Popularity = Rating + 5 * ((double)NumberOfPurchases / MaxNumberOfPurchases);
        }
        else
        {
            Popularity = Rating;
        }
    }

    public static void CalculateDescriptiveStats(Product[] products)
    {
        MaxPrice = products.Select(p => p.Price).Max();
        MinPrice = products.Select(p => p.Price).Min();
        MeanPrice = products.Select(p => (double)p.Price).Average();
        double sum1 = (double)products.Sum(d => ((double)d.Price - MeanPrice) * ((double)d.Price - MeanPrice)); // merge two lines
        PriceStandardDeviation = Math.Sqrt(sum1 / products.Count()); // count must be >1 for meaningful

        MaxYear = products.Select(p => p.Year).Max();
        MinYear = products.Select(p => p.Year).Min();
        MeanYear = products.Select(p => (double)p.Year).Average();
        double sum2 = (double)products.Sum(d => (d.Year - MeanYear) * (d.Year - MeanYear));
        YearStandardDeviation = Math.Sqrt(sum2 / products.Count()); // count must be >1 for meaningful

        foreach (Product product in products) Genres = Genres.Union(product.Keywords).ToArray();
    }

    public void CalculateBuyersInCommonScore(Product[] products, User[] users) // do test with user histories that result in non-extreme values
    {
        Dictionary<Product, int>? preNormalizedBuyersInCommonScore = products.ToDictionary(
            product => product,
            product => users.Count(user => (user.Purchased.Contains(product) && user.Purchased.Contains(this)))
        );
        double maxPreNormalizedBuyersInCommonScore = preNormalizedBuyersInCommonScore.Values.Max();
        if (maxPreNormalizedBuyersInCommonScore == 0) maxPreNormalizedBuyersInCommonScore = 1; // to avoid division by 0
        ViewersInCommonScore = preNormalizedBuyersInCommonScore.ToDictionary(
            item => item.Key,
            item => (double)item.Value * 5 / maxPreNormalizedBuyersInCommonScore
        );
    }

    public override int GetHashCode()
    {
        return ID;
    }
}

public class User
{
    public int ID;

    public string Name;

    public Product[] Viewed, Purchased;

    public Product? CurrentSession;

    public string? PriceSensitivity, YearSensitivity;

    public Dictionary<Product, double> PriceScore = new Dictionary<Product, double> { }, YearScore = new Dictionary<Product, double> { };

    public Dictionary<Product, double>? GenreScore, OverallScore;

    public Dictionary<string, double>? GenrePreference;

    public User(string line, Product[] products)
    {
        string[] input = lineToArray(line);
        try
        {
            ID = int.Parse(input[0]);

            Name = input[1];

            if (input[2] == "") // the user has viewed no movies
            {
                Viewed = new Product[0];
            }
            else
            {
                Viewed = input[2].Split(';').Select(
                    n => Array.Find(products, p => p.ID == int.Parse(n))!
                ).ToArray();
            }

            if (input[3] == "") // the user has purchased no movies
            {
                Purchased = new Product[0];
            }
            else
            {
                Purchased = input[3].Split(';').Select(
                    n => Array.Find(products, p => p.ID == int.Parse(n))!
                ).ToArray();
            }

            if (Viewed.Any(p => p == null) || Purchased.Any(p => p == null))
            {
                throw new FileLoadException();
            }
        }
        catch (Exception)
        {
            throw new FileLoadException("Invalid user file");
        }
    }

    public static void LoadCurrentUserSession(string line, Product[] products, User[] users)
    {
        string[] input = lineToArray(line);
        try
        {
            User user = Array.Find(users, u => u.ID == int.Parse(input[0]))!;
            Product product = Array.Find(products, p => p.ID == int.Parse(input[1]))!;
            if (user == null || product == null)
            {
                throw new FileLoadException();
            }
            user.CurrentSession = product;
        }
        catch (Exception)
        {
            throw new FileLoadException("Invalid current user session file");
        }
    }

    public void DeterminePriceSensitivity(Product[] products, double mean, double standardDeviation) // requires more testing
    {
        var purchasedPrices = Vector.Create(Purchased.Select(p => (double)p.Price).ToArray());
        if (purchasedPrices.Count() < 2) return;
        OneSampleZTest cheapTest = new OneSampleZTest(purchasedPrices, mean, standardDeviation, HypothesisType.OneTailedLower);
        // WriteLine("Test statistic: {0:F4}", cheapTest.Statistic);
        // WriteLine("P-value:        {0:F4}", cheapTest.PValue);
        // WriteLine("Significance level:     {0:F2}", cheapTest.SignificanceLevel);
        // WriteLine("Reject null hypothesis? {0}", cheapTest.Reject() ? "yes" : "no");
        if (cheapTest.Reject(.125))
        {
            PriceSensitivity = "cheap";
            return;
        }
        OneSampleZTest expensiveTest = new OneSampleZTest(purchasedPrices, mean, standardDeviation, HypothesisType.OneTailedUpper);
        if (expensiveTest.Reject(.125))
        {
            PriceSensitivity = "expensive";
        }
    }

    public void CalculatePriceScore(Product[] products, decimal maxPrice, decimal minPrice)
    {
        int factor = PriceSensitivity switch
        {
            "cheap" => -1,
            "expensive" => 1,
            _ => 0
        };
        decimal divisor = (maxPrice != minPrice) ? maxPrice - minPrice : 1; // to avoid division by 0
        foreach (Product product in products)
        {
            PriceScore.Add(product, (double)(5 * factor * (product.Price - minPrice) / divisor + ((PriceSensitivity == "cheap") ? 5 : 0)));
        }
    }

    public void DetermineYearSensitivity(Product[] products, double mean, double standardDeviation) // requires more testing
    {
        var purchasedYears = Vector.Create(Purchased.Select(p => (double)p.Year).ToArray());
        if (purchasedYears.Count() < 2) return;
        OneSampleZTest cheapTest = new OneSampleZTest(purchasedYears, mean, standardDeviation, HypothesisType.OneTailedLower);
        // WriteLine("Test statistic: {0:F4}", cheapTest.Statistic);
        // WriteLine("P-value:        {0:F4}", cheapTest.PValue);
        // WriteLine("Significance level:     {0:F2}", cheapTest.SignificanceLevel);
        // WriteLine("Reject null hypothesis? {0}", cheapTest.Reject() ? "yes" : "no");
        if (cheapTest.Reject(.125))
        {
            YearSensitivity = "old";
            return;
        }
        OneSampleZTest expensiveTest = new OneSampleZTest(purchasedYears, mean, standardDeviation, HypothesisType.OneTailedUpper);
        if (expensiveTest.Reject(.125))
        {
            YearSensitivity = "new";
        }
    }

    public void CalculateYearScore(Product[] products, decimal maxYear, decimal minYear)
    {
        int factor = YearSensitivity switch
        {
            "old" => -1,
            "new" => 1,
            _ => 0
        };
        decimal divisor = (maxYear != minYear) ? maxYear - minYear : 1; // to avoid division by 0
        foreach (Product product in products)
        {
            YearScore.Add(product, (double)(5 * factor * (product.Year - minYear) / divisor + ((YearSensitivity == "old") ? 5 : 0)));
        }
    }

    public void CalculateGenrePreference(string[] genres)
    {
        double divisor =
        (
            0.5 * Viewed.Count()
            + 0.5 * Purchased.Count()
            + ((CurrentSession is not null) ? 5 : 0)
        );
        if (divisor == 0) divisor = 1; // to avoid division by 0
        GenrePreference = genres.ToDictionary(
            genre => genre,
            genre => (double)
            (
                (0.5 * Viewed.Count(product => product.Keywords.Contains(genre))
                + 0.5 * Purchased.Count(product => product.Keywords.Contains(genre))
                + ((CurrentSession is not null && CurrentSession.Keywords.Contains(genre)) ? 5 : 0))
            ) / divisor
        );
    }

    public void CalculateGenreScore(Product[] products)
    {
        GenreScore = products.ToDictionary(
            product => product,
            product => 5 * product.Keywords.Select(genre => GenrePreference![genre]).Sum() / product.Keywords.Count()
        );
    }

    public void CalculateOverallScore(Product[] products)
    {
        OverallScore = products.ToDictionary(
            product => product,
            product => (
                product.Rating
                + PriceScore![product]
                + YearScore![product]
                + GenreScore![product]
                + product.ViewersInCommonScore![CurrentSession!] // other way around?
            )
        );
        // foreach (Product product in products)
        // {
        //     WriteLine($"{this.Name,-8} {product.Name,-44} {(double)product.Rating,-5} {PriceScore![product],-20} {YearScore![product],-20} {GenreScore![product],-20} {product.ViewersInCommonScore![CurrentSession!],-5}");
        // }
    }
}

public class Auxiliary
{
    public static IEnumerable<string> fileLines(string fileName)
    {
        return File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(), fileName));
    }

    public static string[] lineToArray(string line)
    {
        return line.Split(',').Select(s => s.Trim()).ToArray();
    }
}