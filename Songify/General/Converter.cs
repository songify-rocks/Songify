namespace Songify.General
{
    public abstract class Converter
    {
        public abstract string Serialize<T>(T obj);
        public abstract T Deserialize<T>(string text);
    }
}
