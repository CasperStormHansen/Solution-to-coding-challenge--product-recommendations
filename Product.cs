using static Shared.Helper;

namespace Shared;

public class Product
{
    public readonly int ID, Year;

    public int NumberOfPurchases;

    public readonly string Name;

    public readonly decimal Price;

    public readonly double Rating;

    public double Popularity;

    public Dictionary<Product, double>? BuyersInCommonScore;

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
        NumberOfPurchases = users.Count(user => (user.Purchased.Contains(this)));
        if (NumberOfPurchases > MaxNumberOfPurchases) MaxNumberOfPurchases = NumberOfPurchases;
    }

    public void CalculatePopularity()
    {
        if (MaxNumberOfPurchases == 0) MaxNumberOfPurchases = 1; // to avoid division by 0 (here and everywhere else, if divisor is changed like this, every dividend is 0, so the the desired result is achieved)
        double purchaseScore = 5 * ((double)NumberOfPurchases / MaxNumberOfPurchases);
        Popularity = Rating + purchaseScore;
        LogOutput($"{ID,2}  {Name,-44}  {Rating,3:N1}  {purchaseScore,3:N1}  {Popularity,4:N1}");
    }

    public static void CalculateDescriptiveStats(Product[] products)
    {
        MaxPrice = products.Select(p => p.Price).Max();
        MinPrice = products.Select(p => p.Price).Min();
        MeanPrice = products.Select(p => (double)p.Price).Average();
        double sum1 = products.Sum(d => ((double)d.Price - MeanPrice) * ((double)d.Price - MeanPrice));
        PriceStandardDeviation = Math.Sqrt(sum1 / products.Count());

        MaxYear = products.Select(p => p.Year).Max();
        MinYear = products.Select(p => p.Year).Min();
        MeanYear = products.Select(p => (double)p.Year).Average();
        double sum2 = products.Sum(d => (d.Year - MeanYear) * (d.Year - MeanYear));
        YearStandardDeviation = Math.Sqrt(sum2 / products.Count());

        foreach (Product product in products) Genres = Genres.Union(product.Keywords).ToArray();
    }

    public void CalculateBuyersInCommonScore(Product[] products, User[] users)
    {
        Dictionary<Product, double> tentativeBuyersInCommonScore = products.ToDictionary(
            product => product,
            product => (double)users.Count(user => (user.Purchased.Contains(product) && user.Purchased.Contains(this) && product != this))
        );
        BuyersInCommonScore = MultiplySoMaxIsFive(tentativeBuyersInCommonScore);
    }

    public override int GetHashCode() // allows products to be keys in dictionaries
    {
        return ID;
    }
}