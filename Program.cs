using Extreme.Mathematics;
using Extreme.Statistics.Tests;

namespace Solution
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // load data from files
            Product[] products = (
                File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(), "Products.txt")).Select // make helper function
                (
                    line => new Product(
                        line.Split(',').Select(s => s.Trim()).ToArray() // move this inside constructor?
                    )
                )
            ).ToArray();

            User[] users = (
                File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(), "Users.txt")).Select
                (
                    line => new User(
                        line.Split(',').Select(s => s.Trim()).ToArray(),
                        products
                    )
                )
            ).ToArray();

            foreach (string line in File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(), "CurrentUserSession.txt")))
            {
                User.LoadCurrentUserSession(
                    line.Split(',').Select(s => s.Trim()).ToArray(),
                    products,
                    users
                );
            }

            // part one of challenge
            foreach (Product product in products) product.CalculateNumberOfPurchases(users);

            foreach (Product product in products) product.CalculatePopularity();

            Product[] productsByPopularity = products.OrderByDescending(product => product.Popularity).ToArray();

            // what if less than three movies?
            Console.WriteLine($"The three most popular movies are {productsByPopularity[0].Name}, {productsByPopularity[1].Name}, and {productsByPopularity[2].Name}."); // rewrite text
            Console.WriteLine();

            // part two of challenge
            Product.CalculateDescriptiveStats(products);

            // (2)
            foreach (User user in users)
            {
                user.DeterminePriceSensitivity(products, Product.MeanPrice, Product.PriceStandardDeviation);
                // foreach (Product product in products) // each product?
                // {
                user.CalculatePriceScore(products, Product.MaxPrice, Product.MinPrice);
                // }
            }

            // (3)
            foreach (User user in users)
            {
                user.DetermineYearSensitivity(products, Product.MeanYear, Product.YearStandardDeviation);
                // foreach (Product product in products)
                // {
                user.CalculateYearScore(products, Product.MaxYear, Product.MinYear);
                // }
            }

            // (4)
            foreach (User user in users) // one line?
            {
                user.CalculateGenrePreference(Product.Genres!);
                user.CalculateGenreScore(products);
            }

            // (5)
            foreach (Product product in products)
            {
                product.CalculateBuyersInCommonScore(products, users);
            }

            // (Overall score)
            foreach (User user in users)
            {
                if (user.CurrentSession is not null) // also this check above?
                {
                    user.CalculateOverallScore(products);
                    Product[] productsByUserScore = products
                        .Where(product => !user.Purchased.Contains(product) && user.CurrentSession != product)
                        .OrderByDescending(product => user.OverallScore![product])
                        .ToArray();

                    // what if less than three movies?
                    Console.WriteLine($"Hi {user.Name}! You should also check out {productsByUserScore[0].Name}, {productsByUserScore[1].Name}, and {productsByUserScore[2].Name}."); // rewrite text
                    Console.WriteLine();
                }
            }

            Console.WriteLine("The end!");
        }
    }

    public class Product // check conventions for order of members
    {
        public int ID { get; set; } // remove set for some? remove public for some?

        public string Name { get; set; }

        public int Year { get; set; }

        public string[] Keywords { get; set; }

        public decimal Rating { get; set; }

        public decimal Price { get; set; }

        public int NumberOfPurchases { get; set; }

        public static int MaxNumberOfPurchases { get; set; }

        public decimal Popularity { get; set; } // float?

        public static double MeanPrice { get; set; }

        public static double PriceStandardDeviation { get; set; }

        public static decimal MaxPrice { get; set; }

        public static decimal MinPrice { get; set; }

        public static double MeanYear { get; set; }

        public static double YearStandardDeviation { get; set; }

        public static decimal MaxYear { get; set; }

        public static decimal MinYear { get; set; } // merge lines of same type and change to double

        public static string[] Genres { get; set; } = new string[] { };// initialize more as empty and remove"?"

        public Dictionary<Product, double>? ViewersInCommonScore { get; set; } // not static, move up?

        public Product(string[] input)
        {
            try
            {
                ID = int.Parse(input[0]);
                Name = input[1];
                Year = int.Parse(input[2]);
                Keywords = new string[] { input[3], input[4], input[5], input[6], input[7] }.Where(k => k != "").ToArray();
                Rating = decimal.Parse(input[8]);
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
                Popularity = Rating + 5 * ((decimal)NumberOfPurchases / MaxNumberOfPurchases);
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
        public int ID { get; set; }

        public string Name { get; set; }

        public Product[] Viewed { get; set; }

        public Product[] Purchased { get; set; }

        public Product? CurrentSession { get; set; }

        public string? PriceSensitivity { get; set; }

        public Dictionary<Product, double>? PriceScore { get; set; }

        public string? YearSensitivity { get; set; }

        public Dictionary<Product, double>? YearScore { get; set; }

        public Dictionary<string, double>? GenrePreference { get; set; }

        public Dictionary<Product, double>? GenreScore { get; set; }

        public Dictionary<Product, double>? OverallScore { get; set; }

        public User(string[] input, Product[] products)
        {
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

        public static void LoadCurrentUserSession(string[] input, Product[] products, User[] users)
        {
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
            var purchasedPrices = Vector.Create(Purchased.Select(p => (double)p.Price).ToArray()); // determine type
            if (purchasedPrices.Count() < 2) return;
            OneSampleZTest cheapTest = new OneSampleZTest(purchasedPrices, mean, standardDeviation, HypothesisType.OneTailedLower);
            // Console.WriteLine("Test statistic: {0:F4}", cheapTest.Statistic);
            // Console.WriteLine("P-value:        {0:F4}", cheapTest.PValue);
            // Console.WriteLine("Significance level:     {0:F2}", cheapTest.SignificanceLevel);
            // Console.WriteLine("Reject null hypothesis? {0}", cheapTest.Reject() ? "yes" : "no");
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
            PriceScore = new Dictionary<Product, double> { };
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
            var purchasedYears = Vector.Create(Purchased.Select(p => (double)p.Year).ToArray()); // determine type
            if (purchasedYears.Count() < 2) return;
            OneSampleZTest cheapTest = new OneSampleZTest(purchasedYears, mean, standardDeviation, HypothesisType.OneTailedLower);
            // Console.WriteLine("Test statistic: {0:F4}", cheapTest.Statistic);
            // Console.WriteLine("P-value:        {0:F4}", cheapTest.PValue);
            // Console.WriteLine("Significance level:     {0:F2}", cheapTest.SignificanceLevel);
            // Console.WriteLine("Reject null hypothesis? {0}", cheapTest.Reject() ? "yes" : "no");
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
            YearScore = new Dictionary<Product, double> { };
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
                + ((CurrentSession is not null) ? 5 : 0) // only works if current has not been viewed before
            );
            if (divisor == 0) divisor = 1; // to avoid division by 0; if changed all dividends will be 0 *****copy this to elsewhere*****
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

        public void CalculateGenreScore(Product[] products) // consider a return value
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
                    (double)product.Rating // Rating already double?
                    + PriceScore![product]
                    + YearScore![product]
                    + GenreScore![product] //////
                    + product.ViewersInCommonScore![CurrentSession!] // other way around?
                )
            );
            // foreach (Product product in products)
            // {
            //     Console.WriteLine($"{this.Name,-8} {product.Name,-44} {(double)product.Rating,-5} {PriceScore![product],-20} {YearScore![product],-20} {GenreScore![product],-20} {product.ViewersInCommonScore![CurrentSession!],-5}");
            // }
        }
    }
}

// https://stackoverflow.com/questions/14810444/find-the-second-maximum-number-in-an-array-with-the-smallest-complexity
// https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/ms229043(v=vs.100)?redirectedfrom=MSDN