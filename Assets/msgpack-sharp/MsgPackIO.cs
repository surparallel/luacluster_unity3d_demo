using System;
using System.Reflection;
using System.IO;
using System.Text;
using System.Collections;
using MsgPack.Serialization;
using System.Collections.Generic;

namespace scopely.msgpacksharp
{
	public static class MsgPackIO
	{
		private static readonly DateTime unixEpocUtc = new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );
		private const string nullProhibitedExceptionMessage = "Null value encountered but is prohibited";

        internal static int ReadNumArrayElements(BinaryReader reader)
        {
            byte header = reader.ReadByte();
            int numElements = -1;
            if (header != MsgPackConstants.Formats.NIL)
            {
                if (header >= MsgPackConstants.FixedArray.MIN && header <= MsgPackConstants.FixedArray.MAX)
                {
                    numElements = header - MsgPackConstants.FixedArray.MIN;
                }
                else if (header == MsgPackConstants.Formats.ARRAY_16)
                {
                    numElements = (reader.ReadByte() << 8) +
                    reader.ReadByte();
                }
                else if (header == MsgPackConstants.Formats.ARRAY_32)
                {
                    numElements = (reader.ReadByte() << 24) +
                        (reader.ReadByte() << 16) +
                        (reader.ReadByte() << 8) +
                        reader.ReadByte();
                }
                else
                {
                    throw new ApplicationException("The serialized data format is invalid due to an invalid array size specification at offset " + reader.BaseStream.Position);
                }
            }
            return numElements;
        }

        internal static void DeserializeArray(Array array, int numElements, BinaryReader reader)
        {
            Type elementType = array.GetType().GetElementType();
            for (int i = 0; i < numElements; i++)
            {
                object o = DeserializeValue(elementType, reader, NilImplication.Null);
                object safeVal = null;
                if (o != null)
                {
                    if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        safeVal = Convert.ChangeType(o, Nullable.GetUnderlyingType(elementType));
                    else
                        safeVal = Convert.ChangeType(o, elementType);
                }
                array.SetValue(safeVal, i);
            }
        }

		internal static bool DeserializeCollection(IList collection, BinaryReader reader)
		{
			bool isNull = true;
			if (!collection.GetType().IsGenericType)
				throw new NotSupportedException("Only generic List<T> lists are supported");
			Type elementType = collection.GetType().GetGenericArguments()[0];
            int numElements = ReadNumArrayElements(reader);
            if (numElements >= 0)
            {
				isNull = false;
				for (int i = 0; i < numElements; i++)
				{
                    object o = DeserializeValue(elementType, reader, NilImplication.Null);
                    object safeVal = null;
                    if (o != null)
                    {
                        if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            safeVal = Convert.ChangeType(o, Nullable.GetUnderlyingType(elementType));
                        else
                            safeVal = Convert.ChangeType(o, elementType);
                    }
                    collection.Add(safeVal);
				}
			}
			return isNull;
		}

        internal static bool DeserializeCollection(IDictionary collection, BinaryReader reader, byte? header = null)
		{
			bool isNull = true;
			if (!collection.GetType().IsGenericType)
				throw new NotSupportedException("Only generic Dictionary<T,U> dictionaries are supported");
			Type keyType = collection.GetType().GetGenericArguments()[0];
			Type valueType = collection.GetType().GetGenericArguments()[1];
            if (!header.HasValue)
			    header = reader.ReadByte();
			if (header != MsgPackConstants.Formats.NIL)
			{
				int numElements = 0;
				if (header >= MsgPackConstants.FixedMap.MIN && header <= MsgPackConstants.FixedMap.MAX)
				{
                    numElements = header.Value - MsgPackConstants.FixedMap.MIN;
				}
				else if (header == MsgPackConstants.Formats.MAP_16)
				{
					numElements = (reader.ReadByte() << 8) + 
						reader.ReadByte();
				}
				else if (header == MsgPackConstants.Formats.MAP_32)
				{
					numElements = (reader.ReadByte() << 24) +
						(reader.ReadByte() << 16) +
						(reader.ReadByte() << 8) +
						reader.ReadByte();
				}
				else
					throw new ApplicationException("The serialized data format is invalid due to an invalid map size specification");
				isNull = false;
				for (int i = 0; i < numElements; i++)
				{
					object key = DeserializeValue(keyType, reader, NilImplication.MemberDefault);
                    object val = DeserializeValue(valueType, reader, NilImplication.Null);
					object safeKey = Convert.ChangeType(key, keyType);
                    object safeVal = null;
                    if (val != null)
                    {
                        if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            safeVal = Convert.ChangeType(val, Nullable.GetUnderlyingType(valueType));
                        else if (valueType == typeof(object))
                            safeVal = val;
                        else
                            safeVal = Convert.ChangeType(val, valueType);
                    }
					collection.Add(safeKey, safeVal);
				}
			}
			return isNull;
		}

