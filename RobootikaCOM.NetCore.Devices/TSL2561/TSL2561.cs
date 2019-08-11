using System;
using System.Device.I2c;

namespace RobootikaCOM.NetCore.Devices
{
    public class TSL2561
    {
        // TSL Address Constants
        public const int I2C_ADDR_0_0x29 = 0x29;    // address with '0' shorted on board 
        public const int I2C_ADDR_0x39 = 0x39;      // default address 
        public const int I2C_ADDR_1_0x49 = 0x49;    // address with '1' shorted on board 

        private const int TSL2561_CMD = 0x80;
        private const int TSL2561_CMD_CLEAR = 0xC0;
        // TSL Registers
        private const int TSL2561_REG_CONTROL = 0x00;
        private const int TSL2561_REG_TIMING = 0x01;
        private const int TSL2561_REG_THRESH_L = 0x02;
        private const int TSL2561_REG_THRESH_H = 0x04;
        private const int TSL2561_REG_INTCTL = 0x06;
        private const int TSL2561_REG_ID = 0x0A;
        private const int TSL2561_REG_DATA_0 = 0x0C;
        private const int TSL2561_REG_DATA_1 = 0x0E;

        //public const int TSL2561_GAIN_1X = 0x00;
        //public const int TSL2561_GAIN_16X = 0x10;

        public const int INTEGRATIONTIME_13MS = 0x00;
        public const int INTEGRATIONTIME_101MS = 0x01;
        public const int INTEGRATIONTIME_402MS = 0x02;

        //default values
        private const bool gainDefault = false;
        private const int integrationTimeDefault = INTEGRATIONTIME_101MS;

        private I2cDevice I2C;
        private int I2C_ADDRESS { get; set; } = I2C_ADDR_0x39;
        private uint MS { get; set; } = 101;
        private bool gainSet { get; set; } = gainDefault;
        public TSL2561(int i2cAddress = I2C_ADDR_0x39)
        {
            I2C_ADDRESS = i2cAddress;
            Initialise();
        }
        public static bool IsInitialised { get; private set; } = false;
        private void Initialise()
        {
            if (!IsInitialised)
            {
                EnsureInitialized();
            }
        }
        private void EnsureInitialized()
        {
            try
            {
                var i2cSettings = new I2cConnectionSettings(1, I2C_ADDRESS);
                I2C = I2cDevice.Create(i2cSettings);

                PowerUp();
                SetTiming();
                IsInitialised = true;
            }
            catch (Exception ex)
            {
                throw new Exception("I2C Initialization Failed", ex);
            }
        }
        // TSL2561 Sensor Power up
        private void PowerUp()
        {
            write8(TSL2561_REG_CONTROL, 0x03);
        }
        // TSL2561 Sensor Power down
        private void PowerDown()
        {
            write8(TSL2561_REG_CONTROL, 0x00);
        }
        // Retrieve TSL ID
        public byte GetId()
        {
            return I2CRead8(TSL2561_REG_ID);
        }
        public void SetTiming(Boolean gain = gainDefault, byte integrationTime = integrationTimeDefault)
        {
            gainSet = gain;
            MS = (uint)setTiming(gain, integrationTime);
        }
        // Set TSL2561 Timing and return the MS
        private int setTiming(Boolean gain = gainDefault, byte integrationTime = integrationTimeDefault)
        {
            int ms = 0;
            switch (integrationTime)
            {
                case INTEGRATIONTIME_13MS: ms = 14; break;
                case INTEGRATIONTIME_101MS: ms = 101; break;
                case INTEGRATIONTIME_402MS: ms = 402; break;
                default: ms = 101; break;
            }
            int timing = I2CRead8(TSL2561_REG_TIMING);
            // Set gain (0 or 1) 
            if (gain)
                timing |= 0x10;
            else
                timing &= (~0x10);

            // Set integration time (0 to 3) 
            timing &= ~0x03;
            timing |= (integrationTime & 0x03);

            write8(TSL2561_REG_TIMING, (byte)timing);
            return ms;
        }
        // Get channel data
        private uint[] GetData()
        {
            uint[] Data = new uint[2];
            Data[0] = I2CRead16(TSL2561_REG_DATA_0);
            Data[1] = I2CRead16(TSL2561_REG_DATA_1);
            return Data;
        }

        // Calculate Lux
        public double GetLux()
        {
            bool gain = gainSet;
            uint ms = MS;

            Initialise();
            uint[] Data = GetData();
            uint CH0 = Data[0];
            uint CH1 = Data[1];

            double ratio, d0, d1;
            double lux = 0.0;

            // Determine if either sensor saturated (0xFFFF)
            if ((CH0 == 0xFFFF) || (CH1 == 0xFFFF))
            {
                lux = 0.0;
                return lux;
            }
            // Convert from unsigned integer to floating point
            d0 = CH0; d1 = CH1;

            // We will need the ratio for subsequent calculations
            ratio = d1 / d0;

            // Normalize for integration time
            d0 *= (402.0 / ms);
            d1 *= (402.0 / ms);

            // Normalize for gain
            if (!gain)
            {
                d0 *= 16;
                d1 *= 16;
            }
            // Determine lux per datasheet equations:
            if (ratio < 0.5)
                lux = 0.0304 * d0 - 0.062 * d0 * Math.Pow(ratio, 1.4);
            else if (ratio < 0.61)
                lux = 0.0224 * d0 - 0.031 * d1;
            else if (ratio < 0.80)
                lux = 0.0128 * d0 - 0.0153 * d1;
            else if (ratio < 1.30)
                lux = 0.00146 * d0 - 0.00112 * d1;
            else
                lux = 0.0;

            return Math.Round(lux, 2);
        }
        // Write byte
        private void write8(byte addr, byte cmd)
        {
            byte[] Command = new byte[] { (byte)((addr) | TSL2561_CMD), cmd };

            I2C.Write(Command);
        }
        // Read byte
        private byte I2CRead8(byte addr)
        {
            byte[] aaddr = new byte[] { (byte)((addr) | TSL2561_CMD) };
            byte[] data = new byte[1];

            I2C.WriteRead(aaddr, data);

            return data[0];
        }
        // Read integer
        private ushort I2CRead16(byte addr)
        {
            byte[] aaddr = new byte[] { (byte)((addr) | TSL2561_CMD) };
            byte[] data = new byte[2];

            I2C.WriteRead(aaddr, data);

            return (ushort)((data[1] << 8) | (data[0]));
        }
    }
}