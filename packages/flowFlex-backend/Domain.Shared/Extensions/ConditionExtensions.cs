using Dm;
using SqlSugar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.DynamicData;
using static FlowFlex.Domain.Shared.Extensions.SqlSugarExtensions;

namespace FlowFlex.Domain.Shared.Extensions;

public static class ConditionExtensions
{
    public static Expression<Func<T, bool>> ToExpression<T>(this QueryConditionModel query) where T : class, new()
    {
        var exp = Expressionable.Create<T>();

        foreach (var condition in query.Condition)
        {
            var fieldName = condition.FieldName;
            var list = condition.CustomExpList;

            foreach (var item in list)
            {
                var p = Expression.Parameter(typeof(T), "x");
                var name = GetPropertyName<T>(fieldName) ?? throw new Exception($"Invalid field name: {fieldName}");

                var prop = Expression.Property(p, name);

                Expression body = null;

                if (item.Operation == Operation.Equal || item.Operation == Operation.Is)
                {
                    body = Expression.Equal(prop, Expression.Constant(item.Value));
                }
                else if (item.Operation == Operation.GreaterThan)
                {
                    body = Expression.GreaterThan(prop, Expression.Constant(item.Value));
                }
                else if (item.Operation == Operation.GtEqual)
                {
                    body = Expression.GreaterThanOrEqual(prop, Expression.Constant(item.Value));
                }
                else if (item.Operation == Operation.LessThan)
                {
                    body = Expression.LessThan(prop, Expression.Constant(item.Value));
                }
                else if (item.Operation == Operation.LtEqual)
                {
                    body = Expression.LessThanOrEqual(prop, Expression.Constant(item.Value));
                }
                else if (item.Operation == Operation.Like)
                {
                    // TODO: Implement Like operation
                    throw new NotImplementedException("Like operation not implemented");
                }
                else if (item.Operation == Operation.In)
                {
                    throw new NotImplementedException();
                }

                if (body != null)
                {
                    var ppp = Expression.Lambda<Func<T, bool>>(body, p);
                    exp.And(ppp);
                }
            }
        }

        return exp.ToExpression();
    }

