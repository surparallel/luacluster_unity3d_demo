using System;
using System.Collections.Generic;
using MsgPack.Serialization;

namespace scopely.msgpacksharp
{
    public class SerializationContext
    {
        internal Dictionary<Type, MsgPackSerializer> Serializers { get; private set; }
        private SerializationMethod _serializationMethod;
        public SerializationMethod SerializationMethod
        {
            get { return _serializationMethod; }
            set
            {
                if (_serializationMethod != value)
                {
                    switch (value)
                    {
                        case SerializationMethod.Array:
                        case SerializationMethod.Map:
                            _serializationMethod = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("value");
                    }    
                    Serializers = new Dictionary<Type, MsgPackSerializer>();
                }
            }
        }

        public SerializationContext()
        {
            Serializers = new Dictionary<Type, MsgPackSerializer>();
            _serializationMethod = SerializationMethod.Array;
        }

        public void RegisterSerializer<T>(IList<MessagePackMemberDefinition> propertyDefinitions)
        {
            Serializers[typeof(T)] = new MsgPackSerializer(typeof(T), propertyDefinitions);
        }

        public void RegisterSerializer<T>(params string[] propertyNames)
        {
            var defs = new List<MessagePackMemberDefinition>();
            foreach (var propertyName in propertyNames)
            {
                defs.Add(new MessagePackMemberDefinition()
                {
                    PropertyName = propertyName,
                    NilImplication = NilImplication.MemberDefault
                });
            }
            Serializers[typeof(T)] = new MsgPackSerializer(typeof(T), defs);
        }
    }
}