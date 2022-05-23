using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SIL.Machine.WebApi.DataAccess;

public class WritableContractResolver : DefaultContractResolver
{
    protected override IList<Newtonsoft.Json.Serialization.JsonProperty> CreateProperties(
        Type type,
        MemberSerialization memberSerialization
    )
    {
        IList<Newtonsoft.Json.Serialization.JsonProperty> props = base.CreateProperties(type, memberSerialization);
        return props.Where(p => p.Writable).ToList();
    }
}
