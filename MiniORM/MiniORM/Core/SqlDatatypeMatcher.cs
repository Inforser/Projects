namespace MiniORM.Core
{
    using System;
    using MiniORM.Interfaces;

    public class SqlDatatypeMatcher : IDatatypeMatcher
    {
        public string MatchSCharpToDb(string csDatatype)
        {
            switch (csDatatype)
            {
                case "Int32":
                    return "INT";
                case "Int64":
                    return "BIGINT";
                case "String":
                    return "VARCHAR(MAX)";
                case "DateTime":
                    return "DATETIME";
                case "Boolean":
                    return "BIT";
                case "Double":
                    return "DECIMAL";
                default: throw new ArgumentException("The datatype could not be matched to a coresponding DB datatype.");
            }
        }
    }
}