		internal static long ToUnixMillis(DateTime dateTime)
		{
			return (long)dateTime.ToUniversalTime().Subtract(unixEpocUtc).TotalMilliseconds;
		}

        internal static long ToUnixMillis(TimeSpan span)
        {
            return (long)span.TotalMilliseconds;
        }

		internal static DateTime ToDateTime(long value)
		{
			return unixEpocUtc.AddMilliseconds(value).ToLocalTime();
		}

        internal static TimeSpan ToTimeSpan(long value)
        {
            return new TimeSpan(0, 0, 0, 0, (int)value);
        }

		internal static object DeserializeValue(Type type, BinaryReader reader, NilImplication nilImplication)
		{
			object result = null;
			bool isRichType = false;
            if (type == typeof(string))
            {
                result = ReadMsgPackString(reader, nilImplication);
            }
			else if (type == typeof(int) || type == typeof(uint) ||
			         type == typeof(byte) || type == typeof(sbyte) ||
			         type == typeof(short) || type == typeof(ushort) ||
                     type == typeof(long) || type == typeof(ulong) ||
                     type == typeof(int?) || type == typeof(uint?) ||
                     type == typeof(byte?) || type == typeof(sbyte?) ||
                     type == typeof(short?) || type == typeof(ushort?) ||
                     type == typeof(long?) || type == typeof(ulong?))
			{
				result = ReadMsgPackInt(reader, nilImplication);
			}
			else if (type == typeof(char))
			{
				result = ReadMsgPackInt(reader, nilImplication);
			}
			else if (type == typeof(float))
			{
				result = ReadMsgPackFloat(reader, nilImplication);
			}
			else if (type == typeof(double))
			{
				result = ReadMsgPackDouble(reader, nilImplication);
			}
			else if (type == typeof(Boolean) || type == typeof(bool))
			{
				result = ReadMsgPackBoolean(reader, nilImplication);
			}
			else if (type == typeof(DateTime))
			{
                object boxedVal = ReadMsgPackInt(reader, nilImplication);
                if (boxedVal == null)
                {
                    result = null;
                }
                else
                {
                    long unixEpochTicks = (long)boxedVal;
                    result = ToDateTime(unixEpochTicks);
                }
			}
            else if (type == typeof(TimeSpan))
            {
                object boxedVal = ReadMsgPackInt(reader, nilImplication);
                if (boxedVal == null)
                {
                    result = null;
                }
                else
                {
                    int unixEpochTicks = (int)boxedVal;
                    result = ToTimeSpan(unixEpochTicks);
                }
            }
			else if (type.IsEnum)
			{
                object boxedVal = ReadMsgPackString(reader, nilImplication);
                if (boxedVal == null)
                {
                    result = null;
                }
                else
                {
                    string enumVal = (string)boxedVal;
                    if (enumVal == "")
                    {
                        result = null;
                    }
                    else
                    {
                        result = Enum.Parse(type, enumVal);
                    }
                }
			}
			else if (type.IsArray)
			{
                int numElements = MsgPackIO.ReadNumArrayElements(reader);
                if (numElements == -1)
                {
                    result = null;
                }
                else
                {
                    result = Activator.CreateInstance(type, new object[] { numElements });
                    MsgPackIO.DeserializeArray((Array)result, numElements, reader);
                }
			}
			else if (type == typeof(System.Object))
			{
				byte header = reader.ReadByte();
                if (header == MsgPackConstants.Formats.NIL)
                    result = null;
                else if (header == MsgPackConstants.Bool.TRUE)
                    result = true;
                else if (header == MsgPackConstants.Bool.FALSE)
                    result = false;
                else if (header == MsgPackConstants.Formats.FLOAT_64)
                    result = ReadMsgPackDouble(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.FLOAT_32)
                    result = ReadMsgPackFloat(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.INTEGER_16)
                    result = ReadMsgPackInt(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.INTEGER_32)
                    result = ReadMsgPackInt(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.INTEGER_64)
                    result = ReadMsgPackInt(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.INTEGER_8)
                    result = ReadMsgPackInt(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.STRING_8)
                    result = ReadMsgPackString(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.STRING_16)
                    result = ReadMsgPackString(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.STRING_32)
                    result = ReadMsgPackString(reader, nilImplication, header);
                else if (header >= MsgPackConstants.FixedString.MIN && header <= MsgPackConstants.FixedString.MAX)
                    result = ReadMsgPackString(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.UNSIGNED_INTEGER_8)
                    result = ReadMsgPackInt(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.UNSIGNED_INTEGER_16)
                    result = ReadMsgPackInt(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.UNSIGNED_INTEGER_32)
                    result = ReadMsgPackInt(reader, nilImplication, header);
                else if (header == MsgPackConstants.Formats.UNSIGNED_INTEGER_64)
                    result = ReadMsgPackInt(reader, nilImplication, header);
                else if (header >= MsgPackConstants.FixedInteger.POSITIVE_MIN && header <= MsgPackConstants.FixedInteger.POSITIVE_MAX)
                {
                    if (header == 0)
                        result = 0;
                    else
                        result = ReadMsgPackInt(reader, nilImplication, header);
                }
                else if (header >= MsgPackConstants.FixedInteger.NEGATIVE_MIN && header <= MsgPackConstants.FixedInteger.NEGATIVE_MAX)
                    result = ReadMsgPackInt(reader, nilImplication, header);
                else if ((header >= MsgPackConstants.FixedMap.MIN && header <= MsgPackConstants.FixedMap.MAX) ||
                         header == MsgPackConstants.Formats.MAP_16 || header == MsgPackConstants.Formats.MAP_32)
                {
                    result = new Dictionary<string,object>();
                    MsgPackIO.DeserializeCollection((Dictionary<string,object>)result, reader, header);
                }
				else if ((header >= MsgPackConstants.FixedArray.MIN && header <= MsgPackConstants.FixedArray.MAX) ||
						header == MsgPackConstants.Formats.ARRAY_16 || header == MsgPackConstants.Formats.ARRAY_32)
				{
					int numElements = -1;
					if (header != MsgPackConstants.Formats.NIL)
					{
						if (header >= MsgPackConstants.FixedArray.MIN && header <= MsgPackConstants.FixedArray.MAX)
						{
							numElements = header - MsgPackConstants.FixedArray.MIN;
						}
						else if (header == MsgPackConstants.Formats.ARRAY_16)
						{
							numElements = (reader.ReadByte() << 8) +
							reader.ReadByte();
						}
						else if (header == MsgPackConstants.Formats.ARRAY_32)
						{
							numElements = (reader.ReadByte() << 24) +
								(reader.ReadByte() << 16) +
								(reader.ReadByte() << 8) +
								reader.ReadByte();
						}
						else
						{
							throw new ApplicationException("The serialized data format is invalid due to an invalid array size specification at offset " + reader.BaseStream.Position);
						}
					}

					result = Activator.CreateInstance(typeof(object[]), new object[] { numElements });
					MsgPackIO.DeserializeArray((Array)result, numElements, reader);
				}
				else
					isRichType = true;
			}
			else
			{
				isRichType = true;
			}

