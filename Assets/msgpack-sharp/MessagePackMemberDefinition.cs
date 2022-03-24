using System;
using MsgPack.Serialization;

namespace scopely.msgpacksharp
{
    public class MessagePackMemberDefinition
    {
        public MessagePackMemberDefinition()
        {
        }

        public string PropertyName { get; set; }
        public NilImplication NilImplication { get; set; }
    }
}

