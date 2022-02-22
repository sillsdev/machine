
namespace SIL.Machine.WebApi.DataAccess;

public class WritableContractResolver : DefaultContractResolver
{
	protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
	{
		IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
		return props.Where(p => p.Writable).ToList();
	}
}
