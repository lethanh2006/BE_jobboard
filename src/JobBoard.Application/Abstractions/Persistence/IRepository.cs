namespace JobBoard.Application.Abstractions.Persistence;

public interface IRepository<TEntity>
    where TEntity : class
{
    TEntity? GetById(int id);

    void Add(TEntity entity);

    void Update(TEntity entity);
}
