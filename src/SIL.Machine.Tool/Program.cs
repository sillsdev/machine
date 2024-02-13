using System.Threading.Tasks;

namespace SIL.Machine;

#pragma warning disable CA1724 // Type Names Should Not Match Namespaces - SIL.Program.  Ignore.
public class Program
#pragma warning restore CA1724
{
    public static Task<int> Main(string[] args)
    {
        var app = new App();
        return app.ExecuteAsync(args);
    }
}
