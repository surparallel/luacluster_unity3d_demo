using System;
using System.Reflection;
using System.IO;
using MsgPack.Serialization;

namespace scopely.msgpacksharp
{
	internal class SerializableProperty
	{
		internal static readonly object[] EmptyObjArgs = {};
	    private readonly NilImplication _nilImplication;

        internal SerializableProperty(PropertyInfo propInfo, int sequence = 0, NilImplication? nilImplication = null)
		{
			PropInfo = propInfo;
			Name = propInfo.Name;
            _nilImplication = nilImplication ?? NilImplication.MemberDefault;
            Sequence = sequence;
			ValueType = propInfo.PropertyType;
            Type underlyingType = Nullable.GetUnderlyingType(propInfo.PropertyType);
            if (underlyingType != null)
            {
                ValueType = underlyingType;
                if (nilImplication.HasValue == false)
                {
                    _nilImplication = NilImplication.Null;
                }
            }
		}

	    internal PropertyInfo PropInfo { get; private set; }

	    internal string Name { get; private set; }

	    internal Type ValueType { get; private set; }

	    internal int Sequence { get; set; }

        internal void Serialize(object o, BinaryWriter writer, SerializationMethod serializationMethod)
		{
            MsgPackIO.SerializeValue(PropInfo.GetValue(o, EmptyObjArgs), writer, serializationMethod);
		}

		internal void Deserialize(object o, BinaryReader reader)
		{
			object val = MsgPackIO.DeserializeValue(ValueType, reader, _nilImplication);
			object safeValue = (val == null) ? null : Convert.ChangeType(val, ValueType);
			PropInfo.SetValue(o, safeValue, EmptyObjArgs);
		}

		public override string ToString ()
		{
			return string.Format ("[SerializableProperty: Name:{0} ValueType:{1}]", Name, ValueType);
		}
	}
}