    public static Expression<Func<T, bool>> ToDataExpression<T>(this QueryConditionModel query) where T : class, IDynamicValue, new()
    {
        var exp = Expressionable.Create<T>();

        foreach (var condition in query.Condition)
        {
            var fieldName = condition.FieldName;
            var list = condition.CustomExpList;
            var fieldType = GetFieldType(query.FieldMappings, fieldName);

            var internalExp = Expressionable.Create<T>();
            foreach (var item in list)
            {
                if (item.Logical == LogicalOperation.Or)
                {
                    if (fieldType == DataType.Email || fieldType == DataType.Phone)
                    {
                        if (item.Operation == Operation.Equal)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.Or(x => x.FieldName == fieldName && x.Varchar100Value == strValue);
                        }
                        else if (item.Operation == Operation.Like)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.Or(x => x.FieldName == fieldName && x.Varchar100Value.Contains(strValue));
                        }
                        else if (item.Operation == Operation.NotEqual)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.Or(x => x.FieldName == fieldName && x.Varchar100Value != strValue);
                        }
                        else if (item.Operation == Operation.In)
                        {
                            var strLists = item.Value as IEnumerable<string>;
                            internalExp.Or(x => x.FieldName == fieldName && strLists.Contains(x.Varchar100Value));
                        }
                    }
                    else if (fieldType == DataType.SingleLineText)
                    {
                        if (item.Operation == Operation.Equal)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.Or(x => x.FieldName == fieldName && x.Varchar500Value == strValue);
                        }
                        else if (item.Operation == Operation.Like)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.Or(x => x.FieldName == fieldName && x.Varchar500Value.Contains(strValue));
                        }
                        else if (item.Operation == Operation.NotEqual)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.Or(x => x.FieldName == fieldName && x.Varchar500Value != strValue);
                        }
                        else if (item.Operation == Operation.In)
                        {
                            var strLists = item.Value as IEnumerable<string>;
                            internalExp.Or(x => x.FieldName == fieldName && strLists.Contains(x.Varchar500Value));
                        }
                    }
                    else if (fieldType == DataType.MultilineText)
                    {
                        if (item.Operation == Operation.Equal)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.Or(x => x.FieldName == fieldName && x.TextValue == strValue);
                        }
                        else if (item.Operation == Operation.Like)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.Or(x => x.FieldName == fieldName && x.TextValue.Contains(strValue));
                        }
                        else if (item.Operation == Operation.NotEqual)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.Or(x => x.FieldName == fieldName && x.TextValue != strValue);
                        }
                        else if (item.Operation == Operation.In)
                        {
                            var strLists = item.Value as IEnumerable<string>;
                            internalExp.Or(x => x.FieldName == fieldName && strLists.Contains(x.TextValue));
                        }
                    }
                    else if (fieldType == DataType.Number)
                    {
                        var doubleValue = Convert.ToDouble(item.Value);
                        if (item.Operation == Operation.Equal)
                        {
                            internalExp.Or(x => x.FieldName == fieldName && x.DoubleValue == doubleValue);
                        }

                        else if (item.Operation == Operation.GreaterThan)
                        {
                            internalExp.Or(x => x.FieldName == fieldName && x.DoubleValue > doubleValue);
                        }
                        else if (item.Operation == Operation.GtEqual)
                        {
                            internalExp.Or(x => x.FieldName == fieldName && x.DoubleValue >= doubleValue);
                        }
                        else if (item.Operation == Operation.LessThan)
                        {
                            internalExp.Or(x => x.FieldName == fieldName && x.DoubleValue < doubleValue);
                        }
                        else if (item.Operation == Operation.LtEqual)
                        {
                            internalExp.Or(x => x.FieldName == fieldName && x.DoubleValue <= doubleValue);
                        }
                        else if (item.Operation == Operation.NotEqual)
                        {
                            internalExp.Or(x => x.FieldName == fieldName && x.DoubleValue != doubleValue);
                        }
                        else if (item.Operation == Operation.In)
                        {
                            var doubles = new List<double?>();
                            var value = item.Value as IList<double>;
                            foreach (var v in value)
                            {
                                doubles.Add(v);
                            }
                            internalExp.Or(x => x.FieldName == fieldName && doubles.Contains(x.DoubleValue));
                        }
                    }
                    else if (fieldType == DataType.ID && fieldName == "id")
                    {
                        if (item.Operation == Operation.In)
                        {
                            var ids = new List<long?>();
                            var value = item.Value as IList<long>;
                            foreach (var v in value)
                            {
                                ids.Add(v);
                            }

                            internalExp.Or(x => ids.Contains(x.BusinessId));
                        }
                        else
                        {
                            var longValue = Convert.ToInt64(item.Value);
                            internalExp.Or(x => x.BusinessId == longValue);
                        }
                    }
                    else if (fieldType == DataType.ID)
                    {
                        if (item.Operation == Operation.In)
                        {
                            var ids = new List<long?>();
                            var value = item.Value as IList<long>;
                            foreach (var v in value)
                            {
                                ids.Add(v);
                            }

                            internalExp.Or(x => x.FieldName == fieldName && ids.Contains(x.LongValue));
                        }
                        else
                        {
                            var longValue = Convert.ToInt64(item.Value);
                            internalExp.Or(x => x.FieldName == fieldName && x.LongValue == longValue);
                        }
                    }
                }
                else
                {
                    if (fieldType == DataType.Email || fieldType == DataType.Phone)
                    {
                        if (item.Operation == Operation.Equal)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.And(x => x.FieldName == fieldName && x.Varchar100Value == strValue);
                        }
                        else if (item.Operation == Operation.Like)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.And(x => x.FieldName == fieldName && ILike(x.Varchar100Value, strValue));
                        }
                        else if (item.Operation == Operation.NotEqual)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.And(x => x.FieldName == fieldName && x.Varchar100Value != strValue);
                        }
                        else if (item.Operation == Operation.In)
                        {
                            var strLists = item.Value as IEnumerable<string>;
                            internalExp.And(x => x.FieldName == fieldName && strLists.Contains(x.Varchar100Value));
                        }
                    }
                    else if (fieldType == DataType.SingleLineText)
                    {
                        if (item.Operation == Operation.Equal)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.And(x => x.FieldName == fieldName && x.Varchar500Value == strValue);
                        }
                        else if (item.Operation == Operation.Like)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.And(x => x.FieldName == fieldName && ILike(x.Varchar500Value, strValue));
                        }
                        else if (item.Operation == Operation.NotEqual)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.And(x => x.FieldName == fieldName && x.Varchar500Value != strValue);
                        }
                        else if (item.Operation == Operation.In)
                        {
                            var strLists = item.Value as IEnumerable<string>;
                            internalExp.And(x => x.FieldName == fieldName && strLists.Contains(x.Varchar500Value));
                        }
                    }
                    else if (fieldType == DataType.MultilineText)
                    {
                        if (item.Operation == Operation.Equal)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.And(x => x.FieldName == fieldName && x.TextValue == strValue);
                        }
                        else if (item.Operation == Operation.Like)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.And(x => x.FieldName == fieldName && ILike(x.TextValue, strValue));
                        }
                        else if (item.Operation == Operation.NotEqual)
                        {
                            var strValue = item.Value?.ToString();
                            internalExp.And(x => x.FieldName == fieldName && x.TextValue != strValue);
                        }
                        else if (item.Operation == Operation.In)
                        {
                            var strLists = item.Value as IEnumerable<string>;
                            internalExp.And(x => x.FieldName == fieldName && strLists.Contains(x.TextValue));
                        }
                    }
                    else if (fieldType == DataType.Number)
                    {
                        var doubleValue = Convert.ToDouble(item.Value);
                        if (item.Operation == Operation.Equal)
                        {
                            internalExp.And(x => x.FieldName == fieldName && x.DoubleValue == doubleValue);
                        }

                        else if (item.Operation == Operation.GreaterThan)
                        {
                            internalExp.And(x => x.FieldName == fieldName && x.DoubleValue > doubleValue);
                        }
                        else if (item.Operation == Operation.GtEqual)
                        {
                            internalExp.And(x => x.FieldName == fieldName && x.DoubleValue >= doubleValue);
                        }
                        else if (item.Operation == Operation.LessThan)
                        {
                            internalExp.And(x => x.FieldName == fieldName && x.DoubleValue < doubleValue);
                        }
                        else if (item.Operation == Operation.LtEqual)
                        {
                            internalExp.And(x => x.FieldName == fieldName && x.DoubleValue <= doubleValue);
                        }
                        else if (item.Operation == Operation.NotEqual)
                        {
                            internalExp.And(x => x.FieldName == fieldName && x.DoubleValue != doubleValue);
                        }
                        else if (item.Operation == Operation.In)
                        {
                            var doubles = new List<double?>();
                            var value = item.Value as IList<double>;
                            foreach (var v in value)
                            {
                                doubles.Add(v);
                            }
                            internalExp.And(x => x.FieldName == fieldName && doubles.Contains(x.DoubleValue));
                        }
                    }
                    else if (fieldType == DataType.ID && fieldName == "id")
                    {
                        if (item.Operation == Operation.In)
                        {
                            var ids = new List<long?>();
                            var value = item.Value as IList<long>;
                            foreach (var v in value)
                            {
                                ids.Add(v);
                            }

                            internalExp.And(x => ids.Contains(x.BusinessId));
                        }
                        else
                        {
                            var longValue = Convert.ToInt64(item.Value);
                            internalExp.And(x => x.BusinessId == longValue);
                        }
                    }
                    else if (fieldType == DataType.ID)
                    {
                        if (item.Operation == Operation.In)
                        {
                            var ids = new List<long?>();
                            var value = item.Value as IList<long>;
                            foreach (var v in value)
                            {
                                ids.Add(v);
                            }

                            internalExp.And(x => x.FieldName == fieldName && ids.Contains(x.LongValue));
                        }
                        else
                        {
                            var longValue = Convert.ToInt64(item.Value);
                            internalExp.And(x => x.FieldName == fieldName && x.LongValue == longValue);
                        }
                    }
                }
            }

            if (condition.Logical == LogicalOperation.Or)
            {
                exp.Or(internalExp.ToExpression());
            }
            else
            {
                exp.And(internalExp.ToExpression());
            }
        }

        return exp.ToExpression();
    }

    public static string ToSqlWhere(this QueryConditionModel query, string tableName = null)
    {
        var conditions = new List<string>();

        var publicIndex = 0;
        foreach (var condition in query.Condition)
        {
            var fieldName = condition.FieldName;
            var internalIndex = 0;
            foreach (var exp in condition.CustomExpList)
            {
                var field = query.FieldMappings.FirstOrDefault(x => x.FieldName == fieldName);
                string sqlCondition = exp.Operation switch
                {
                    Operation.Equal =>
                        $"{BuildTableName(tableName, fieldName, field.FieldType)} = {FormatValue(Operation.Equal, field.FieldType, exp.Value)}",
                    Operation.NotEqual =>
                        $"{BuildTableName(tableName, fieldName, field.FieldType)} != {FormatValue(Operation.NotEqual, field.FieldType, exp.Value)}",
                    Operation.GreaterThan =>
                        $"{BuildTableName(tableName, fieldName, field.FieldType)} > {FormatValue(Operation.GreaterThan, field.FieldType, exp.Value)}",
                    Operation.GtEqual =>
                        $"{BuildTableName(tableName, fieldName, field.FieldType)} >= {FormatValue(Operation.GtEqual, field.FieldType, exp.Value)}",
                    Operation.LtEqual =>
                        $"{BuildTableName(tableName, fieldName, field.FieldType)} <= {FormatValue(Operation.LtEqual, field.FieldType, exp.Value)}",
                    Operation.LessThan =>
                        $"{BuildTableName(tableName, fieldName, field.FieldType)} < {FormatValue(Operation.LessThan, field.FieldType, exp.Value)}",
                    Operation.Like =>
                        $"{BuildTableName(tableName, fieldName, field.FieldType)} ILIKE {FormatValue(Operation.Like, field.FieldType, exp.Value)}",
                    Operation.Is =>
                        $"{BuildTableName(tableName, fieldName, field.FieldType)} IS {FormatValue(Operation.Is, field.FieldType, exp.Value)}",
                    Operation.In =>
                        $"{BuildTableName(tableName, fieldName, field.FieldType)} IN ({string.Join(", ", ((IList)exp.Value).Cast<object>().Select(x => FormatValue(Operation.In, field.FieldType, x)))})",
                    _ => throw new NotSupportedException($"Operation {exp.Operation} is not supported.")
                };

                conditions.Add(sqlCondition);
                if (internalIndex < condition.CustomExpList.Count - 1)
                    conditions.Add(exp.Logical == LogicalOperation.Or ? "OR" : "AND");

            }

            if (publicIndex < query.Condition.Count - 1)
                conditions.Add(condition.Logical == LogicalOperation.Or ? "OR" : "AND");
        }

        return string.Join($" ", conditions);
    }

    private static string BuildTableName(string tableName, string fieldName, DataType? fieldType)
    {
        if (fieldType.HasValue)
        {
            fieldName = $"{fieldType.Value.GetDbField()}";
            if (fieldName == DbFieldName.StringListValue)
            {
                fieldName = $"{fieldName}::text";
            }
        }

        return $"{(string.IsNullOrWhiteSpace(tableName) ? string.Empty : tableName + ".")}{fieldName}";
    }

    private static string FormatValue(Operation operation, DataType? fieldType, object value)
    {
        if (value == null) return "NULL";

        if (fieldType.HasValue)
        {
            var dbValue = fieldType.Value switch
            {
                DataType.DateTime => $"'{value:yyyy-MM-dd HH:mm:ss zzz}'",
                DataType.SingleLineText => $"'{value}'",
                DataType.Email => $"'{value}'",
                DataType.Phone => $"'{value}'",
                DataType.MultilineText => $"'{value}'",
                _ => $"{value}"
            };

            return dbValue;
        }
        else
        {
            return value switch
            {
                string str => $"'{str}'",
                DateTime dateTime => $"'{dateTime:yyyy-MM-dd HH:mm:ss}'",
                DateTimeOffset dateTime => $"'{dateTime:yyyy-MM-dd HH:mm:ss zzz}'",
                _ => value.ToString()
            };
        }
    }

    private static DataType GetFieldType(IEnumerable<IField> fieldMappings, string fieldName)
    {
        if (fieldName == "id")
            return DataType.ID;

        return fieldMappings.First(x => x.FieldName == fieldName).FieldType;
    }

    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> self, Expression<Func<T, bool>> addExp)
    {
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(self.Body, addExp.Body), self.Parameters);
    }

    private static string GetPropertyName<T>(string name)
    {
        var prop = typeof(T).GetProperties().FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (prop != null)
            return prop.Name;

        return null;
    }
}
