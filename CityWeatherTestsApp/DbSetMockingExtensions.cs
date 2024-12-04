using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;

public static class DbSetMockingExtensions
{
    public static Mock<DbSet<T>> ReturnsDbSet<T>(this Mock<DbSet<T>> dbSetMock, List<T> sourceList) where T : class
    {
        var queryable = sourceList.AsQueryable();

        dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        return dbSetMock;
    }
}
