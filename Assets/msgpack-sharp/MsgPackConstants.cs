using System;
using System.Reflection;
using System.IO;

namespace scopely.msgpacksharp
{
    internal static class MsgPackConstants
    {
        public const int MAX_PROPERTY_COUNT = 15;
        
        public static class Formats 
        {
            public const byte NIL = 0xc0;
            public const byte FLOAT_32 = 0xca;
            public const byte FLOAT_64 = 0xcb;
            public const byte DOUBLE = 0xcb;
            public const byte UINT_8 = 0xcc;
            public const byte UNSIGNED_INTEGER_8 = 0xcc;
            public const byte UINT_16 = 0xcd;
            public const byte UNSIGNED_INTEGER_16 = 0xcd;
            public const byte UINT_32 = 0xce;
            public const byte UNSIGNED_INTEGER_32 = 0xce;
            public const byte UINT_64 = 0xcf;
            public const byte UNSIGNED_INTEGER_64 = 0xcf;
            public const byte INT_8 = 0xd0;
            public const byte INTEGER_8 = 0xd0;
            public const byte INT_16 = 0xd1;
            public const byte INTEGER_16 = 0xd1;
            public const byte INT_32 = 0xd2;
            public const byte INTEGER_32 = 0xd2;
            public const byte INT_64 = 0xd3;
            public const byte INTEGER_64 = 0xd3;
            public const byte STR_8 = 0xd9;
            public const byte STRING_8 = 0xd9;
            public const byte STR_16 = 0xda;
            public const byte STRING_16 = 0xda;
            public const byte STR_32 = 0xdb;
            public const byte STRING_32 = 0xdb;
			public const byte ARRAY_16 = 0xdc;
			public const byte ARRAY_32 = 0xdd;
			public const byte MAP_16 = 0xde;
			public const byte MAP_32 = 0xdf;
        }
        
        public static class FixedInteger
        {
            public const byte POSITIVE_MIN = 0x00;
            public const byte POSITIVE_MAX = 0x7f;
            public const byte NEGATIVE_MIN = 0xe0;
            public const byte NEGATIVE_MAX = 0xff;
        }
        
        public static class FixedString
        {
            public const byte MIN = 0xa0;
            public const byte MAX = 0xbf;
            public const int MAX_LENGTH = 31;
        }
        
        public static class FixedMap
        {
            public const byte MIN = 0x80;
            public const byte MAX = 0x8f;
        }

		public static class FixedArray
		{
			public const byte MIN = 0x90;
			public const byte MAX = 0x9f;
		}

		public static class Bool
		{
			public const byte FALSE = 0xc2;
			public const byte TRUE = 0xc3;
		}
    }
}

