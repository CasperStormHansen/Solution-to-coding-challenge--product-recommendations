using Extreme.Mathematics;
using Extreme.Statistics.Tests;
using static System.Console;
using static Helper;

// *load data from files*
Product[] products = FileLines("Products.txt").Select(line => new Product(line)).ToArray();
User[] users = FileLines("Users.txt").Select(line => new User(line, products)).ToArray();
foreach (string line in FileLines("CurrentUserSession.txt")) User.LoadCurrentUserSession(line, products, users);

// *part one of challenge*
foreach (Product product in products) product.CalculateNumberOfPurchases(users);
foreach (Product product in products) product.CalculatePopularity();
Product[] productsByPopularity = products.OrderByDescending(product => product.Popularity).ToArray();

// output
WriteLine("\nVelkommen til ExperisFlix! De mest populære film lige nu er");
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
    if (user.CurrentSession is not null)
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

public class Product
{
    public int ID, Year, NumberOfPurchases;

    public string Name;

    public decimal Price;

    public double Rating, Popularity;

    public Dictionary<Product, double>? ViewersInCommonScore;

    public string[] Keywords;

    public static int MaxNumberOfPurchases, MaxYear, MinYear;

    public static decimal MaxPrice, MinPrice;

    public static double MeanPrice, PriceStandardDeviation, MeanYear, YearStandardDeviation;

    public static string[] Genres = new string[] { };

    public Product(string line)
    {
        string[] input = LineToArray(line);
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
        if (MaxNumberOfPurchases > 0) MaxNumberOfPurchases = 1; // to avoid division by 0 (here and everywhere else, if divisor is changed like this, every dividend is 0, so the the desired result is achieved)
        Popularity = Rating + 5 * ((double)NumberOfPurchases / MaxNumberOfPurchases);
    }

    public static void CalculateDescriptiveStats(Product[] products)
    {
        MaxPrice = products.Select(p => p.Price).Max();
        MinPrice = products.Select(p => p.Price).Min();
        MeanPrice = products.Select(p => (double)p.Price).Average();
        double sum1 = (double)products.Sum(d => ((double)d.Price - MeanPrice) * ((double)d.Price - MeanPrice));
        PriceStandardDeviation = Math.Sqrt(sum1 / products.Count());

        MaxYear = products.Select(p => p.Year).Max();
        MinYear = products.Select(p => p.Year).Min();
        MeanYear = products.Select(p => (double)p.Year).Average();
        double sum2 = (double)products.Sum(d => (d.Year - MeanYear) * (d.Year - MeanYear));
        YearStandardDeviation = Math.Sqrt(sum2 / products.Count());

        foreach (Product product in products) Genres = Genres.Union(product.Keywords).ToArray();
    }

