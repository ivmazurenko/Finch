using System.Data.SQLite;
using Xunit;

namespace Finch.Generators.Sqlite.Tests;

public class QueryAsyncTests
{
    private readonly SQLiteConnection _connection = ConnectionProvider.Create();

    [Fact]
    public async Task QueriesSeriesAsInt()
    {
        var items = await _connection.QueryAsync<TbUser>("select * from tb_user");

        Assert.Equal(2, items.Count);
        Assert.Equal(1, items[0].id);
        Assert.Equal(2, items[1].id);
        Assert.Equal("John", items[0].name);
        Assert.Equal("Jane", items[1].name);
    }
}