using System.Threading.Tasks;

namespace SIL.Machine
{
    public class Program
    {
        public static Task<int> Main(string[] args)
        {
            var app = new App();
            return app.ExecuteAsync(args);
        }
    }
}
