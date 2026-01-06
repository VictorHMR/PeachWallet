using PeachWallet.Database.Models;
using PeachWallet.Utils;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PeachWallet.Database
{
    public class LocalDbService
    {
        private readonly SQLiteAsyncConnection _connection;
        private const int CurrentDbVersion = 2;

        private bool _initialized;
        private readonly SemaphoreSlim _initSemaphore = new(1, 1);

        public LocalDbService()
        {
            _connection = new SQLiteAsyncConnection(
                Configuration.DATABASE_PATH,
                Configuration.DATABASE_FLAGS
            );
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
                return;

            await _initSemaphore.WaitAsync();
            try
            {
                if (_initialized)
                    return;

                await InitInternalAsync();
                _initialized = true;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }
        private async Task InitInternalAsync()
        {
            await _connection.CreateTableAsync<DbInfo>();

            var dbInfo = await _connection.Table<DbInfo>().FirstOrDefaultAsync();

            if (dbInfo == null)
            {
                await CreateSchema();
                await _connection.InsertAsync(new DbInfo { Version = CurrentDbVersion });
            }
            else if (dbInfo.Version < CurrentDbVersion)
            {
                await MigrateDatabase(dbInfo.Version);
                dbInfo.Version = CurrentDbVersion;
                await _connection.UpdateAsync(dbInfo);
            }
            await CreateSchema();
        }

        private async Task MigrateDatabase(int oldVersion)
        {
        }


        private async Task CreateSchema()
        {
            await _connection.CreateTableAsync<Lancamento>();
            await _connection.CreateTableAsync<ContaBancaria>();
            await _connection.CreateTableAsync<Projecao>();
        }


        public async Task<T> GetAsync<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            await EnsureInitializedAsync();
            if (_connection != null)
            {
                return await _connection.Table<T>().FirstOrDefaultAsync(predicate);
            }
            return default;
        }


        public async Task<bool> UpdateAsync<T>(T entity) where T : new()
        {
            await EnsureInitializedAsync();
            if (entity == null)
                return false;

            var rowsAffected = await _connection.UpdateAsync(entity);
            return rowsAffected > 0;
        }

        public async Task<int> CreateAsync<T>(T entity) where T : new()
        {
            await EnsureInitializedAsync();

            if (_connection is null)
                return -1;

            await _connection.InsertAsync(entity);

            var prop = typeof(T).GetProperty("Id");
            if (prop != null)
            {
                var value = prop.GetValue(entity);
                if (value != null)
                    return (int)value;
            }

            return -1;
        }

        public async Task DeleteAsync<T>(object id) where T : new()
        {
            await EnsureInitializedAsync();
            if (_connection is not null)
                await _connection.DeleteAsync<T>(id);
        }



        public async Task<List<T>> SelectAsync<T>(Expression<Func<T, bool>> predicate = null) where T : new()
        {
            await EnsureInitializedAsync();
            if (_connection != null)
            {
                if (predicate == null)
                {
                    return await _connection.Table<T>().ToListAsync();
                }
                else
                {
                    return await _connection.Table<T>().Where(predicate).ToListAsync();
                }
            }
            return new List<T>(); // Retorna uma lista vazia caso a conexão seja nula
        }

    }

}