			if (isRichType)
			{
				ConstructorInfo constructorInfo = type.GetConstructor(Type.EmptyTypes);
				if (constructorInfo == null)
				{
					throw new ApplicationException("Can't deserialize Type [" + type + "] because it has no default constructor");
				}
				result = constructorInfo.Invoke(SerializableProperty.EmptyObjArgs);
				result = MsgPackSerializer.DeserializeObject(result, reader, nilImplication);
			}
			return result;
		}

		internal static byte ReadHeader(Type t, BinaryReader reader, NilImplication nilImplication, out object result)
		{
			result = null;
			byte v = reader.ReadByte();
			if (v == MsgPackConstants.Formats.NIL)
			{
				if (nilImplication == NilImplication.MemberDefault)
				{
					if (t.IsValueType)
					{
						result = Activator.CreateInstance(t);
					}
				}
				else if (nilImplication == NilImplication.Prohibit)
				{
					throw new ApplicationException(nullProhibitedExceptionMessage);
				}
			}
			return v;
		}

		internal static object ReadMsgPackBoolean(BinaryReader reader, NilImplication nilImplication)
		{
			object result;
			byte v = ReadHeader(typeof(bool), reader, nilImplication, out result);
			if (v != MsgPackConstants.Formats.NIL)
			{
				result = v == MsgPackConstants.Bool.TRUE;
			}
			return result;
		}

