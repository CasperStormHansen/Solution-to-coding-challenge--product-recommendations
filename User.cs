using Extreme.Mathematics;
using Extreme.Statistics.Tests;
using static Shared.Helper;

namespace Shared;

public class User
{
    public readonly int ID;

    public readonly string Name;

    public readonly Product[] Viewed, Purchased;

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

    public void DeterminePriceSensitivity(Product[] products, double mean, double standardDeviation)
    {
        double[] purchasedPrices = Purchased.Select(p => (double)p.Price).ToArray();
        PriceSensitivity = Sensitivity(purchasedPrices, mean, standardDeviation);

        LogOutput($"\n*** {Name} ***\n");
        LogOutput(PriceSensitivity switch
        {
            -1 => "Foretrækker bilige film",
            1 => "Foretrækker dyre film",
            _ => "Der er ikke evidens for prisfølsomhed"
        }
        );
    }

    public void DetermineYearSensitivity(Product[] products, double mean, double standardDeviation)
    {
        double[] purchasedYears = Purchased.Select(p => (double)p.Year).ToArray();
        YearSensitivity = Sensitivity(purchasedYears, mean, standardDeviation);

        LogOutput(YearSensitivity switch
        {
            -1 => "Foretrækker gamle film",
            1 => "Foretrækker nye film",
            _ => "Der er ikke evidens for en alderspræference"
        }
        );
    }

    int Sensitivity(double[] sample, double mean, double standardDeviation)
    {
        if (sample.Count() < 2) return 0;
        var sampleVector = Vector.Create(sample);
        OneSampleZTest lowSampleMeanTest = new OneSampleZTest(sampleVector, mean, standardDeviation, HypothesisType.OneTailedLower);
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
            genre =>
            (
                0.5 * Viewed.Count(product => product.Keywords.Contains(genre))
                + 0.5 * Purchased.Count(product => product.Keywords.Contains(genre))
                + ((CurrentSession is not null && CurrentSession.Keywords.Contains(genre)) ? 5 : 0)
            ) / divisor
        );

        LogOutput($"\nGenrepræferencer");
        LogOutput("---------------------");
        foreach (string genre in GenrePreference.Keys)
        {
            LogOutput($"{genre,-15}  {GenrePreference[genre],4:N2}");
        }
    }

    public void CalculateGenreScore(Product[] products)
    {
        Dictionary<Product, double> tentativeGenreScore = products.ToDictionary(
            product => product,
            product => 5 * product.Keywords.Select(genre => GenrePreference![genre]).Sum() / product.Keywords.Count()
        );
        GenreScore = MultiplySoMaxIsFive(tentativeGenreScore);
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
                + CurrentSession!.BuyersInCommonScore![product] // score component 5
            )
        );

        LogOutput($"\n{"ID",2}  {"Titel",-44}  {"(1)",3:N1}  {"(2)",3:N1}  {"(3)",3:N1}  {"(4)",3:N1}  {"(5)",3:N1}  {"(T)",4:N1}");
        LogOutput("-------------------------------------------------------------------------------");
        foreach (Product product in products)
        {
            LogOutput($"{product.ID,2}  {product.Name,-44}  {product.Rating,3:N1}  {PriceScore![product],3:N1}  {YearScore![product],3:N1}  {GenreScore![product],3:N1}  {CurrentSession!.BuyersInCommonScore![product],3:N1}  {OverallScore![product],4:N1}");
        }
    }
}