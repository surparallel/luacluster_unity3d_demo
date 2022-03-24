using System;

namespace scopely.msgpacksharp
{
	public class TypeInfo
	{
		public TypeInfo(Type type)
		{
			IsGenericList = type.GetInterface("System.Collections.Generic.IList`1") != null;
			IsGenericDictionary = type.GetInterface("System.Collections.Generic.IDictionary`2") != null;
			IsSerializableGenericCollection = IsGenericList || IsGenericDictionary;
		}

		public bool IsGenericList { get; set; }
		public bool IsGenericDictionary { get; set; }
		public bool IsSerializableGenericCollection { get; set; }
	}
}
