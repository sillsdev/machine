using Microsoft.Extensions.Options;
using SIL.Machine.WebApi.Configuration;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.Mongo
{
	public class MongoProjectRepository : MongoRepository<Project>, IProjectRepository
	{
		public MongoProjectRepository(IOptions<MongoDataAccessOptions> options)
			: base(options, "projects")
		{
		}
	}
}
