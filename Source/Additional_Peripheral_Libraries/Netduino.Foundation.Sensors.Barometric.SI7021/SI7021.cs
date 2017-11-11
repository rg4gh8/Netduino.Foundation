﻿using System;
using System.Threading;
using Netduino.Foundation.Devices;

namespace Netduino.Foundation.Sensors.Barometric
{
    /// <summary>
    ///     Provide access to the SI7021 temperature and humidity sensor.
    /// </summary>
    public class SI7021
    {
        #region Enums

        /// <summary>
        ///     Specific device type / model.
        /// </summary>
        public enum DeviceType
        {
            Unknown = 0x00,
            Si7013 = 0x0d,
            Si7020 = 0x14,
            Si7021 = 0x15,
            EngineeringSample = 0xff
        }

        #endregion Enums

        #region Classes / structures

        /// <summary>
        ///     Device registers.
        /// </summary>
        private static class Registers
        {
            public static readonly byte MeasureHumidityWithHold = 0xe5;
            public static readonly byte MeasureHumidityNoHold = 0xf5;
            public static readonly byte MeasureTemperatureWithHold = 0xe3;
            public static readonly byte MeasureTemperatureNoHold = 0xf3;
            public static readonly byte ReadPreviousTemperatureMeasurement = 0xe0;
            public static readonly byte Reset = 0xfe;
            public static readonly byte WriteUserRegister1 = 0xe6;
            public static readonly byte ReadUserRegister1 = 0xe7;
            public static readonly byte ReadIDFirstBytePart1 = 0xfa;
            public static readonly byte ReadIDFirstBytePart2 = 0x0f;
            public static readonly byte ReadIDSecondBytePart1 = 0xfc;
            public static readonly byte ReadIDSecondBytePart2 = 0xc9;
            public static readonly byte ReadFirmwareRevisionPart1 = 0x84;
            public static readonly byte ReadFirmwareRevisionPart2 = 0xb8;
        }

        #endregion Classes / Structures

        #region Member variables / fields

        /// <summary>
        ///     SI7021 is an I2C device.
        /// </summary>
        private readonly I2CBus _si7021;

        #endregion Member variables / fields

        #region Properties

        /// <summary>
        ///     Relative humidity (percentage)
        /// </summary>
        /// <remarks>
        ///     This value is only valid after a call to Read.
        /// </remarks>
        public float Humidity { get; private set; }

        /// <summary>
        ///     Temperature (degrees C)
        /// </summary>
        /// <remarks>
        ///     This value is only valid after a call to Read.
        /// </remarks>
        public float Temperature { get; private set; }

        /// <summary>
        ///     Serial number of the device.
        /// </summary>
        public ulong SerialNumber { get; private set; }

        /// <summary>
        ///     Device type as extracted from the serial number.
        /// </summary>
        public DeviceType SensorType { get; private set; }

        /// <summary>
        ///     Firmware revision of the sensor.
        /// </summary>
        public byte FirmwareRevision { get; private set; }

        /// <summary>
        ///     Get / Set the resolution of the sensor.
        /// </summary>
        public byte Resolution
        {
            get
            {
                var register = _si7021.ReadRegister(Registers.ReadUserRegister1);
                var resolution = (byte) ((register >> 7) | (register & 0x01));
                return resolution;
            }
            set
            {
                if (value > 3)
                {
                    throw new ArgumentException("Resolution should be in the range 0-3");
                }
                var register = _si7021.ReadRegister(Registers.ReadUserRegister1);
                register &= 0x7e;
                var mask = (byte) (value & 0x01);
                mask |= (byte) ((value & 0x02) << 7);
                register |= mask;
                _si7021.WriteRegister(Registers.WriteUserRegister1, register);
            }
        }

        #endregion Properties

        #region Constructors

        /// <summary>
        ///     Default constructor (private to prevent the user from calling this).
        /// </summary>
        private SI7021()
        {
        }

        /// <summary>
        ///     Create a new SI7021 temperature and humidity sensor.
        /// </summary>
        /// <param name="address">Sensor address (default to 0x40).</param>
        /// <param name="speed">Speed of the I2C interface (default to 100 KHz).</param>
        public SI7021(byte address = 0x40, ushort speed = 100)
        {
            _si7021 = new I2CBus(address, speed);
            //
            //  Get the device ID.
            //
            var part1 = _si7021.WriteRead(new[]
            {
                Registers.ReadIDFirstBytePart1,
                Registers.ReadIDFirstBytePart2
            }, 8);
            var part2 = _si7021.WriteRead(new[]
            {
                Registers.ReadIDSecondBytePart1,
                Registers.ReadIDSecondBytePart2
            }, 6);
            SerialNumber = 0;
            for (var index = 0; index < 4; index++)
            {
                SerialNumber <<= 8;
                SerialNumber += part1[index * 2];
            }
            SerialNumber <<= 8;
            SerialNumber += part2[0];
            SerialNumber <<= 8;
            SerialNumber += part2[1];
            SerialNumber <<= 8;
            SerialNumber += part2[3];
            SerialNumber <<= 8;
            SerialNumber += part2[4];
            if ((part2[0] == 0) || (part2[0] == 0xff))
            {
                SensorType = DeviceType.EngineeringSample;
            }
            else
            {
                SensorType = (DeviceType) part2[0];
            }
            //
            //  Read the firmware revision.
            //
            var firmware = _si7021.WriteRead(new[]
            {
                Registers.ReadFirmwareRevisionPart1,
                Registers.ReadFirmwareRevisionPart2
            }, 1);
            FirmwareRevision = firmware[0];
            //
            //  Now make the first measurement.
            //
            Read();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        ///     Make a temperature and humidity reading.
        /// </summary>
        public void Read()
        {
            _si7021.WriteByte(Registers.MeasureHumidityNoHold);
            //
            //  Maximum conversion time is 12ms (page 5 of the datasheet).
            //
            Thread.Sleep(25);
            var data = _si7021.ReadBytes(3);
            var humidityReading = (ushort) ((data[0] << 8) + data[1]);
            Humidity = ((125 * (float) humidityReading) / 65536) - 6;
            if (Humidity < 0)
            {
                Humidity = 0;
            }
            else
            {
                if (Humidity > 100)
                {
                    Humidity = 100;
                }
            }
            data = _si7021.ReadRegisters(Registers.ReadPreviousTemperatureMeasurement, 2);
            var temperatureReading = (short) ((data[0] << 8) + data[1]);
            Temperature = (float) (((175.72 * temperatureReading) / 65536) - 46.85);
        }

        /// <summary>
        ///     Reset the sensor and take a fresh reading.
        /// </summary>
        public void Reset()
        {
            _si7021.WriteByte(Registers.Reset);
            Thread.Sleep(50);
            Read();
        }

        /// <summary>
        ///     Turn the heater on or off.
        /// </summary>
        /// <param name="onOrOff">Heater status, true = turn heater on, false = turn heater off.</param>
        public void Heater(bool onOrOff)
        {
            var register = _si7021.ReadRegister(Registers.ReadUserRegister1);
            register &= 0xfd;
            if (onOrOff)
            {
                register |= 0x02;
            }
            _si7021.WriteRegister(Registers.WriteUserRegister1, register);
        }

        #endregion Methods
    }
}