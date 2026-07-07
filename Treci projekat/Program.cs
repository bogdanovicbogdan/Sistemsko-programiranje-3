using System.Threading.Tasks;

namespace Treci_projekat
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new AppHost().RunAsync();
        }
    }
}
