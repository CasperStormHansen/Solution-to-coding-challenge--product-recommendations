using Shared;
using static Shared.Helper;

// sets the output mode: default or log mode (if the program is run with the argument "-logmode")
SetMode(args);

// loads data from files into Product and User objects and collects those into two arrays
// the files must have the right names, be in the right format, and be placed in the same folder as this program
Product[] products = FileLines("Products.txt").Select(line => new Product(line)).ToArray();
User[] users = FileLines("Users.txt").Select(line => new User(line, products)).ToArray();
foreach (string line in FileLines("CurrentUserSession.txt")) User.LoadCurrentUserSession(line, products, users);

// calculates scores for part one of the challenge, orders the products by total score, and (if in log mode) outputs log info
foreach (Product product in products) product.CalculateNumberOfPurchases(users);
foreach (Product product in products) product.CalculatePopularity();
Product[] productsByPopularity = products.OrderByDescending(product => product.Popularity).ToArray();

// if in default mode, outputs the user message for part one of the challenge
UserOutput("\nVelkommen til ExperisFlix! De mest populære film lige nu er");
for (int i = 0; i < Math.Min(3, productsByPopularity.Count()); i++)
{
    UserOutput($"  {productsByPopularity[i].Name}");
}
UserOutput();

// calculates product statistics that are used for part two of the challenge
Product.CalculateDescriptiveStats(products);

// calculates score component 5
foreach (Product product in products) product.CalculateBuyersInCommonScore(products, users);

foreach (User user in users)
{
    if (user.CurrentSession is not null)
    {
        // calculates score component 2, and (if in log mode) outputs result of price sensitivity test
        user.DeterminePriceSensitivity(products, Product.MeanPrice, Product.PriceStandardDeviation);
        user.CalculatePriceScore(products, Product.MaxPrice, Product.MinPrice);

        // calculates score component 3, and (if in log mode) outputs result of year sensitivity test
        user.DetermineYearSensitivity(products, Product.MeanYear, Product.YearStandardDeviation);
        user.CalculateYearScore(products, Product.MaxYear, Product.MinYear);

        // calculates score component 4, and (if in log mode) outputs genre preferences
        user.CalculateGenrePreference(Product.Genres);
        user.CalculateGenreScore(products);

        // calculates overall score, outputs log info (if in log mode), and orders the products by total score (excluding those already purchased or currently viewed)
        user.CalculateOverallScore(products);
        Product[] productsByUserScore = products
            .Where(product => !user.Purchased.Contains(product) && user.CurrentSession != product)
            .OrderByDescending(product => user.OverallScore![product])
            .ToArray();

        // if in default mode, outputs the user message for part two of the challenge
        UserOutput($"Hej {user.Name}! Hvis du er interesseret i {user.CurrentSession.Name}, skulle du også ta' at tjekke disse film ud:");
        for (int i = 0; i < Math.Min(3, productsByUserScore.Count()); i++)
        {
            UserOutput($"  {productsByUserScore[i].Name}");
        }
        UserOutput();
    }
}