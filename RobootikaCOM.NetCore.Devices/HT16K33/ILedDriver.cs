namespace RobootikaCOM.NetCore.Devices
{
    public interface ILedDriver
    {
        int PanelsPerFrame { get; }
        void SetBlinkRate(LedDriver.BlinkRate blinkrate);
        void SetBrightness(byte level);
        void SetFrameState(LedDriver.Display state);
        void Write(Pixel[] frame);
        void Write(ulong[] frame);
    }
}
