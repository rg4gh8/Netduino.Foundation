using System.Threading;
using Microsoft.SPOT.Hardware;

namespace Netduino.Foundation.Displays
{
    public class ST7735 : DisplayTFTSPIBase
    {
        private DisplayType displayType;

        private byte _xOffset;
        private byte _yOffset;

        private ST7735() { }

        public ST7735(Cpu.Pin chipSelectPin, Cpu.Pin dcPin, Cpu.Pin resetPin,
            uint width, uint height,
            SPI.SPI_module spiModule = SPI.SPI_module.SPI1,
            uint speedKHz = 9500, 
            DisplayType displayType = DisplayType.ST7735R) : base(chipSelectPin, dcPin, resetPin, width, height, spiModule, speedKHz)
        {
            this.displayType = displayType;

            Initialize();
        }

        public enum DisplayType
        {
            ST7735R,
            ST7735R_GreenTab,
            ST7735R_BlackTab,
            ST7735R_128x128,
            ST7735R_144x144,
            ST7735R_80x160,
            ST7735B, //done
        }

        new public enum LcdCommand : byte
        {
            NOP = 0x0,
            SWRESET = 0x01,
            RDDID = 0x04,
            RDDST = 0x09,
            SLPIN = 0x10,
            SLPOUT = 0x11,
            PTLON = 0x12,
            NORON = 0x13,
            INVOFF = 0x20,
            INVON = 0x21,
            DISPOFF = 0x28,
            DISPON = 0x29,
            CASET = 0x2A,
            RASET = 0x2B,
            RAMWR = 0x2C,
            RAMRD = 0x2E,
            COLMOD = 0x3A,
            MADCTL = 0x36,
            MADCTL_MY = 0x80,
            MADCTL_MX = 0x40,
            MADCTL_MV = 0x20,
            MADCTL_ML = 0x10,
            MADCTL_RGB = 0x00,
            MADCTL_BGR = 0x08,
            FRMCTR1 = 0xB1,
            FRMCTR2 = 0xB2,
            FRMCTR3 = 0xB3,
            INVCTR = 0xB4,
            DISSET5 = 0xB6,
            PWCTR1 = 0xC0,
            PWCTR2 = 0xC1,
            PWCTR3 = 0xC2,
            PWCTR4 = 0xC3,
            PWCTR5 = 0xC4,
            VMCTR1 = 0xC5,
            RDID1 = 0xDA,
            RDID2 = 0xDB,
            RDID3 = 0xDC,
            RDID4 = 0xDD,
            PWCTR6 = 0xFC,
            GMCTRP1 = 0xE0,
            GMCTRN1 = 0xE1
        }

        protected void SendCommand(LcdCommand command)
        {
            SendCommand((byte)command);
        }

        protected void SendCommand(LcdCommand command, byte[] data)
        {
            SendCommand((byte)command);
            SendData(data);
        }

        protected override void Initialize()
        {
            resetPort.Write(true);
            Thread.Sleep(50);
            resetPort.Write(false);
            Thread.Sleep(50);
            resetPort.Write(true);
            Thread.Sleep(50);

            _xOffset = _yOffset = 0;

            if (displayType == DisplayType.ST7735B)
            {
                Init7735B();
                SetAddressWindow(0, 0, (byte)(_width - 1), (byte)(_height - 1));
                return;
            }

            CommonInit();

            if (displayType == DisplayType.ST7735R_GreenTab)
                Init7735RGreen();
            else if (displayType == DisplayType.ST7735R_144x144)
                Init7735RGreen144x144();
            else if (displayType == DisplayType.ST7735R_80x160)
                Init7735RGreen80x160();
            else
                Init7735RRed();

            Init7735REnd();

            if(displayType == DisplayType.ST7735R_80x160 || 
                displayType == DisplayType.ST7735R_BlackTab)
            {
                SendCommand(LcdCommand.MADCTL, new byte[] { 0xC0 });
                SendCommand(LcdCommand.INVOFF);
            }

            SetAddressWindow(0, 0, (byte)(_width - 1), (byte)(_height - 1));

            dataCommandPort.Write(Data);
        }

