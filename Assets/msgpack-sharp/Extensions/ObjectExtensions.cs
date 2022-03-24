using System;

namespace scopely.msgpacksharp.Extensions
{
	public static class ObjectExtensions
	{
		public static byte[] ToMsgPack(this object o)
		{
			if (o == null)
				throw new ArgumentException("Can't serialize null references", "o");
			return MsgPackSerializer.SerializeObject(o);
		}
	}
}
