﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoenixIDE.Processor;
using FoenixIDE.Display;
using FoenixIDE.MemoryLocations;
using FoenixIDE.Common;

namespace FoenixIDE
{
    /// <summary>
    /// Maps an address on the bus to a device or memory. GPU, RAM, and ROM are hard coded. Other I/O devices will be added 
    /// later.
    /// </summary>
    public class MemoryManager : FoenixIDE.Common.IMappable
    {
        public const int MinAddress = 0x000000;
        public const int MaxAddress = 0xffffff;

        public MemoryRAM RAM = null;
        public MemoryRAM FLASH = null;
        public MemoryRAM VIDEO = null;
        public MemoryRAM IO = null;

        public bool VectorPull = false;

        public List<IMappable> Devices = new List<IMappable>();

        public int StartAddress
        {
            get
            {
                return 0;
            }
        }

        public int Length
        {
            get
            {
                return 0x1000000;
            }
        }

        public int EndAddress
        {
            get
            {
                return 0xFFFFFF;
            }
        }

        /// <summary>
        /// Determine whehter the address being read from or written to is an I/O device or a memory cell.
        /// If the location is an I/O device, return that device. Otherwise, return the memory being referenced.
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="Device"></param>
        /// <param name="DeviceAddress"></param>
        public void GetDeviceAt(int Address, out FoenixIDE.Common.IMappable Device, out int DeviceAddress)
        {
            if (Address >= MemoryMap.RAM_START && Address <= MemoryMap.RAM_END)
            {
                Device = RAM;
                DeviceAddress = Address - RAM.StartAddress;
                return;
            }

            if(Address >= MemoryMap.IO_START && Address <= MemoryMap.IO_END)
            {
                Device = IO;
                DeviceAddress = Address - IO.StartAddress;
                return;
            }

            if (Address >= MemoryMap.VIDEO_START && Address < (MemoryMap.VIDEO_START + MemoryMap.VIDEO_SIZE))
            {
                Device = VIDEO;
                DeviceAddress = Address - VIDEO.StartAddress;
            }

            if (Address >= MemoryMap.FLASH_START && Address <= MemoryMap.FLASH_END)
            {
                Device = FLASH;
                DeviceAddress = Address - FLASH.StartAddress;
                return;
            }

            // oops, we didn't map this to anything. 
            Device = null;
            DeviceAddress = 0;
        }

        public virtual byte this[int Address]
        {
            get { return ReadByte(Address); }
            set { WriteByte(Address, value); ; }
        }

        public virtual byte this[int Bank, int Address]
        {
            get { return ReadByte(Bank * 0xffff + Address & 0xffff); }
            set { WriteByte(Bank * 0xffff + Address & 0xffff, value); }
        }

        public virtual byte ReadByte(int Address)
        {
            GetDeviceAt(Address, out FoenixIDE.Common.IMappable device, out int deviceAddress);
            if (device == null)
                return 0xff;
            return device.ReadByte(deviceAddress);
        }

        /// <summary>
        /// Reads a 16-bit word from memory
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>
        public int ReadWord(int Address)
        {
            GetDeviceAt(Address, out FoenixIDE.Common.IMappable device, out int deviceAddress);
            return device.ReadByte(deviceAddress) + (device.ReadByte(deviceAddress + 1) << 8);
        }

        /// <summary>
        /// Reads 3 bytes from memory and builds a 24-bit unsigned integer.
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public int ReadLong(int Address)
        {
            GetDeviceAt(Address, out FoenixIDE.Common.IMappable device, out int deviceAddress);
            return device.ReadByte(deviceAddress)
                + (device.ReadByte(deviceAddress + 1) << 8)
                + (device.ReadByte(deviceAddress + 2) << 16);
        }

        public virtual void WriteByte(int Address, byte Value)
        {
            GetDeviceAt(Address, out FoenixIDE.Common.IMappable device, out int deviceAddress);
            device.WriteByte(deviceAddress, Value);
        }

        public void WriteWord(int Address, int Value)
        {
            GetDeviceAt(Address, out FoenixIDE.Common.IMappable device, out int deviceAddress);
            device.WriteByte(Address, (byte)(Value & 0xff));
            device.WriteByte(Address + 1, (byte)(Value >> 8 & 0xff));
        }

        public void WriteLong(int Address, int Value)
        {
            GetDeviceAt(Address, out FoenixIDE.Common.IMappable device, out int deviceAddress);
            device.WriteByte(deviceAddress, (byte)(Value & 0xff));
            device.WriteByte(deviceAddress + 1, (byte)(Value >> 8 & 0xff));
            device.WriteByte(deviceAddress + 2, (byte)(Value >> 16 & 0xff));
        }

        public int Read(int Address, int Length)
        {
            GetDeviceAt(Address, out FoenixIDE.Common.IMappable device, out int deviceAddress);
            int addr = deviceAddress;
            int ret = device.ReadByte(addr);
            if (Length >= 2)
                ret += device.ReadByte(addr + 1) << 8;
            if (Length >= 3)
                ret += device.ReadByte(addr + 2) << 16;
            return ret;
        }

        internal void Write(int Address, int Value, int Length)
        {
            GetDeviceAt(Address, out FoenixIDE.Common.IMappable device, out int deviceAddress);
            if (device == null)
                throw new Exception("No device at " + Address.ToString("X6"));
            device.WriteByte(deviceAddress, (byte)(Value & 0xff));
            if (Length >= 2)
                device.WriteByte(deviceAddress + 1, (byte)(Value >> 8 & 0xff));
            if (Length >= 3)
                device.WriteByte(deviceAddress + 2, (byte)(Value >> 16 & 0xff));
        }
    }
}