        protected void CommonInit()
        {
            SendCommand(LcdCommand.SWRESET);
            DelayMs(150);
            SendCommand(LcdCommand.SLPOUT);
            DelayMs(150);
            SendCommand(LcdCommand.FRMCTR1);  // frame rate control - normal mode
            SendData(new byte[] { 0x01, 0x2C, 0x2D });// frame rate = fosc / (1 x 2 + 40) * (LINE + 2C + 2D)

            SendCommand(LcdCommand.FRMCTR2);  // frame rate control - idle mode
            dataCommandPort.Write(Data);
            Write(0x01);  // frame rate = fosc / (1 x 2 + 40) * (LINE + 2C + 2D)
            Write(0x2C);
            Write(0x2D);

            SendCommand(LcdCommand.FRMCTR3);  // frame rate control - partial mode
            dataCommandPort.Write(Data);
            Write(0x01); // dot inversion mode
            Write(0x2C);
            Write(0x2D);
            Write(0x01); // line inversion mode
            Write(0x2C);
            Write(0x2D);

            SendCommand(LcdCommand.INVCTR);  // display inversion control
            SendData(0x07);  // no inversion
    
            SendCommand(LcdCommand.PWCTR1);  // power control
            dataCommandPort.Write(Data);
            Write(0xA2);
            Write(0x02);      // -4.6V
            Write(0x84);      // AUTO mode

            SendCommand(LcdCommand.PWCTR2);  // power control
            SendData(0xC5);      // VGH25 = 2.4C VGSEL = -10 VGH = 3 * AVDD

            SendCommand(LcdCommand.PWCTR3);  // power control
            dataCommandPort.Write(Data);
            Write(0x0A);      // Opamp current small 
            Write(0x00);      // Boost frequency

            SendCommand(LcdCommand.PWCTR4);  // power control
            dataCommandPort.Write(Data);
            Write(0x8A);      // BCLK/2, Opamp current small & Medium low
            Write(0x2A);

            SendCommand(LcdCommand.PWCTR5);  // power control
            dataCommandPort.Write(Data);
            Write(0x8A);
            Write(0xEE);

            SendCommand(LcdCommand.VMCTR1);  // power control
            dataCommandPort.Write(Data);
            Write(0x0E);

            SendCommand(LcdCommand.MADCTL);  // memory access control (directions)
            dataCommandPort.Write(Data);
            Write(0xC8);  // row address/col address, bottom to top refresh

            SendCommand(LcdCommand.COLMOD);  // set color mode
            dataCommandPort.Write(Data);
            Write(0x05);  // 16-bit color

        }

        protected void Init7735B()
        {
            SendCommand(LcdCommand.SWRESET);
            DelayMs(150);
            SendCommand(LcdCommand.SLPOUT);
            DelayMs(150);

            SendCommand(LcdCommand.COLMOD);  // set color mode
            dataCommandPort.Write(Data);
            Write(0x05);  // 16-bit color

            SendCommand(LcdCommand.FRMCTR1);  // frame rate control - normal mode
            SendData(new byte[] { 0x00, 0x06, 0x03, 10 });// frame rate = fosc / (1 x 2 + 40) * (LINE + 2C + 2D)

            SendCommand(LcdCommand.MADCTL);  // memory access control (directions)
            SendData(0xC8);  // row address/col address, bottom to top refresh

            SendCommand(LcdCommand.DISSET5);
            SendData(new byte[] { 0x15, 0x02 });

            SendCommand(LcdCommand.INVCTR);  // display inversion control
            SendData(0x07);  // no inversion

            SendCommand(LcdCommand.PWCTR1);  // power control
            dataCommandPort.Write(Data);
            Write(0x02);
            Write(0x70);
            Write(10);

            SendCommand(LcdCommand.PWCTR2);  // power control
            SendData(0xC5);      // VGH25 = 2.4C VGSEL = -10 VGH = 3 * AVDD

            SendCommand(LcdCommand.PWCTR3);  // power control
            dataCommandPort.Write(Data);
            Write(0x01);      // Opamp current small 
            Write(0x02);      // Boost frequency

            SendCommand(LcdCommand.VMCTR1);  // power control
            SendData(new byte[] { 0x3C, 0x38, 10 });

            SendCommand(LcdCommand.PWCTR6);
            SendData(new byte[] { 0x11, 0x15 });

            SendCommand(LcdCommand.GMCTRP1);
            SendData(new byte[]
            {
                0x09, 0x16, 0x09, 0x20, 0x21, 0x1B, 0x13, 0x19,
                0x17, 0x15, 0x1E, 0x2B, 0x04, 0x05, 0x02, 0x0E
            });

            SendCommand(LcdCommand.GMCTRN1);
            SendData(new byte[]
            {
                0x0B, 0x14, 0x08, 0x1E, 0x22, 0x1D, 0x18, 0x1E,
                0x1B, 0x1A, 0x24, 0x2B, 0x06, 0x06, 0x02, 0x0F,
            });

            SendCommand(LcdCommand.CASET);
            SendData(new byte[]
            {
                0x00, 0x02,             //     XSTART = 2
                0x00, 0x81,             //     XEND = 129
            });

            SendCommand(LcdCommand.RASET);
            SendData(new byte[]
            {
                0x00, 0x02,             //     XSTART = 1
                0x00, 0x81,             //     XEND = 160
            });

            SendCommand(LcdCommand.NORON);
            SendCommand(LcdCommand.DISPON);

            DelayMs(500);
        }

