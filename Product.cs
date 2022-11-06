using static Shared.IO;

namespace Shared;

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