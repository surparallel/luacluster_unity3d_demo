using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using MsgPack.Serialization;

namespace scopely.msgpacksharp
{
	public class MsgPackSerializer
	{
        public static readonly SerializationContext DefaultContext = new SerializationContext();
		private Dictionary<string,SerializableProperty> propsByName;
		private List<SerializableProperty> props;
		private Type serializedType;
		private static Dictionary<Type,TypeInfo> typeInfos = new Dictionary<Type, TypeInfo>();

		public MsgPackSerializer(Type type)
		{
			serializedType = type;
			BuildMap();
		}

        public MsgPackSerializer(Type type, IList<MessagePackMemberDefinition> propertyDefinitions)
        {
            serializedType = type;
            BuildMap(propertyDefinitions);
        }

		internal static bool IsGenericList(Type type)
		{
			TypeInfo info = null;
			if (!typeInfos.TryGetValue(type, out info))
			{
				info = new TypeInfo(type);
				typeInfos[type] = info;
			}
			return info.IsGenericList;
		}

		internal static bool IsGenericDictionary(Type type)
		{
			TypeInfo info = null;
			if (!typeInfos.TryGetValue(type, out info))
			{
				info = new TypeInfo(type);
				typeInfos[type] = info;
			}
			return info.IsGenericDictionary;
		}

		internal static bool IsSerializableGenericCollection(Type type)
		{
			TypeInfo info = null;
			if (!typeInfos.TryGetValue(type, out info))
			{
				info = new TypeInfo(type);
				typeInfos[type] = info;
			}
			return info.IsSerializableGenericCollection;
		}

		private static MsgPackSerializer GetSerializer(Type t)
		{
			MsgPackSerializer result = null;
            lock (DefaultContext.Serializers)
            {
                if (!DefaultContext.Serializers.TryGetValue(t, out result))
                {
                    result = DefaultContext.Serializers[t] = new MsgPackSerializer(t);
                }
            }
			return result;
		}

		public static byte[] SerializeObject(object o)
		{
			return GetSerializer(o.GetType()).Serialize(o);
		}

		public static int SerializeObject(object o, byte[] buffer, int offset)
		{
			return GetSerializer(o.GetType()).Serialize(o, buffer, offset);
		}

