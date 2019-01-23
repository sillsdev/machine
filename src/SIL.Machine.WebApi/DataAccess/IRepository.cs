using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	public interface IRepository<T> where T : class, IEntity<T>
	{
		Task InitAsync(CancellationToken ct = default(CancellationToken));

		Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default(CancellationToken));	
		Task<T> GetAsync(string id, CancellationToken ct = default(CancellationToken));
		Task InsertAsync(T entity, CancellationToken ct = default(CancellationToken));
		Task UpdateAsync(T entity, bool checkConflict = false, CancellationToken ct = default(CancellationToken));
		Task DeleteAsync(T entity, bool checkConflict = false, CancellationToken ct = default(CancellationToken));
		Task DeleteAsync(string id, CancellationToken ct = default(CancellationToken));

		Task<Subscription<T>> SubscribeAsync(string id, CancellationToken ct = default(CancellationToken));
	}
}
