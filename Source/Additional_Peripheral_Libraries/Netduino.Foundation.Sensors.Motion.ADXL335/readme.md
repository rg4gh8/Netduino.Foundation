# ADXL335 - Triple Axis Accelerometer

The ADXL335 is a low power accelerometer capable of measuring +/- 3g of acceleration along three axes.

## Purchasing

The ADXL335 sensor can be purchased on a breakout board from the following suppliers:

* [Adafruit ADXL335](https://www.adafruit.com/product/163)
* [Sparkfun ADXL335](https://www.sparkfun.com/products/9269)

## Hardware

![ADXL335 on Breadboard](ADXL335OnBreadboard.png)

The datasheet notes that bypass capacitors should be installed for the X, Y and Z outputs from the sensor.  Both the Sparkfun and Adafruit breakout boards have `0.1 &mu;F capacitors installed on the board.

## Software

The following code will set up the sensor and display the G force and raw sensor data every 250 milliseconds:

```csharp
using System.Threading;
using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using Netduino.Foundation.Sensors.Motion;

namespace ADXL335Test
{
    public class Program
    {
        public static void Main()
        {
            ADXL335 adxl335 = new ADXL335(AnalogChannels.ANALOG_PIN_A0, AnalogChannels.ANALOG_PIN_A1, AnalogChannels.ANALOG_PIN_A2);
            adxl335.SupplyVoltage = 3.3;
            adxl335.XVoltsPerG = 0.343;
            adxl335.YVoltsPerG = 0.287;
            adxl335.ZVoltsPerG = 0.541;
            while (true)
            {
                ADXL335.Readings acceleration = adxl335.GetAcceleration();
                ADXL335.Readings rawData = adxl335.GetRawSensorData();

                Debug.Print("\n");
                Debug.Print("X: " + acceleration.X.ToString("F2") + ", " + rawData.X.ToString("F2"));
                Debug.Print("Y: " + acceleration.Y.ToString("F2") + ", " + rawData.Y.ToString("F2"));
                Debug.Print("Z: " + acceleration.Z.ToString("F2") + ", " + rawData.Z.ToString("F2"));
                Thread.Sleep(250);
            }
        }
    }
}
```

Note that the voltage per axes have been set for the sensor installed in this setup.

## API

### `Readings` Structure

The `Readings` structure can be used to hold the X, Y and Z acceleration data either in G or as raw sensor readings.  The exact contents will be determined by the method called (see `GetRawSensorData` and `GetAcceleration`).

### Constructor `ADXL335(Cpu.AnalogChannel x, Cpu.AnalogChannel y, Cpu.AnalogChannel z)`

The constructor takes three `Cpu.AnalogChannel` inputs, one for each axis.  The specified channels will be attached to the respective X, Y, and Z readings.

### Properties

#### `SupplyVoltage`

This property holds the supply voltage for the sensor.  By default this will be set to 3.3V when the constructor is executed.  This value will be used to determine the centre point (0g) for the sensor.

#### `XVoltsPerG`, `YVoltsPerG`, `ZVoltsPerG`

These three properties hold the voltage change that will be expected for each 1g of acceleration the sensor experiences.

These are set to the default values from the data sheet (X: 0.325, Y: 0.325, Z: 0.550).  These values should be set in the application following calibration.

### Methods

#### `GetAcceleration`

Get a `Readings` structure holding the acceleration for the X, Y and Z axes.  This method uses the `SupplyVoltage`, `XVoltsPerG`, `YVoltsPerG` and `ZVoltsPerG` properties to determine the current acceleration being experienced.

#### `GetRawSensorData`

This method returns a `Readings` structure that holds the data from the analog inputs for each of the X, Y and Z axes.  These values are in the range 0-1.