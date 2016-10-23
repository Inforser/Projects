namespace MiniORM.Core
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using MiniORM.Attributes;
    using MiniORM.Interfaces;

    public class EntityManager : IDbContext
    {
        private readonly IDatatypeMatcher datatypeMatcher;
        private readonly string connectionString;
        private readonly bool isCodeFirst;

        private SqlConnection connection;

        public EntityManager(IDatatypeMatcher datatypeMatcher, string connectionString, bool isCodeFirst)
        {
            this.datatypeMatcher = datatypeMatcher;
            this.connectionString = connectionString;
            this.isCodeFirst = isCodeFirst;
        }

        public bool Persist(object entityInst)
        {
            if (entityInst == null)
            {
                return false;
            }

            var entity = entityInst.GetType();
            if (!this.CheckIfTableExists(entity))
            {
                if (!this.isCodeFirst)
                {
                    return false;
                }

                var wasCreated = this.CreateTable(entity);
                if (!wasCreated)
                {
                    return false;
                }
            }

            var tableName = this.GetTableName(entity);

            FieldInfo primary = this.GetId(entity);
            object idValue = primary.GetValue(entityInst);

            if (idValue == null || (int)idValue <= 0)
            {
                return this.Insert(entityInst);
            }

            return this.Update(entityInst);
        }

        public T FindById<T>(int id)
        {
            var tableName = this.GetTableName(typeof(T));
            T result = default(T);
            var queryString =
                $"SELECT * FROM {tableName} " +
                "  WHERE Id = @id";

            using (this.connection = new SqlConnection(this.connectionString))
            {
                this.connection.Open();
                var command = new SqlCommand(queryString, this.connection);
                command.Parameters.AddWithValue("@id", id);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        throw new InvalidOperationException(
                            $"No entity was found with id {id}");
                    }

                    reader.Read();
                    result = this.CreateEntity<T>(reader);
                }

                this.connection.Close();
            }
            return result;
        }

        private T CreateEntity<T>(SqlDataReader reader)
        {
            object[] originalValues = new object[reader.FieldCount];
            reader.GetValues(originalValues);

            object[] values = new object[reader.FieldCount - 1];
            Array.Copy(originalValues, 1, values, 0, reader.FieldCount- 1);
            Type[] types = new Type[values.Length];
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = values[i].GetType();
            }

            T entityInstance = (T)typeof(T).GetConstructor(types)?.Invoke(values);

            if (entityInstance == null)
            {
                throw new ArgumentNullException();
            }

            typeof(T).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.IsDefined(typeof(IdAttribute)))?
                .SetValue(entityInstance, originalValues[0]);

            return entityInstance;
        }

        public IEnumerable<T> FindAll<T>()
        {
            var tableName = this.GetTableName(typeof(T));
            List<T> outputList = new List<T>();
            var queryString =
                $"SELECT * FROM {tableName}";

            using (this.connection = new SqlConnection(this.connectionString))
            {
                this.connection.Open();
                var command = new SqlCommand(queryString, this.connection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        outputList.Add(this.CreateEntity<T>(reader));
                    }
                }

                this.connection.Close();
            }
            return outputList;
        }

        public IEnumerable<T> FindAll<T>(string filter)
        {
            var tableName = this.GetTableName(typeof(T));
            List<T> outputList = new List<T>();
            var queryString =
                $"SELECT * FROM {tableName}";
            queryString += Environment.NewLine + filter;

            using (this.connection = new SqlConnection(this.connectionString))
            {
                this.connection.Open();
                var command = new SqlCommand(queryString, this.connection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        outputList.Add(this.CreateEntity<T>(reader));
                    }
                }

                this.connection.Close();
            }
            return outputList;
        }

        public T FindFirst<T>()
        {
            var tableName = this.GetTableName(typeof(T));
            T output = default(T);
            var queryString =
                $"SELECT TOP 1 * FROM {tableName}";
            
            using (this.connection = new SqlConnection(this.connectionString))
            {
                this.connection.Open();
                var command = new SqlCommand(queryString, this.connection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();

                    output = this.CreateEntity<T>(reader);
                }

                this.connection.Close();
            }
            return output;
        }

        public T FindFirst<T>(string filter)
        {
            var tableName = this.GetTableName(typeof(T));
            T output = default(T);
            var queryString =
                $"SELECT TOP 1 * FROM {tableName}";
            queryString += Environment.NewLine + filter;

            using (this.connection = new SqlConnection(this.connectionString))
            {
                this.connection.Open();
                var command = new SqlCommand(queryString, this.connection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();

                    output = this.CreateEntity<T>(reader);
                }

                this.connection.Close();
            }
            return output;
        }

        private FieldInfo GetId(Type entity)
        {
            var field = entity
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(x => x.IsDefined(typeof(IdAttribute)));
            if (field == null)
            {
                throw new InvalidOperationException(
                    "Cannot operate with entity without a primary key.");
            }

            return field;
        }

        private string GetTableName(Type entity)
        {
            var customAttr = entity.GetCustomAttribute<EntityAttribute>();

            if (customAttr == null)
            {
                throw new InvalidOperationException("The class is not defined with the Entity Attribute.");
            }

            var tableName = customAttr.TableName;
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(tableName, "The table's name cannot be null or whitespace.");
            }

            return tableName;
        }

        private string GetColumnName(FieldInfo field)
        {
            var customAttr = field.GetCustomAttribute<ColumnAttribute>();

            if (customAttr == null)
            {
                throw new InvalidOperationException("The field is not defined with the Column Attribute.");
            }

            var columnName = customAttr.Name;
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new ArgumentNullException(columnName, "The column's name cannot be null or whitespace.");
            }

            return columnName;
        }

        private bool CheckIfTableExists(Type entity)
        {
            var cmdString =
                "SELECT COUNT(name) FROM sys.sysobjects " +
                " WHERE [Name] = @tableName " +
                "   AND [xtype] = 'U'";

            var tableName = this.GetTableName(entity);

            var tablesCount = 0;
            using (this.connection = new SqlConnection(this.connectionString))
            {
                this.connection.Open();
                var command = new SqlCommand(cmdString, this.connection);
                command.Parameters.AddWithValue("@tableName", tableName);
                tablesCount = int.Parse(command.ExecuteScalar().ToString());

                this.connection.Close();
            }

            return tablesCount > 0;
        }

        private IEnumerable<string> GetColumnNames(Type entity)
        {
            return entity
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.IsDefined(typeof(ColumnAttribute)))
                .Select(this.GetColumnName);
        }

        private IEnumerable<string> GetColumnValues<T>(T entityInst)
        {
            var entity = entityInst.GetType(); 
            return entity.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(f => f.IsDefined(typeof(ColumnAttribute)))
                .Select(f =>
                {
                    var fieldtype = f.FieldType.Name;
                    if (fieldtype == "DateTime")
                    {
                        return ((DateTime)f.GetValue(entityInst)).ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    return f.GetValue(entityInst).ToString();
                })
                .Select(s => $"'{s}'");
        }

        private int GetLastId(string tableName)
        {
            var queryString =
                $"SELECT MAX(Id) FROM {tableName}";
            var output = 0;
            using (this.connection = new SqlConnection(this.connectionString))
            {
                this.connection.Open();
                var command = new SqlCommand(queryString, this.connection);
                output = (int)command.ExecuteScalar();
                if (output == 0)
                {
                    throw new ArgumentException("Could not retrieve Id number.");
                }

                this.connection.Close();
            }

            return output;
        }

        private bool CreateTable(Type entity)
        {
            var builder = new StringBuilder();
            var tableName = this.GetTableName(entity);

            builder.AppendLine($"CREATE TABLE {tableName} (");
            builder.AppendLine("Id INT IDENTITY PRIMARY KEY, ");

            string[] columnNames = this.GetColumnNames(entity).ToArray();

            string[] columnTypes = entity
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.IsDefined(typeof(ColumnAttribute)))
                .Select(f => this.datatypeMatcher.MatchSCharpToDb(f.FieldType.Name))
                .ToArray();

            var numOfColumns = columnNames.Length;
            for (int i = 0; i < numOfColumns; i++)
            {
                builder.AppendLine($"{columnNames[i]} {columnTypes[i]}, ");
            }

            builder.AppendLine(")");

            var result = 0;
            using (this.connection = new SqlConnection(this.connectionString))
            {
                this.connection.Open();
                var command = new SqlCommand(builder.ToString(), this.connection);

                result = command.ExecuteNonQuery();
                this.connection.Close();
            }

            return result > 0;
        }

        private bool Insert<T>(T entityInst)
        {
            var entity = entityInst.GetType();
            var cmdString = this.PrepareInsertString(entityInst);
            int rowsAffected = 0;
            using (this.connection = new SqlConnection(this.connectionString))
            {
                this.connection.Open();
                SqlCommand command = new SqlCommand(cmdString, this.connection);
                rowsAffected = command.ExecuteNonQuery();

                this.connection.Close();
            }

            int idNum = this.GetLastId(this.GetTableName(entity));
            this.GetId(entity).SetValue(entityInst, idNum);

            return rowsAffected > 0;
        }

        private string PrepareInsertString<T>(T entityInst)
        {
            var entity = entityInst.GetType();
            var tableName = this.GetTableName(entity);
            var columnNames = this.GetColumnNames(entity);
            var columnValues = this.GetColumnValues(entityInst);
            
            string cmdString =
                $"INSERT INTO {tableName} " +
                $"({string.Join(", ", columnNames)}) " +
                $"VALUES ({string.Join(", ", columnValues)})";

            return cmdString;
        }

        private bool Update<T>(T entityInst)
        {
            var cmdString = this.PrepareUpdateString(entityInst);

            var rowsAffected = 0;
            using (this.connection = new SqlConnection(this.connectionString))
            {
                this.connection.Open();
                var command = new SqlCommand(cmdString, this.connection);
                rowsAffected = command.ExecuteNonQuery();

                this.connection.Close();
            }

            return rowsAffected > 0;
        }

        private string PrepareUpdateString<T>(T entityInst)
        {
            var entity = entityInst.GetType();
            var tableName = this.GetTableName(entity);
            var updatePairs = this.GetUpdatePairsString(entityInst);
            var id = (int)this.GetId(entity).GetValue(entityInst);

            string cmdString =
                $"UPDATE {tableName} " +
                $"   SET {updatePairs} " +
                $" WHERE Id = {id}";

            return cmdString;
        }

        private string GetUpdatePairsString<T>(T entityInst)
        {
            var entity = entityInst.GetType();
            var columnNames = this.GetColumnNames(entity).ToArray();
            var columnValues = this.GetColumnValues(entityInst).ToArray();
            var numOfColumns = columnNames.Length;
            var pairList = new List<string>();
            
            for (int i = 0; i < numOfColumns; i++)
            {
                pairList.Add($"{columnNames[i]} = {columnValues[i]}");
            }

            return string.Join(", ", pairList);
        }
    }
}
