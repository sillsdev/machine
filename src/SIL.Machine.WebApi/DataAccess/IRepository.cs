using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	public interface IRepository<T> where T : class, IEntity<T>
	{
		IEnumerable<T> GetAll();
		bool TryGet(string id, out T entity);
		void Insert(T entity);
		void Update(T entity, bool checkConflict = false);
		void Delete(T entity, bool checkConflict = false);

		Task<IEnumerable<T>> GetAllAsync();	
		Task<T> GetAsync(string id);
		Task InsertAsync(T entity);
		Task UpdateAsync(T entity, bool checkConflict = false);
		Task DeleteAsync(T entity, bool checkConflict = false);
	}
}
