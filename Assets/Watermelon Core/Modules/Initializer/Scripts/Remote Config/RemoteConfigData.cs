using Newtonsoft.Json;

namespace Watermelon
{
    public abstract class RemoteConfigData
    {
        [JsonIgnore]
        public abstract string Key { get; }

        [JsonIgnore]
        public virtual bool PrettyPrint { get; } = false;
    }
}
