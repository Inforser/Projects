namespace MiniORM.Interfaces
{
    using System.Collections.Generic;

    public interface IDbContext
    {
        bool Persist(object entity);

        T FindById<T>(int id);

        IEnumerable<T> FindAll<T>();

        IEnumerable<T> FindAll<T>(string filter);

        T FindFirst<T>();

        T FindFirst<T>(string filter);
    }
}