        protected void Init7735RGreen()
        {
            SendCommand(LcdCommand.CASET, new byte[] { 0x00, 0x02, 0x00, 0x7F + 0x02 });
            SendCommand(LcdCommand.RASET, new byte[] { 0x00, 0x01, 0x00, 0x9F + 0x01 });

            _xOffset = 1;
            _yOffset = 2;
        }

        protected void Init7735RRed()
        {
            SendCommand(LcdCommand.CASET, new byte[] { 0x00, 0x00, 0x00, 0x7F });
            SendCommand(LcdCommand.RASET, new byte[] { 0x00, 0x00, 0x00, 0x9F });
        }

        protected void Init7735RGreen144x144()
        {
            SendCommand(LcdCommand.CASET, new byte[] { 0x00, 0x00, 0x00, 0x7F });
            SendCommand(LcdCommand.RASET, new byte[] { 0x00, 0x00, 0x00, 0x7F });

            _xOffset = 2;
            _yOffset = 1;
        }
        protected void Init7735RGreen80x160()
        {
            SendCommand(LcdCommand.CASET, new byte[] { 0x00, 0x00, 0x00, 0x7F });
            SendCommand(LcdCommand.RASET, new byte[] { 0x00, 0x00, 0x00, 0x9F });

            _xOffset = 26;
            _yOffset = 1;
        }

        protected void Init7735REnd ()
        {
            SendCommand(LcdCommand.GMCTRP1);
            SendData(new byte[]
            {
                0x02, 0x1c, 0x07, 0x12, 0x37, 0x32, 0x29, 0x2d,
                0x29, 0x25, 0x2B, 0x39, 0x00, 0x01, 0x03, 0x10,
            });

            SendCommand(LcdCommand.GMCTRN1);
            SendData(new byte[]
            {
                0x03, 0x1d, 0x07, 0x06, 0x2E, 0x2C, 0x29, 0x2D,
                0x2E, 0x2E, 0x37, 0x3F, 0x00, 0x00, 0x02, 0x10,
            });

            SendCommand(LcdCommand.NORON);
            Thread.Sleep(50);
            SendCommand(LcdCommand.DISPON);
            Thread.Sleep(10);
        }


        private void SetAddressWindow(byte x0, byte y0, byte x1, byte y1)
        {
            x0 += _xOffset;
            y0 += _yOffset;

            x1 += _xOffset;
            y1 += _yOffset;

            SendCommand(LcdCommand.CASET);  // column addr set
            dataCommandPort.Write(Data);
            Write(0x00);
            Write(x0);   // XSTART 
            Write(0x00);
            Write(x1);   // XEND

            SendCommand(LcdCommand.RASET);  // row addr set
            dataCommandPort.Write(Data);
            Write(0x00);
            Write(y0);    // YSTART
            Write(0x00);
            Write(y1);    // YEND

            SendCommand(LcdCommand.RAMWR);  // write to RAM
        }
    }
}