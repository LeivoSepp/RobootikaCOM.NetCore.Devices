using System.Threading.Tasks;

namespace RobootikaCOM.NetCore.Devices
{
    static class Util
    {
        static public void Delay(int milliseconds)
        {
            Task.Delay(milliseconds).Wait();
        }
    }
}