    public void CalculateBuyersInCommonScore(Product[] products, User[] users) // do test with user histories that result in non-extreme values
    {
        Dictionary<Product, int> preNormalizedBuyersInCommonScore = products.ToDictionary(
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

    public override int GetHashCode() // allows products to be keys in dictionaries
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

    public int PriceSensitivity, YearSensitivity;

    public Dictionary<Product, double> PriceScore = new Dictionary<Product, double> { }, YearScore = new Dictionary<Product, double> { };

    public Dictionary<string, double>? GenrePreference;

    public Dictionary<Product, double>? GenreScore, OverallScore;

    public User(string line, Product[] products)
    {
        string[] input = LineToArray(line);
        try
        {
            ID = int.Parse(input[0]);
            Name = input[1];
            Viewed = SemicolonIDListToProductArray(input[2], products);
            Purchased = SemicolonIDListToProductArray(input[3], products);
            if (Viewed.Any(p => p == null) || Purchased.Any(p => p == null)) throw new FileLoadException();
        }
        catch (Exception)
        {
            throw new FileLoadException("Invalid user file");
        }
    }

    public static void LoadCurrentUserSession(string line, Product[] products, User[] users)
    {
        string[] input = LineToArray(line);
        try
        {
            User user = Array.Find(users, u => u.ID == int.Parse(input[0]))!;
            Product product = Array.Find(products, p => p.ID == int.Parse(input[1]))!;
            if (user == null || product == null) throw new FileLoadException();
            user.CurrentSession = product;
        }
        catch (Exception)
        {
            throw new FileLoadException("Invalid current user session file");
        }
    }

    public void DeterminePriceSensitivity(Product[] products, double mean, double standardDeviation) // requires more testing
    {
        double[] purchasedPrices = Purchased.Select(p => (double)p.Price).ToArray();
        PriceSensitivity = Sensitivity(purchasedPrices, mean, standardDeviation); // -1 means preference for cheap, 1 means preference for expensive, 0 means insufficient evidence for either
    }

    public void DetermineYearSensitivity(Product[] products, double mean, double standardDeviation) // requires more testing
    {
        double[] purchasedYears = Purchased.Select(p => (double)p.Year).ToArray();
        YearSensitivity = Sensitivity(purchasedYears, mean, standardDeviation); // -1 means preference for old, 1 means preference for new, 0 means insufficient evidence for either
    }

    int Sensitivity(double[] sample, double mean, double standardDeviation)
    {
        if (sample.Count() < 2) return 0;
        var sampleVector = Vector.Create(sample);
        OneSampleZTest lowSampleMeanTest = new OneSampleZTest(sampleVector, mean, standardDeviation, HypothesisType.OneTailedLower);
        // WriteLine("Test statistic: {0:F4}", cheapTest.Statistic);
        // WriteLine("P-value:        {0:F4}", cheapTest.PValue);
        // WriteLine("Significance level:     {0:F2}", cheapTest.SignificanceLevel);
        // WriteLine("Reject null hypothesis? {0}", cheapTest.Reject() ? "yes" : "no");
        if (lowSampleMeanTest.Reject(.125)) return -1; // the sample's mean is lower than the population's mean to a statistically significant extent
        OneSampleZTest highSampleMeanTest = new OneSampleZTest(sampleVector, mean, standardDeviation, HypothesisType.OneTailedUpper);
        if (highSampleMeanTest.Reject(.125)) return 1; // the sample's mean is higher than the population's mean to a statistically significant extent
        return 0; // insufficient evidence to conclude that the difference between the sample's mean and the populations's mean is not just due to chance
    }

    public void CalculatePriceScore(Product[] products, decimal maxPrice, decimal minPrice)
    {
        decimal divisor = (maxPrice != minPrice) ? maxPrice - minPrice : 1; // to avoid division by 0
        foreach (Product product in products)
        {
            PriceScore.Add(product, (double)(5 * PriceSensitivity * (product.Price - minPrice) / divisor + ((PriceSensitivity == -1) ? 5 : 0)));
        }
    }

    public void CalculateYearScore(Product[] products, decimal maxYear, decimal minYear)
    {
        decimal divisor = (maxYear != minYear) ? maxYear - minYear : 1; // to avoid division by 0
        foreach (Product product in products)
        {
            YearScore.Add(product, (double)(5 * YearSensitivity * (product.Year - minYear) / divisor + ((YearSensitivity == -1) ? 5 : 0)));
        }
    }

    public void CalculateGenrePreference(string[] genres)
    {
        double divisor =
        (
            0.5 * Viewed.Count()
            + 0.5 * Purchased.Count() // purchased products get weight 1, because every purchased product is also viewed
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
                product.Rating // score component 1
                + PriceScore![product] // score component 2
                + YearScore![product] // score component 3
                + GenreScore![product] // score component 4
                + CurrentSession!.ViewersInCommonScore![product] // score component 5
            )
        );
        // foreach (Product product in products)
        // {
        //     WriteLine($"{this.Name,-8} {product.Name,-44} {(double)product.Rating,-5} {PriceScore![product],-20} {YearScore![product],-20} {GenreScore![product],-20} {product.ViewersInCommonScore![CurrentSession!],-5}");
        // }
    }
}

public class Helper
{
    public static IEnumerable<string> FileLines(string fileName)
    {
        return File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(), fileName));
    }

    public static string[] LineToArray(string line)
    {
        return line.Split(',').Select(s => s.Trim()).ToArray();
    }

    public static Product[] SemicolonIDListToProductArray(string semiColonList, Product[] products)
    {
        if (String.IsNullOrEmpty(semiColonList)) return new Product[0];
        return semiColonList.Split(';').Select(n => Array.Find(products, p => p.ID == int.Parse(n))).ToArray()!;
    }
}

// separate files for each class