/// <summary>
/// Mimic the full CLI namespace and naming so that this library can be used
/// as a drop-in replacement and/or linked file with both frameworks as needed.
/// </summary>

namespace MsgPack.Serialization
{
	public enum NilImplication
	{
		MemberDefault,
		Null,
		Prohibit
	}
}
