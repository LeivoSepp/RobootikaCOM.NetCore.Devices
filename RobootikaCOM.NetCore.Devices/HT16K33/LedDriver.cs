using System;
using System.Collections.Generic;
using System.Text;

namespace RobootikaCOM.NetCore.Devices
{
    public class LedDriver
    {
        public enum Display : byte
        {
            On,
            Off,
        }

        public enum BlinkRate : byte
        {
            Off,
            Fast,
            Medium,
            Slow,
        }
    }
}
