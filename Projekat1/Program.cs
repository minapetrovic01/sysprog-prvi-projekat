using Projekat1;

internal class Program
{
    private static readonly string _urlServer = "http://localhost";
    private static readonly int _portServer = 8080;
    private static readonly string _rootLocation = "../../root";
    private static void Main(string[] args)
    {
        
        try
        {
            WebServer server = new WebServer(_urlServer, _rootLocation, _portServer, 16);
            server.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error when running server!");
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            Console.WriteLine("Done!");
        }
    }
}