		internal static object ReadMsgPackFloat(BinaryReader reader, NilImplication nilImplication, byte header = 0)
		{
			object result = null;
			byte v = header == 0 ? ReadHeader(typeof(float), reader, nilImplication, out result) : header;
			if (v != MsgPackConstants.Formats.NIL)
			{
				if (v != MsgPackConstants.Formats.FLOAT_32)
					throw new ApplicationException("Serialized data doesn't match type being deserialized to");
				byte[] data = reader.ReadBytes(4);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				result = BitConverter.ToSingle(data, 0);
			}
			return result;
		}

		internal static object ReadMsgPackDouble(BinaryReader reader, NilImplication nilImplication, byte header = 0)
		{
			object result = null;
			byte v = header == 0 ? ReadHeader(typeof(double), reader, nilImplication, out result) : header;
			if (v != MsgPackConstants.Formats.NIL)
			{
				if (v != MsgPackConstants.Formats.FLOAT_64)
					throw new ApplicationException("Serialized data doesn't match type being deserialized to");
				byte[] data = reader.ReadBytes(8);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				result = BitConverter.ToDouble(data, 0);
			}
			return result;
		}

		internal static object ReadMsgPackULong(BinaryReader reader, NilImplication nilImplication, byte header = 0)
		{
			object result = null;
			byte v = header == 0 ? ReadHeader(typeof(ulong), reader, nilImplication, out result) : header;
			if (v != MsgPackConstants.Formats.NIL)
			{
				if (v != MsgPackConstants.Formats.UINT_64)
					throw new ApplicationException("Serialized data doesn't match type being deserialized to");
				result = reader.ReadUInt64();
			}
			return result;
		}

