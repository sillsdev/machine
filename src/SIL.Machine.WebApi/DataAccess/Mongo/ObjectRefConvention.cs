using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace SIL.Machine.WebApi.DataAccess.Mongo
{
	public class ObjectRefConvention : ConventionBase, IMemberMapConvention
	{
		public void Apply(BsonMemberMap memberMap)
		{
			if (memberMap.MemberName.EndsWith("Ref"))
				memberMap.SetSerializer(new StringSerializer(BsonType.ObjectId));
		}
	}
}
