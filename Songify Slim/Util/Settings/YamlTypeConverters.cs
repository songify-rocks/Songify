using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Events;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Songify_Slim.Util.Settings
{
    internal class YamlTypeConverters
    {
        public class SingleStringToListConverter : IYamlTypeConverter
        {
            public bool Accepts(Type type)
            {
                return type == typeof(List<string>);
            }

            public object ReadYaml(IParser parser, Type type)
            {
                if (parser.TryConsume<Scalar>(out var scalar))
                {
                    // If the node is a scalar (single string), return it as a single-item list
                    return new List<string> { scalar.Value };
                }
                else if (parser.TryConsume<SequenceStart>(out var _))
                {
                    // If the node is a sequence, deserialize it as a list of strings
                    var list = new List<string>();
                    while (!parser.TryConsume<SequenceEnd>(out var _))
                    {
                        var item = parser.Consume<Scalar>().Value;
                        list.Add(item);
                    }
                    return list;
                }
                throw new YamlException("Expected a scalar or sequence node.");
            }

            public void WriteYaml(IEmitter emitter, object value, Type type)
            {
                // Implement this if you plan to serialize objects back to YAML
                throw new NotImplementedException();
            }
        }
    }
}