		internal static object ReadMsgPackInt(BinaryReader reader, NilImplication nilImplication, byte header = 0)
		{
			object result = null;
			byte v = header == 0 ? ReadHeader(typeof(long), reader, nilImplication, out result) : header;
			if (v != MsgPackConstants.Formats.NIL)
			{
				if (v <= MsgPackConstants.FixedInteger.POSITIVE_MAX)
				{
					result = v;
				}
				else if (v >= MsgPackConstants.FixedInteger.NEGATIVE_MIN)
				{
					result = -(v - MsgPackConstants.FixedInteger.NEGATIVE_MIN);
				}
				else if (v == MsgPackConstants.Formats.UINT_8)
				{
					result = reader.ReadByte();
				}
				else if (v == MsgPackConstants.Formats.UINT_16)
				{
					result = (reader.ReadByte() << 8) + 
						reader.ReadByte();
				}
				else if (v == MsgPackConstants.Formats.UINT_32)
				{
                    result = (uint)(reader.ReadByte() << 24) + 
                        (uint)(reader.ReadByte() << 16) + 
                        (uint)(reader.ReadByte() << 8) + 
                        (uint)reader.ReadByte();
				}
				else if (v == MsgPackConstants.Formats.UINT_64)
				{
                    result = (ulong)(reader.ReadByte() << 56) +
                        (ulong)(reader.ReadByte() << 48) +
                        (ulong)(reader.ReadByte() << 40) +
                        (ulong)(reader.ReadByte() << 32) +
                        (ulong)(reader.ReadByte() << 24) +
                        (ulong)(reader.ReadByte() << 16) +
                        (ulong)(reader.ReadByte() << 8) +
                        (ulong)reader.ReadByte();
				}
				else if (v == MsgPackConstants.Formats.INT_8)
				{
					result = reader.ReadSByte();
				}
				else if (v == MsgPackConstants.Formats.INT_16)
				{
					byte[] data = reader.ReadBytes(2);
					if (BitConverter.IsLittleEndian)
						Array.Reverse(data);
					result = BitConverter.ToInt16(data, 0);
				}
				else if (v == MsgPackConstants.Formats.INT_32)
				{
					byte[] data = reader.ReadBytes(4);
					if (BitConverter.IsLittleEndian)
						Array.Reverse(data);
					result = BitConverter.ToInt32(data, 0);
				}
				else if (v == MsgPackConstants.Formats.INT_64)
				{
					byte[] data = reader.ReadBytes(8);
					if (BitConverter.IsLittleEndian)
						Array.Reverse(data);
					result = BitConverter.ToInt64(data, 0);
				}
				else
					throw new ApplicationException("Serialized data doesn't match type being deserialized to");
			}
			return result;
		}

		internal static object ReadMsgPackString(BinaryReader reader, NilImplication nilImplication, byte header = 0)
		{
			object result = null;
			byte v = header == 0 ? ReadHeader(typeof(string), reader, nilImplication, out result) : header;
			if (v != MsgPackConstants.Formats.NIL)
			{
				int length = 0;
				if (v >= MsgPackConstants.FixedString.MIN && v <= MsgPackConstants.FixedString.MAX)
				{
					length = v - MsgPackConstants.FixedString.MIN;
				}
				else if (v == MsgPackConstants.Formats.STR_8)
				{
					length = reader.ReadByte();
				}
				else if (v == MsgPackConstants.Formats.STR_16)
				{
					length = (reader.ReadByte() << 8) + 
						reader.ReadByte();
				}
				else if (v == MsgPackConstants.Formats.STR_32)
				{
					length = (reader.ReadByte() << 24) + 
						(reader.ReadByte() << 16) + 
						(reader.ReadByte() << 8) + 
						reader.ReadByte();
				}
				byte[] stringBuffer = reader.ReadBytes(length);
				result = UTF8Encoding.UTF8.GetString(stringBuffer);
			}
			return result;
		}

		internal static void WriteMsgPack(BinaryWriter writer, bool val)
		{
			if (val)
				writer.Write(MsgPackConstants.Bool.TRUE);
			else
				writer.Write(MsgPackConstants.Bool.FALSE);
		}

		internal static void WriteMsgPack(BinaryWriter writer, float val)
		{
			byte[] data = BitConverter.GetBytes(val);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(data);
			writer.Write(MsgPackConstants.Formats.FLOAT_32);
			writer.Write(data);
		}

		internal static void WriteMsgPack(BinaryWriter writer, double val)
		{
			byte[] data = BitConverter.GetBytes(val);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(data);
			writer.Write(MsgPackConstants.Formats.FLOAT_64);
			writer.Write(data);
		}

		internal static void WriteMsgPack(BinaryWriter writer, DateTime val)
		{
			WriteMsgPack(writer, ToUnixMillis(val));
		}

        internal static void WriteMsgPack(BinaryWriter writer, TimeSpan val)
        {
            WriteMsgPack(writer, ToUnixMillis(val));
        }

		internal static void WriteMsgPack(BinaryWriter writer, sbyte val)
		{
			writer.Write(MsgPackConstants.Formats.INT_8);
			writer.Write(val);
		}

		internal static void WriteMsgPack(BinaryWriter writer, byte val)
		{
			writer.Write(MsgPackConstants.Formats.UINT_8);
			writer.Write(val);
		}

		internal static void WriteMsgPack(BinaryWriter writer, char val)
		{
			WriteMsgPack(writer, (ushort)val);
		}

