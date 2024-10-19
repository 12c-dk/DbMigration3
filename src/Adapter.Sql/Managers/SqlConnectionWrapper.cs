using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Adapter.Sql.Managers
{
    public class SqlConnectionWrapper
    {
        //The purpose of this class is providing a way to mock IDbConnection. This means that required methods are implemented with pass through parameters, but as VIRTUAL methods, which allows mocking the output. 

        readonly IDbConnection _iDbConnection;
        public SqlConnectionWrapper(string connectionString)
        {
            _iDbConnection = new SqlConnection(connectionString);
            _iDbConnection.Open();
            //con.Query
        }
        public SqlConnectionWrapper()
        { }

        private void CheckConnection()
        {
            bool wasClosed = _iDbConnection.State == ConnectionState.Closed;
            try
            {
                if (wasClosed)
                {
                    _iDbConnection.Open();

                }
            }
            catch (Exception ex)
            {
                throw new Exception("SqlConnectionWrapper: Error trying to open SQL connection. " + ex.Message);
            }


            ConnectionState currentState = _iDbConnection.State;
            if (currentState != ConnectionState.Open)
            {
                _iDbConnection.Open();
            }
        }
        //
        // Summary:
        //     Return a sequence of dynamic objects with properties matching the columns.
        //
        // Parameters:
        //   cnn:
        //     The connection to query on.
        //
        //   sql:
        //     The SQL to execute for the query.
        //
        //   param:
        //     The parameters to pass, if any.
        //
        //   transaction:
        //     The transaction to use, if any.
        //
        //   buffered:
        //     Whether to buffer the results in memory.
        //
        //   commandTimeout:
        //     The command timeout (in seconds).
        //
        //   commandType:
        //     The type of command to execute.
        //
        // Remarks:
        //     Note: each row can be accessed via "dynamic", or by casting to an IDictionary<string,object>
        public virtual IEnumerable<T> Query<T>(string sql, object param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            CheckConnection();
            return _iDbConnection.Query<T>(sql, param, transaction, buffered, commandTimeout, commandType);
        }


        //
        // Summary:
        //     Execute parameterized SQL.
        //
        // Parameters:
        //   cnn:
        //     The connection to query on.
        //
        //   sql:
        //     The SQL to execute for this query.
        //
        //   param:
        //     The parameters to use for this query.
        //
        //   transaction:
        //     The transaction to use for this query.
        //
        //   commandTimeout:
        //     Number of seconds before command execution timeout.
        //
        //   commandType:
        //     Is it a stored proc or a batch?
        //
        // Returns:
        //     The number of rows affected.
        public virtual int Execute(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            CheckConnection();
            return _iDbConnection.Execute(sql, param, transaction, commandTimeout, commandType);
        }

        //
        // Summary:
        //     Execute a command asynchronously using Task.
        //
        // Parameters:
        //   cnn:
        //     The connection to query on.
        //
        //   sql:
        //     The SQL to execute for this query.
        //
        //   param:
        //     The parameters to use for this query.
        //
        //   transaction:
        //     The transaction to use for this query.
        //
        //   commandTimeout:
        //     Number of seconds before command execution timeout.
        //
        //   commandType:
        //     Is it a stored proc or a batch?
        //
        // Returns:
        //     The number of rows affected.
        public virtual Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            CheckConnection();
            return _iDbConnection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
        }

        public virtual Task<int> ExecuteAsync(CommandDefinition command)
        {
            CheckConnection();
            return _iDbConnection.ExecuteAsync(command);
        }

        public virtual Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            CheckConnection();
            return _iDbConnection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
        }

        public virtual Task<IEnumerable<dynamic>> QueryAsync(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            CheckConnection();
            return _iDbConnection.QueryAsync(sql, param, transaction, commandTimeout, commandType);
        }

    }
}
