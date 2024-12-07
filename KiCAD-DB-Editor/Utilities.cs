using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor
{
    public static class Utilities
    {
        public static HashSet<string> ReservedParameterNames = new HashSet<string>()
        {
            "part uid",
            "description",
            "manufacturer",
            "mpn",
            "value",
            "exclude from bom",
            "exclude from board",
            "exclude from sim",
        };
        public static string[] ReservedParameterNameStarts = new string[] 
        {
            "symbol",
            "footprint",
        };
        public static HashSet<char> SafeCategoryCharacters = new HashSet<char>("abcdefghjiklmnopqrstuvwxyz0123456789_-& ");
        public static HashSet<char> SafeParameterCharacters = new HashSet<char>("abcdefghjiklmnopqrstuvwxyz0123456789_-& ");

        public static Dictionary<byte, char> Base32EncodeBook = new()
        {
            { 0, '0' },
            { 1, '1' },
            { 2, '2' },
            { 3, '3' },
            { 4, '4' },
            { 5, '5' },
            { 6, '6' },
            { 7, '7' },
            { 8, '8' },
            { 9, '9' },
            { 10, 'A' },
            { 11, 'B' },
            { 12, 'C' },
            { 13, 'D' },
            { 14, 'E' },
            { 15, 'F' },
            { 16, 'G' },
            { 17, 'H' },
            { 18, 'J' },
            { 19, 'K' },
            { 20, 'M' },
            { 21, 'N' },
            { 22, 'P' },
            { 23, 'Q' },
            { 24, 'R' },
            { 25, 'S' },
            { 26, 'T' },
            { 27, 'V' },
            { 28, 'W' },
            { 29, 'X' },
            { 30, 'Y' },
            { 31, 'Z' },
        };

        public static Dictionary<char, byte> Base32DecodeBook = new()
        {
            { '0', 0 },
            { '1', 1 },
            { '2', 2 },
            { '3', 3 },
            { '4', 4 },
            { '5', 5 },
            { '6', 6 },
            { '7', 7 },
            { '8', 8 },
            { '9', 9 },
            { 'A', 10 },
            { 'B', 11 },
            { 'C', 12 },
            { 'D', 13 },
            { 'E', 14 },
            { 'F', 15 },
            { 'G', 16 },
            { 'H', 17 },
            { 'J', 18 },
            { 'K', 19 },
            { 'M', 20 },
            { 'N', 21 },
            { 'P', 22 },
            { 'Q', 23 },
            { 'R', 24 },
            { 'S', 25 },
            { 'T', 26 },
            { 'V', 27 },
            { 'W', 28 },
            { 'X', 29 },
            { 'Y', 30 },
            { 'Z', 31 },
        };

        private static Random random = new Random();

        public const int PartUIDSchemeNumberOfWildcards = 11;
        public static string GeneratePartUID(string partUIDScheme)
        {
            if (partUIDScheme.Count(c => c == '#') != Utilities.PartUIDSchemeNumberOfWildcards)
                throw new InvalidDataException("Part UID scheme does not contain the necessary wildcard characters");

            // Seconds Epoch [54:20]
            // Milliseconds [19:10]
            // Random [9:0]
            const int secondsEpochShift = 20;
            const int secondsEpochBits = 35;
            const int millisecondsShift = 10;
            const int millisecondsBits = 10;
            const int randomShift = 0;
            const int randomBits = 10;

            DateTime utcNow = DateTime.UtcNow;
            TimeSpan epoch = utcNow.Subtract(DateTime.UnixEpoch);

            UInt64 secondsSinceEpoch = (UInt64)(epoch.TotalSeconds) & (((UInt64)1 << secondsEpochBits) - 1);
            uint milliseconds = (uint)utcNow.Millisecond & ((1 << millisecondsBits) - 1);
            uint randomValue = (uint)(random.Next(1 << 10)) & ((1 << randomBits) - 1);
            UInt64 uid = (secondsSinceEpoch << secondsEpochShift) |
                ((UInt64)milliseconds << millisecondsShift) |
                ((UInt64)randomValue << randomShift);
            string base32UID = Utilities.UInt64ToBase32(uid).PadLeft(Utilities.PartUIDSchemeNumberOfWildcards, Base32EncodeBook[0]);
            char[] partUIDArray = partUIDScheme.ToCharArray();
            for (int i = 0; i < base32UID.Length; i++)
                partUIDArray[Array.IndexOf(partUIDArray, '#')] = base32UID[i];
            string partUID = new string(partUIDArray);

            /*
            // Type [59:55]
            // Millisecond Epoch [54:10]
            // Random [9:0]
            const int typeShift = 55;
            const int typeBits = 5;
            const int millisecondEpochShift = 10;
            const int millisecondEpochBits = 45;
            const int randomShift = 0;
            const int randomBits = 10;

            const byte type = 0 & ((1 << typeBits) - 1);
            UInt64 millisecondsSinceEpoch = (UInt64)(DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds) & (((UInt64)1 << millisecondEpochBits) - 1);
            uint randomValue = (uint)(random.Next(1 << 10)) & ((1 << randomBits) - 1);
            UInt64 uid = ((UInt64)type << typeShift) | (millisecondsSinceEpoch << millisecondEpochShift) | ((UInt64)randomValue << randomShift);
            string base32UID = Utilities.UInt64ToBase32(uid).PadLeft(Utilities.PartUIDSchemeNumberOfWildcards, Base32EncodeBook[0]);
            char[] partUIDArray = partUIDScheme.ToCharArray();
            for (int i = 0; i < base32UID.Length; i++)
                partUIDArray[Array.IndexOf(partUIDArray, '#')] = base32UID[i];
            string partUID = new string(partUIDArray);
            */

            /*
            // Type [69:65]
            // Year [64:55]
            // Month [54:50]
            // Day [49:45]
            // Hours [44:40]
            // Minutes [39:30]
            // Seconds [29:20]
            // Milliseconds [19:10]
            // Random [9:0]
            const int typeShift = 65;
            const int typeBits = 5;
            const int yearShift = 55;
            const int yearBits = 10;
            const int monthShift = 50;
            const int monthBits = 5;
            const int dayShift = 45;
            const int dayBits = 5;
            const int hoursShift = 40;
            const int hoursBits = 5;
            const int minutesShift = 30;
            const int minutesBits = 10;
            const int secondsShift = 20;
            const int secondsBits = 10;
            const int millisecondEpochShift = 10;
            const int millisecondEpochBits = 10;
            const int randomShift = 0;
            const int randomBits = 10;
            const byte type = 0 & ((1 << typeBits) - 1);
            DateTime utcnow = DateTime.UtcNow;
            TimeSpan epoch = utcnow.Subtract(DateTime.UnixEpoch);
            uint year = (uint)(utcnow.Year - 1970) & ((1 << yearBits) - 1);
            uint month = (uint)utcnow.Month & ((1 << monthBits) - 1);
            uint day = (uint)utcnow.Day & ((1 << dayBits) - 1);
            uint hours = (uint)utcnow.Hour & ((1 << hoursBits) - 1);
            uint minutes = (uint)utcnow.Minute & ((1 << minutesBits) - 1);
            uint seconds = (uint)utcnow.Second & ((1 << secondsBits) - 1);
            uint milliseconds = (uint)utcnow.Millisecond & ((1 << millisecondEpochBits) - 1);
            uint randomValue = (uint)(random.Next(1 << 10)) & ((1 << randomBits) - 1);
            UInt128 uid = ((UInt128)type << typeShift) |
                ((UInt128)year << yearShift) |
                ((UInt128)month << monthShift) |
                ((UInt128)day << dayShift) |
                ((UInt128)hours << hoursShift) |
                ((UInt128)minutes << minutesShift) |
                ((UInt128)seconds << secondsShift) |
                ((UInt128)milliseconds << millisecondEpochShift) |
                ((UInt128)randomValue << randomShift);
            string base32UID = Utilities.UInt128ToBase32(uid).PadLeft(Utilities.PartUIDSchemeNumberOfWildcards, Base32EncodeBook[0]);
            char[] partUIDArray = partUIDScheme.ToCharArray();
            for (int i = 0; i < base32UID.Length; i++)
                partUIDArray[Array.IndexOf(partUIDArray, '#')] = base32UID[i];
            string partUID = new string(partUIDArray);
            */

            return partUID;
        }

        private static string UInt64ToBase32(UInt64 uid)
        {
            if (uid == 0)
                return Base32EncodeBook[0].ToString();
            else
            {
                string s = "";
                UInt64 v = uid;
                while (v > 0)
                {
                    s = Base32EncodeBook[(byte)(v & 0x1F)] + s;
                    v = v >> 5;
                }
                return s;
            }
        }
    }
}
