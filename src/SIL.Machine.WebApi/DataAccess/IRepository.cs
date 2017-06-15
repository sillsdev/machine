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
		void Update(T entity);
		void Delete(T entity);

		Task<IEnumerable<T>> GetAllAsync();	
		Task<T> GetAsync(string id);
		Task InsertAsync(T entity);
		Task UpdateAsync(T entity);
		Task DeleteAsync(T entity);
	}
}
