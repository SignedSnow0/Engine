using Engine;

namespace Gioco
{
    class Program
    {
        static void Main(string[] args)
        {
            using(CoreEngine gioco = new CoreEngine())
            {
                gioco.Run(60.0);                
            }
        }
    }
}
