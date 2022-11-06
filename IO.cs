namespace Shared;

public class IO
{
    public static bool LogMode;

    public static void SetMode(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8; // allows output with Danish characters

        if (args.Count() == 1 && args[0] == "-logmode")
        {
            LogMode = true;
            LogOutput($"\n{"Popularitet",-48}  {"(1)",3:N1}  {"(2)",3:N1}  {"(T)",4:N1}");
            LogOutput("----------------------------------------------------------------");
        }
    }

    public static void LogOutput(string output = "")
    {
        if (LogMode) Console.WriteLine(output);
    }

    public static void UserOutput(string output = "")
    {
        if (!LogMode) Console.WriteLine(output);
    }

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