/// <summary>
/// Mimic the full CLI namespace and naming so that this library can be used
/// as a drop-in replacement and/or linked file with both frameworks as needed.
/// </summary>

using System;

namespace MsgPack.Serialization
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class MessagePackMemberAttribute : Attribute
	{
		private readonly int id;

		public MessagePackMemberAttribute(int id)
		{
			this.id = id;
			NilImplication = NilImplication.MemberDefault;
		}

		public int Id
		{
			get { return id; }
		}

		public NilImplication NilImplication { get; set; }
	}
}
