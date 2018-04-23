using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public interface IRepository<T> where T : class, IEntity<T>
	{
		Task InitAsync();

		Task<IEnumerable<T>> GetAllAsync();	
		Task<T> GetAsync(string id);
		Task InsertAsync(T entity);
		Task UpdateAsync(T entity, bool checkConflict = false);
		Task DeleteAsync(T entity, bool checkConflict = false);
		Task DeleteAsync(string id);

		Task<IDisposable> SubscribeAsync(string id, Action<EntityChange<T>> listener);
	}
}
