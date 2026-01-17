using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Songify_Slim.Util.Configuration
{
    public sealed class ListStringOrObjectConverter<TItem> : IYamlTypeConverter
    {
        private readonly Func<string, TItem> _fromString;

        public ListStringOrObjectConverter(Func<string, TItem> fromString)
        {
            _fromString = fromString;
        }

        public bool Accepts(Type type) => type == typeof(List<TItem>);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
        {
            parser.Consume<SequenceStart>();

            List<TItem> list = [];

            while (!parser.TryConsume<SequenceEnd>(out _))
            {
                // old format: "- some string"
                if (parser.TryConsume<Scalar>(out Scalar scalar))
                {
                    string s = scalar.Value?.Trim();
                    if (!string.IsNullOrWhiteSpace(s))
                        list.Add(_fromString(s));
                    continue;
                }

                // new format: "- { ... }"
                object obj = nestedObjectDeserializer(typeof(TItem));
                if (obj is TItem item)
                    list.Add(item);
            }

            return list;
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer nestedObjectSerializer)
        {
            emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
            if (value is IEnumerable<TItem> items)
                foreach (TItem item in items) nestedObjectSerializer(item);
            emitter.Emit(new SequenceEnd());
        }
    }
}