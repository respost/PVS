using System;
using System.Collections.Generic;
using System.Text; 
using System.Runtime.InteropServices;

namespace Php
{

    public class IDE
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct IDSECTOR
        {
            public ushort wGenConfig;
            public ushort wNumCyls;
            public ushort wReserved;
            public ushort wNumHeads;
            public ushort wBytesPerTrack;
            public ushort wBytesPerSector;
            public ushort wSectorsPerTrack;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public ushort[] wVendorUnique;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
            public string sSerialNumber;
            public ushort wBufferType;
            public ushort wBufferSize;
            public ushort wECCSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string sFirmwareRev;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string sModelNumber;
            public ushort wMoreVendorUnique;
            public ushort wDoubleWordIO;
            public ushort wCapabilities;
            public ushort wReserved1;
            public ushort wPIOTiming;
            public ushort wDMATiming;
            public ushort wBS;
            public ushort wNumCurrentCyls;
            public ushort wNumCurrentHeads;
            public ushort wNumCurrentSectorsPerTrack;
            public uint ulCurrentSectorCapacity;
            public ushort wMultSectorStuff;
            public uint ulTotalAddressableSectors;
            public ushort wSingleWordDMA;
            public ushort wMultiWordDMA;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public byte[] bReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DRIVERSTATUS
        {
            public byte bDriverError;
            public byte bIDEStatus;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] bReserved;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] dwReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SENDCMDOUTPARAMS
        {
            public uint cBufferSize;
            public DRIVERSTATUS DriverStatus;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 513)]
            public byte[] bBuffer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct SRB_IO_CONTROL
        {
            public uint HeaderLength;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string Signature;
            public uint Timeout;
            public uint ControlCode;
            public uint ReturnCode;
            public uint Length;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IDEREGS
        {
            public byte bFeaturesReg;
            public byte bSectorCountReg;
            public byte bSectorNumberReg;
            public byte bCylLowReg;
            public byte bCylHighReg;
            public byte bDriveHeadReg;
            public byte bCommandReg;
            public byte bReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SENDCMDINPARAMS
        {
            public uint cBufferSize;
            public IDEREGS irDriveRegs;
            public byte bDriveNumber;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] bReserved;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] dwReserved;
            public byte bBuffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GETVERSIONOUTPARAMS
        {
            public byte bVersion;
            public byte bRevision;
            public byte bReserved;
            public byte bIDEDeviceMap;
            public uint fCapabilities;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] dwReserved; // For future use. 
        }

        [DllImport("kernel32.dll")]
        private static extern int CloseHandle(uint hObject);
        [DllImport("kernel32.dll")]
        private static extern int DeviceIoControl(uint hDevice,
                       uint dwIoControlCode,
                       ref SENDCMDINPARAMS lpInBuffer,
                       int nInBufferSize,
                       ref SENDCMDOUTPARAMS lpOutBuffer,
                       int nOutBufferSize,
                       ref uint lpbytesReturned,
                       int lpOverlapped);

        [DllImport("kernel32.dll")]
        private static extern int DeviceIoControl(uint hDevice,
         uint dwIoControlCode,
         int lpInBuffer,
         int nInBufferSize,
         ref GETVERSIONOUTPARAMS lpOutBuffer,
         int nOutBufferSize,
         ref uint lpbytesReturned,
         int lpOverlapped);

        [DllImport("kernel32.dll")]
        private static extern uint CreateFile(string lpFileName,
         uint dwDesiredAccess,
         uint dwShareMode,
         int lpSecurityAttributes,
         uint dwCreationDisposition,
         uint dwFlagsAndAttributes,
         int hTemplateFile);

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint INVALID_HANDLE_VALUE = 0xffffffff;
        private const uint DFP_GET_VERSION = 0x00074080;
        private const int IDE_ATAPI_IDENTIFY = 0xA1; // Returns ID sector for ATAPI. 
        private const int IDE_ATA_IDENTIFY = 0xEC; // Returns ID sector for ATA. 
        private const int IDENTIFY_BUFFER_SIZE = 512;
        private const uint DFP_RECEIVE_DRIVE_DATA = 0x0007c088;

        public static string Read(byte drive)
        {
            OperatingSystem os = Environment.OSVersion;
            if (os.Platform != PlatformID.Win32NT) throw new NotSupportedException("仅支持WindowsNT/2000/XP");
            //我没有NT4，请哪位大大测试一下NT4下能不能用 
            //if (os.Version.Major < 5) throw new NotSupportedException("仅支持WindowsNT/2000/XP"); 

            string driveName = "\\\\.\\PhysicalDrive" + drive.ToString();
            uint device = CreateFile(driveName,
             GENERIC_READ | GENERIC_WRITE,
             FILE_SHARE_READ | FILE_SHARE_WRITE,
             0, OPEN_EXISTING, 0, 0);
            if (device == INVALID_HANDLE_VALUE) return "";
            GETVERSIONOUTPARAMS verPara = new GETVERSIONOUTPARAMS();
            uint bytRv = 0;

            if (0 != DeviceIoControl(device, DFP_GET_VERSION,
             0, 0, ref verPara, Marshal.SizeOf(verPara),
             ref bytRv, 0))
            {
                if (verPara.bIDEDeviceMap > 0)
                {
                    byte bIDCmd = (byte)(((verPara.bIDEDeviceMap >> drive & 0x10) != 0) ? IDE_ATAPI_IDENTIFY : IDE_ATA_IDENTIFY);
                    SENDCMDINPARAMS scip = new SENDCMDINPARAMS();
                    SENDCMDOUTPARAMS scop = new SENDCMDOUTPARAMS();

                    scip.cBufferSize = IDENTIFY_BUFFER_SIZE;
                    scip.irDriveRegs.bFeaturesReg = 0;
                    scip.irDriveRegs.bSectorCountReg = 1;
                    scip.irDriveRegs.bCylLowReg = 0;
                    scip.irDriveRegs.bCylHighReg = 0;
                    scip.irDriveRegs.bDriveHeadReg = (byte)(0xA0 | ((drive & 1) << 4));
                    scip.irDriveRegs.bCommandReg = bIDCmd;
                    scip.bDriveNumber = drive;

                    if (0 != DeviceIoControl(device, DFP_RECEIVE_DRIVE_DATA,
                     ref scip, Marshal.SizeOf(scip), ref scop,
                     Marshal.SizeOf(scop), ref bytRv, 0))
                    {
                        StringBuilder s = new StringBuilder();
                        for (int i = 20; i < 40; i += 2)
                        {
                            s.Append((char)(scop.bBuffer[i + 1]));
                            s.Append((char)scop.bBuffer[i]);
                        }
                        CloseHandle(device);
                        return s.ToString().Trim();
                    }
                }
            }
            CloseHandle(device);
            return "";
        }
    }
}
