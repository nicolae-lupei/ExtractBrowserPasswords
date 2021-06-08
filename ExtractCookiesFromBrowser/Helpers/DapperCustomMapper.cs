using System;
using System.Collections.Concurrent;
using System.Linq;
using Dapper;

namespace ExtractCookiesFromBrowser.Helpers
{
    internal static class DapperCustomMapper
    {
        /// <summary>
        /// Contains registered entities
        /// </summary>
        private static readonly ConcurrentStack<Type> Entities = new ConcurrentStack<Type>();

        /// <summary>
        /// Register custom map
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        public static void RegisterCustomMap<TEntity>()
        {
            var entityType = typeof(TEntity);
            if (Entities.Contains(entityType)) return;
            if (!entityType.IsClass || entityType == typeof(string) || entityType.IsArray || entityType.IsPrimitive) return;
            SqlMapper.SetTypeMap(
                entityType,
                new CustomPropertyTypeMap(
                    typeof(TEntity),
                    (type, columnName) =>
                    {
                        return type.GetProperties()
                            .FirstOrDefault(prop =>
                                prop.Name == columnName
                                || prop.GetCustomAttributes(false)
                                    .OfType<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()
                                    .Any(x => x.Name == columnName));
                    }));

            Entities.Push(entityType);
        }
    }
}