		public byte[] Serialize(object o)
		{
			byte[] result = null;
			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					Serialize(o, writer);
					result = new byte[stream.Position];
				}
				result = stream.ToArray();
			}
			return result;
		}

		public int Serialize(object o, byte[] buffer, int offset)
		{
			int endPos = 0;
			using (MemoryStream stream = new MemoryStream(buffer))
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					stream.Seek(offset, SeekOrigin.Begin);
					Serialize(o, writer);
					endPos = (int)stream.Position;
				}
			}
			return endPos;
		}

		public static T Deserialize<T>(byte[] buffer) where T : new()
		{
			using (MemoryStream stream = new MemoryStream(buffer))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					object o = DeserializeObjectType(typeof(T), reader);
					return (T)Convert.ChangeType(o, typeof(T));
				}
			}
		}

		public static object Deserialize(Type t, byte[] buffer)
		{
			return Deserialize(t, buffer, 0);
		}

		public static object Deserialize(Type t, byte[] buffer, int offset)
		{
			using (MemoryStream stream = new MemoryStream(buffer))
			{
				stream.Seek(offset, SeekOrigin.Begin);
				using (BinaryReader reader = new BinaryReader(stream))
				{
					object o = DeserializeObjectType(t, reader);
					return Convert.ChangeType(o, t);
				}
			}
		}
		public class DeserializeRet
		{
			public object o;
			public int numRead;
		}
		public static DeserializeRet DeserializeObject2(byte[] buffer, int offset)
		{
			DeserializeRet deserializeRet = new DeserializeRet();
			using (MemoryStream stream = new MemoryStream(buffer))
			{
				stream.Seek(offset, SeekOrigin.Begin);
				using (BinaryReader reader = new BinaryReader(stream))
				{
					deserializeRet.o = DeserializeObjectType(typeof(object), reader);
					deserializeRet.numRead = (int)stream.Position;
				}
			}
			return deserializeRet;
		}

		public static int DeserializeObject(object o, byte[] buffer, int offset)
		{
			int numRead = 0;
			using (MemoryStream stream = new MemoryStream(buffer))
			{
				stream.Seek(offset, SeekOrigin.Begin);
				using (BinaryReader reader = new BinaryReader(stream))
				{
					GetSerializer(o.GetType()).Deserialize(o, reader);
					numRead = (int)stream.Position;
				}
			}
			return numRead;
		}

		internal static object DeserializeObject(object o, BinaryReader reader, NilImplication nilImplication = NilImplication.MemberDefault)
		{
		    var list = o as IList;
		    if (list != null)
			{
				return MsgPackIO.DeserializeCollection(list, reader) ? null : o;
			}
		    var dictionary = o as IDictionary;
		    if (dictionary != null)
		    {
		        return MsgPackIO.DeserializeCollection(dictionary, reader) ? null : o;
		    }
		    return GetSerializer(o.GetType()).Deserialize(o, reader);
		}

	    internal static object DeserializeObjectType(Type type, BinaryReader reader, NilImplication nilImplication = NilImplication.MemberDefault)
		{
			if (type.IsPrimitive || 
				type == typeof(string) ||
				type == typeof(object) ||
				IsSerializableGenericCollection(type))
			{
				return MsgPackIO.DeserializeValue(type, reader, nilImplication);
			}
		    ConstructorInfo constructorInfo = type.GetConstructor(Type.EmptyTypes);
		    if (constructorInfo == null)
		        throw new ApplicationException("Can't deserialize Type [" + type + "] in MsgPackSerializer because it has no default constructor");
		    object result = constructorInfo.Invoke(SerializableProperty.EmptyObjArgs);
		    return GetSerializer(type).Deserialize(result, reader);
		}

		internal object Deserialize(object result, BinaryReader reader)
		{
			byte header = reader.ReadByte();
			if (header == MsgPackConstants.Formats.NIL)
				result = null;
			else
			{
			    if (DefaultContext.SerializationMethod == SerializationMethod.Array)
			    {
			        if (header == MsgPackConstants.Formats.ARRAY_16)
			        {
			            reader.ReadByte();
			            reader.ReadByte();
			        }
			        else if (header == MsgPackConstants.Formats.ARRAY_32)
			        {
			            reader.ReadByte();
			            reader.ReadByte();
			            reader.ReadByte();
			            reader.ReadByte();
			        }
			        else if (header < MsgPackConstants.FixedArray.MIN || header > MsgPackConstants.FixedArray.MAX)
			        {
			            throw new ApplicationException("The serialized array format isn't valid for header [" + header + "]");
			        }

			        foreach (SerializableProperty prop in props)
			        {
			            prop.Deserialize(result, reader);
			        }
			    }
			    else
			    {
			        int numElements;
			        if (header >= MsgPackConstants.FixedMap.MIN && header <= MsgPackConstants.FixedMap.MAX)
			        {
			            numElements = header & 0x0F;
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
                    {
                        throw new ApplicationException("The serialized map format isn't valid");
                    }

			        for (int i = 0; i < numElements; i++)
			        {
			            string propName = (string) MsgPackIO.ReadMsgPackString(reader, NilImplication.Null);
			            SerializableProperty propToProcess = null;
			            if (propsByName.TryGetValue(propName, out propToProcess))
			                propToProcess.Deserialize(result, reader);
			        }
			    }
			}
			return result;
		}

		internal static void SerializeObject(object o, BinaryWriter writer)
		{
			GetSerializer(o.GetType()).Serialize(o, writer);
		}

		private void Serialize(object o, BinaryWriter writer)
		{
			if (o == null)
				writer.Write((byte)MsgPackConstants.Formats.NIL);
			else
			{
				if (serializedType.IsPrimitive || 
					serializedType == typeof(string) ||
					IsSerializableGenericCollection(serializedType))
				{
					MsgPackIO.SerializeValue(o, writer, DefaultContext.SerializationMethod);
				}
				else
				{
                    if (DefaultContext.SerializationMethod == SerializationMethod.Map)
					{
						if (props.Count <= 15)
						{
							byte arrayVal = (byte)(MsgPackConstants.FixedMap.MIN + props.Count);
							writer.Write(arrayVal);
						}
						else if (props.Count <= UInt16.MaxValue)
						{
							writer.Write((byte)MsgPackConstants.Formats.MAP_16);
							byte[] data = BitConverter.GetBytes((ushort)props.Count);
							if (BitConverter.IsLittleEndian)
								Array.Reverse(data);
							writer.Write(data);
						}
						else
						{
							writer.Write((byte)MsgPackConstants.Formats.MAP_32);
							byte[] data = BitConverter.GetBytes((uint)props.Count);
							if (BitConverter.IsLittleEndian)
								Array.Reverse(data);
							writer.Write(data);
						}
						foreach (SerializableProperty prop in props)
						{
							MsgPackIO.WriteMsgPack(writer, prop.Name);
                            prop.Serialize(o, writer, DefaultContext.SerializationMethod);
						}
					}
					else
					{
						if (props.Count <= 15)
						{
							byte arrayVal = (byte)(MsgPackConstants.FixedArray.MIN + props.Count);
							writer.Write(arrayVal);
						}
						else if (props.Count <= UInt16.MaxValue)
						{
							writer.Write((byte)MsgPackConstants.Formats.ARRAY_16);
							byte[] data = BitConverter.GetBytes((ushort)props.Count);
							if (BitConverter.IsLittleEndian)
								Array.Reverse(data);
							writer.Write(data);
						}
						else
						{
							writer.Write((byte)MsgPackConstants.Formats.ARRAY_32);
							byte[] data = BitConverter.GetBytes((uint)props.Count);
							if (BitConverter.IsLittleEndian)
								Array.Reverse(data);
							writer.Write(data);
						}
						foreach (SerializableProperty prop in props)
						{
                            prop.Serialize(o, writer, DefaultContext.SerializationMethod);
						}
					}
				}
			}
		}

		private void BuildMap()
        {
			if (!serializedType.IsPrimitive && 
				serializedType != typeof(string) &&
				!IsSerializableGenericCollection(serializedType))
			{
				props = new List<SerializableProperty>();
				propsByName = new Dictionary<string, SerializableProperty>();
				foreach (PropertyInfo prop in serializedType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
				    if (prop.CanRead == false || prop.CanWrite == false)
				    {
				        continue;
				    }

                    SerializableProperty serializableProp = null;
				    if (DefaultContext.SerializationMethod == SerializationMethod.Map)
				    {
                        serializableProp = new SerializableProperty(prop);
				    }
				    else
				    {
                        object[] customAttributes = prop.GetCustomAttributes(typeof(MessagePackMemberAttribute), true);
                        if (customAttributes.Length == 1)
                        {
                            var att = (MessagePackMemberAttribute)customAttributes[0];
                            serializableProp = new SerializableProperty(prop, att.Id, att.NilImplication);
                        }
				    }

				    if (serializableProp != null)
				    {
                        props.Add(serializableProp);
                        propsByName[serializableProp.Name] = serializableProp;    
				    }
				}
			    if (DefaultContext.SerializationMethod == SerializationMethod.Array)
			    {
                    props.Sort((x, y) => (x.Sequence.CompareTo(y.Sequence)));    
			    }
			}
		}

        private void BuildMap(IList<MessagePackMemberDefinition> propertyDefinitions)
        {
            if (!serializedType.IsPrimitive && 
                serializedType != typeof(string) &&
                !IsSerializableGenericCollection(serializedType))
            {
                props = new List<SerializableProperty>();
                propsByName = new Dictionary<string, SerializableProperty>();
                int id = 1;
                foreach(MessagePackMemberDefinition def in propertyDefinitions)
                {
                    PropertyInfo prop = serializedType.GetProperty(def.PropertyName);

                    if (prop == null || !prop.CanRead || !prop.CanWrite)
                    {
                        continue;
                    }

                    SerializableProperty serializableProp = null;
                    if (DefaultContext.SerializationMethod == SerializationMethod.Map)
                    {
                        serializableProp = new SerializableProperty(prop);
                    }
                    else
                    {
                        serializableProp = new SerializableProperty(prop, id++, def.NilImplication);
                    }

                    if (serializableProp != null)
                    {
                        props.Add(serializableProp);
                        propsByName[serializableProp.Name] = serializableProp;    
                    }
                }
                if (DefaultContext.SerializationMethod == SerializationMethod.Array)
                {
                    props.Sort((x, y) => (x.Sequence.CompareTo(y.Sequence)));    
                }
            }
        }
	}
}

