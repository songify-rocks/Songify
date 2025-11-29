using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Songify_Slim.Util.Configuration
{
    internal class YamlTypeConverters
    {
        public class SingleStringToListConverter : IYamlTypeConverter
        {
            public bool Accepts(Type type)
            {
                return type == typeof(List<string>);
            }

            public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
            {
                if (parser.TryConsume<Scalar>(out Scalar scalar))
                {
                    // If the node is a scalar (single string), return it as a single-item list
                    return new List<string> { scalar.Value };
                }
                else if (parser.TryConsume<SequenceStart>(out SequenceStart _))
                {
                    // If the node is a sequence, deserialize it as a list of strings
                    List<string> list = [];
                    while (!parser.TryConsume<SequenceEnd>(out SequenceEnd _))
                    {
                        string item = parser.Consume<Scalar>().Value;
                        list.Add(item);
                    }
                    return list;
                }
                throw new YamlException("Expected a scalar or sequence node.");
            }

            public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
            {
                // not implemented
            }
        }
    }
}