		internal static void WriteMsgPack(BinaryWriter writer, ushort val)
		{
			if (val <= MsgPackConstants.FixedInteger.POSITIVE_MAX)
			{
				writer.Write((byte)val);
			}
			else if (val <= Byte.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_8);
				writer.Write((byte)val);
			}
			else
			{
				writer.Write(MsgPackConstants.Formats.UINT_16);
				byte[] data = BitConverter.GetBytes(val);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
		}

		internal static void WriteMsgPack(BinaryWriter writer, short val)
		{
			if (val >= 0 && val <= MsgPackConstants.FixedInteger.POSITIVE_MAX)
			{
				writer.Write((byte)val);
			}
			else if (val >= 0 && val <= byte.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_8);
				writer.Write((byte)val);
			}
			else if (val >= SByte.MinValue && val <= SByte.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.INT_8);
				writer.Write((sbyte)val);
			}
			else
			{
				writer.Write(MsgPackConstants.Formats.INT_16);
				byte[] data = BitConverter.GetBytes(val);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
		}

		internal static void WriteMsgPack(BinaryWriter writer, uint val)
		{
			if (val <= MsgPackConstants.FixedInteger.POSITIVE_MAX)
			{
				writer.Write((byte)val);
			}
			else if (val <= byte.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_8);
				writer.Write((byte)val);
			}
			else if (val <= UInt16.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_16);
				ushort outVal = (ushort)val;
				byte[] data = BitConverter.GetBytes(outVal);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
			else
			{
				writer.Write(MsgPackConstants.Formats.UINT_32);
				byte[] data = BitConverter.GetBytes(val);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
		}

		internal static void WriteMsgPack(BinaryWriter writer, int val)
		{
			if (val >= 0 && val <= MsgPackConstants.FixedInteger.POSITIVE_MAX)
			{
				writer.Write((byte)val);
			}
			else if (val >= 0 && val <= byte.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_8);
				writer.Write((byte)val);
			}
			else if (val >= sbyte.MinValue && val <= sbyte.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.INT_8);
				writer.Write((sbyte)val);
			}
			else if (val >= Int16.MinValue && val <= Int16.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.INT_16);
				short outVal = (short)val;
				byte[] data = BitConverter.GetBytes(outVal);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
			else if (val >= 0 && val <= UInt16.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_16);
				ushort outVal = (ushort)val;
				byte[] data = BitConverter.GetBytes(outVal);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
			else
			{
				writer.Write(MsgPackConstants.Formats.INT_32);
				byte[] data = BitConverter.GetBytes(val);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
		}

		internal static void WriteMsgPack(BinaryWriter writer, ulong val)
		{
			if (val <= MsgPackConstants.FixedInteger.POSITIVE_MAX)
			{
				writer.Write((byte)val);
			}
			else if (val <= byte.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_8);
				writer.Write((byte)val);
			}
			else if (val <= UInt16.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_16);
				ushort outVal = (ushort)val;
				byte[] data = BitConverter.GetBytes(outVal);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
			else if (val <= UInt32.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_32);
				uint outVal = (uint)val;
				byte[] data = BitConverter.GetBytes(outVal);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
			else
			{
				writer.Write(MsgPackConstants.Formats.UINT_64);
				byte[] data = BitConverter.GetBytes(val);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
		}

		internal static void WriteMsgPack(BinaryWriter writer, long val)
		{
			if (val >= 0 && val <= MsgPackConstants.FixedInteger.POSITIVE_MAX)
			{
				writer.Write((byte)val);
			}
			else if (val >= 0 && val <= byte.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_8);
				writer.Write((byte)val);
			}
			else if (val >= sbyte.MinValue && val <= sbyte.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.INT_8);
				writer.Write((sbyte)val);
			}
			else if (val >= Int16.MinValue && val <= Int16.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.INT_16);
				short outVal = (short)val;
				byte[] data = BitConverter.GetBytes(outVal);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
			else if (val >= 0 && val <= UInt16.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_16);
				ushort outVal = (ushort)val;
				byte[] data = BitConverter.GetBytes(outVal);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
			else if (val >= Int32.MinValue && val <= Int32.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.INT_32);
				int outVal = (int)val;
				byte[] data = BitConverter.GetBytes(outVal);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
			else if (val >= 0 && val <= UInt32.MaxValue)
			{
				writer.Write(MsgPackConstants.Formats.UINT_32);
				uint outVal = (uint)val;
				byte[] data = BitConverter.GetBytes(outVal);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
			else
			{
				writer.Write(MsgPackConstants.Formats.INT_64);
				byte[] data = BitConverter.GetBytes(val);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);
				writer.Write(data);
			}
		}

		internal static void WriteMsgPack(BinaryWriter writer, string s)
		{
			if (string.IsNullOrEmpty(s))
				writer.Write(MsgPackConstants.FixedString.MIN);
			else
			{
				byte[] utf8Bytes = UTF8Encoding.UTF8.GetBytes(s);
				uint length = (uint)utf8Bytes.Length;
				if (length <= MsgPackConstants.FixedString.MAX_LENGTH)
				{
					byte val = (byte)(MsgPackConstants.FixedString.MIN | length);
					writer.Write(val);
				}
				else if (length <= byte.MaxValue)
				{
					writer.Write(MsgPackConstants.Formats.STR_8);
					writer.Write((byte)length);
				}
				else if (length <= ushort.MaxValue)
				{
					writer.Write(MsgPackConstants.Formats.STR_16);
					ushort outVal = (ushort)length;
					byte[] data = BitConverter.GetBytes(outVal);
					if (BitConverter.IsLittleEndian)
						Array.Reverse(data);
					writer.Write(data);
				}
				else
				{
					writer.Write(MsgPackConstants.Formats.STR_32);
					uint outVal = (uint)length;
					byte[] data = BitConverter.GetBytes(outVal);
					if (BitConverter.IsLittleEndian)
						Array.Reverse(data);
					writer.Write(data);
				}
				for (int i = 0; i < utf8Bytes.Length; i++)
				{
					writer.Write(utf8Bytes[i]);
				}
			}
		}

        internal static void SerializeEnumerable(IEnumerator collection, BinaryWriter writer, SerializationMethod serializationMethod)
		{
			while (collection.MoveNext())
			{
				object val = collection.Current;
                SerializeValue(val, writer, serializationMethod);
			}
		}

        internal static void SerializeValue(object val, BinaryWriter writer, SerializationMethod serializationMethod)
		{
			if (val == null)
				writer.Write(MsgPackConstants.Formats.NIL);
			else
			{
				Type t = val.GetType();
				t = Nullable.GetUnderlyingType(t) ?? t;
                if (t == typeof(string))
                {
                    WriteMsgPack(writer, (string)val);
                }
                else if (t == typeof(char) || t == typeof(System.Char))
                {
                    WriteMsgPack(writer, (char)val);
                }
                else if (t == typeof(float) || t == typeof(Single))
                {
                    WriteMsgPack(writer, (float)val);
                }
                else if (t == typeof(double) || t == typeof(Double))
                {
                    WriteMsgPack(writer, (double)val);
                }
                else if (t == typeof(byte) || t == typeof(Byte))
                {
                    WriteMsgPack(writer, (byte)val);
                }
                else if (t == typeof(sbyte) || t == typeof(SByte))
                {
                    WriteMsgPack(writer, (sbyte)val);
                }
                else if (t == typeof(short) || t == (typeof(Int16)))
                {
                    WriteMsgPack(writer, (short)val);
                }
                else if (t == typeof(ushort) || t == (typeof(UInt16)))
                {
                    WriteMsgPack(writer, (ushort)val);
                }
                else if (t == typeof(int) || t == (typeof(Int32)))
                {
                    WriteMsgPack(writer, (int)val);
                }
                else if (t == typeof(uint) || t == (typeof(UInt32)))
                {
                    WriteMsgPack(writer, (uint)val);
                }
                else if (t == typeof(long) || t == (typeof(Int64)))
                {
                    WriteMsgPack(writer, (long)val);
                }
                else if (t == typeof(ulong) || t == (typeof(UInt64)))
                {
                    WriteMsgPack(writer, (ulong)val);
                }
                else if (t == typeof(bool) || t == (typeof(Boolean)))
                {
                    WriteMsgPack(writer, (bool)val);
                }
                else if (t == typeof(DateTime))
                {
                    WriteMsgPack(writer, (DateTime)val);
                }
                else if (t == typeof(TimeSpan))
                {
                    WriteMsgPack(writer, (TimeSpan)val);
                }
                else if (t == typeof(decimal))
                {
                    throw new ApplicationException("The Decimal Type isn't supported");
                }
				else if (t.IsEnum)
				{
					WriteMsgPack(writer, Enum.GetName(t, val));
				}
				else if (t.IsArray)
				{
					Array array = val as Array;
					if (array == null)
					{
						writer.Write((byte)MsgPackConstants.Formats.NIL);
					}
					else
					{
						if (array.Length <= 15)
						{
							byte arrayVal = (byte)(MsgPackConstants.FixedArray.MIN + array.Length);
							writer.Write(arrayVal);
						}
						else if (array.Length <= UInt16.MaxValue)
						{
							writer.Write((byte)MsgPackConstants.Formats.ARRAY_16);
							byte[] data = BitConverter.GetBytes((ushort)array.Length);
							if (BitConverter.IsLittleEndian)
								Array.Reverse(data);
							writer.Write(data);
						}
						else
						{
							writer.Write((byte)MsgPackConstants.Formats.ARRAY_32);
							byte[] data = BitConverter.GetBytes((uint)array.Length);
							if (BitConverter.IsLittleEndian)
								Array.Reverse(data);
							writer.Write(data);
						}
                        SerializeEnumerable(array.GetEnumerator(), writer, serializationMethod);
					}
				}
				else if (MsgPackSerializer.IsGenericList(t))
				{
				    IList list = val as IList;
				    if (list.Count <= 15)
				    {
				        byte arrayVal = (byte)(MsgPackConstants.FixedArray.MIN + list.Count);
				        writer.Write(arrayVal);
				    }
				    else if (list.Count <= UInt16.MaxValue)
				    {
				        writer.Write((byte)MsgPackConstants.Formats.ARRAY_16);
				        byte[] data = BitConverter.GetBytes((ushort)list.Count);
				        if (BitConverter.IsLittleEndian)
				            Array.Reverse(data);
				        writer.Write(data);
				    }
				    else
				    {
				        writer.Write((byte)MsgPackConstants.Formats.ARRAY_32);
				        byte[] data = BitConverter.GetBytes((uint)list.Count);
				        if (BitConverter.IsLittleEndian)
				            Array.Reverse(data);
				        writer.Write(data);
				    }
				    SerializeEnumerable(list.GetEnumerator(), writer, serializationMethod);
				}
				else if (MsgPackSerializer.IsGenericDictionary(t))
				{
				    IDictionary dictionary = val as IDictionary;
				    if (dictionary.Count <= 15)
				    {
				        byte header = (byte)(MsgPackConstants.FixedMap.MIN + dictionary.Count);
				        writer.Write(header);
				    }
				    else if (dictionary.Count <= UInt16.MaxValue)
				    {
				        writer.Write((byte)MsgPackConstants.Formats.MAP_16);
				        byte[] data = BitConverter.GetBytes((ushort)dictionary.Count);
				        if (BitConverter.IsLittleEndian)
				            Array.Reverse(data);
				        writer.Write(data);
				    }
				    else
				    {
				        writer.Write((byte)MsgPackConstants.Formats.MAP_32);
				        byte[] data = BitConverter.GetBytes((uint)dictionary.Count);
				        if (BitConverter.IsLittleEndian)
				            Array.Reverse(data);
				        writer.Write(data);
				    }
				    IDictionaryEnumerator enumerator = dictionary.GetEnumerator();
				    while (enumerator.MoveNext())
				    {
                        SerializeValue(enumerator.Key, writer, serializationMethod);
                        SerializeValue(enumerator.Value, writer, serializationMethod);
				    }
				}
				else
				{
                    MsgPackSerializer.SerializeObject(val, writer);
				}
			}
		}
	}
}

