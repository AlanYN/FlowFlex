using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FlowFlex.Domain.Shared.DynamicConverter.Interfaces;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Extensions;

namespace FlowFlex.Domain.Shared.Utils;

public class LogicExpression
{
    public LogicEnum Logic { get; set; }

    public string FieldName { get; set; }

    public Func<string, string> FieldConvertFunc { get; set; }

    public DataType? FieldType { get; set; }

    public bool IsBusinessId { get; set; }

    public object FieldValue { get; set; }

    public LogicExpression[] Condition { get; set; }

    private readonly Dictionary<string, object> _parameters = [];
    private int _parameterIndex = 0;
    private string _tableName = string.Empty;
    private bool _isMapField = false;

    /// <summary>
    /// Build SQL WHERE statement
    /// </summary>
    /// <returns>Return SQL statement and parameter dictionary</returns>
    public (string Sql, Dictionary<string, object> Parameters) BuildSqlWhere(string tableName = null, bool isMapField = false, Func<ConditionFunc, string> func = null)
    {
        _tableName = string.IsNullOrEmpty(tableName) ? string.Empty : $"{tableName}.";
        _isMapField = isMapField;
        var sql = BuildExpression(this, func);
        return (sql, _parameters);
    }

    public static LogicExpression Create(string fieldName, DataType fieldType, LogicEnum logic, object value = null)
    {
        return new LogicExpression()
        {
            FieldName = fieldName,
            FieldType = fieldType,
            Logic = logic,
            FieldValue = value
        };
    }

    public static LogicExpression CreateID(LogicEnum logic, object value)
    {
        return new LogicExpression()
        {
            IsBusinessId = true,
            FieldName = "id",
            FieldType = DataType.ID,
            Logic = logic,
            FieldValue = value
        };
    }

    public static LogicExpression CreateAnd(params LogicExpression[] expressions)
    {
        return new LogicExpression()
        {
            Logic = LogicEnum.And,
            Condition = expressions
        };
    }

    public static LogicExpression CreateOr(params LogicExpression[] expressions)
    {
        return new LogicExpression()
        {
            Logic = LogicEnum.Or,
            Condition = expressions
        };
    }

    private string GetFieldName(LogicExpression expression)
    {
        if (_isMapField)
        {
            string fieldName = expression.FieldName;
            if (expression.FieldType.HasValue && expression.IsBusinessId == false)
            {
                fieldName = $"{_tableName}{expression.FieldType.Value.GetDbField()}";
            }
            else if (expression.IsBusinessId == false)
            {
                fieldName = $"{_tableName}{expression.FieldName}";
            }

            if (expression.FieldConvertFunc != null)
            {
                fieldName = expression.FieldConvertFunc.Invoke(fieldName);
            }

            return fieldName;
        }
        else
        {
            if (expression.FieldConvertFunc != null)
            {
                return expression.FieldConvertFunc.Invoke(expression.FieldName);
            }
            return expression.FieldName;
        }
    }

    /// <summary>
    /// Recursively build expression
    /// </summary>
    private string BuildExpression(LogicExpression expression, Func<ConditionFunc, string> func)
    {
        // Handle composite conditions
        if (expression.Condition != null && expression.Condition.Length != 0)
        {
            var conditions = expression.Condition
                .Select(x => BuildExpression(x, func))
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            if (conditions.Count == 0)
            {
                return string.Empty;
            }

            var logicOperator = expression.Logic == LogicEnum.Or ? " OR " : " AND ";
            return $"({string.Join(logicOperator, conditions)})";
        }

        // Handle single condition
        if (string.IsNullOrEmpty(expression.FieldName))
        {
            return string.Empty;
        }

        var result = string.Empty;
        if (_isMapField)
        {
            if (expression.IsBusinessId)
            {
                result = BuildSingleCondition(expression);
            }
            else if (_isMapField)
                result = $"({_tableName}field_name = '{expression.FieldName}' AND {BuildSingleCondition(expression)})";
        }
        else
        {
            result = BuildSingleCondition(expression);
        }

        if (func != null)
            result = func(new ConditionFunc()
            {
                Expression = expression,
                Condition = result
            });

        return result;
    }

    /// <summary>
    /// Build single condition
    /// </summary>
    private string BuildSingleCondition(LogicExpression expression)
    {
        var parameterName = $"@p{_parameterIndex++}";
        var fieldName = GetFieldName(expression);

        switch (expression.Logic)
        {
            case LogicEnum.Equal:
                _parameters[parameterName] = ChangeValueToFieldType(expression.FieldValue, expression.FieldType.Value); //expression.FieldType.GetFieldValue(expression.FieldValue);
                return $"{fieldName} = {parameterName}";

            case LogicEnum.NotEqual:
                _parameters[parameterName] = ChangeValueToFieldType(expression.FieldValue, expression.FieldType.Value);
                return $"{fieldName} != {parameterName}";

            case LogicEnum.GreaterThan:
                _parameters[parameterName] = ChangeValueToFieldType(expression.FieldValue, expression.FieldType.Value);
                return $"{fieldName} > {parameterName}";

            case LogicEnum.GreaterThanOrEqual:
                _parameters[parameterName] = ChangeValueToFieldType(expression.FieldValue, expression.FieldType.Value);
                return $"{fieldName} >= {parameterName}";

            case LogicEnum.LessThan:
                _parameters[parameterName] = ChangeValueToFieldType(expression.FieldValue, expression.FieldType.Value);
                return $"{fieldName} < {parameterName}";

            case LogicEnum.LessThanOrEqual:
                _parameters[parameterName] = ChangeValueToFieldType(expression.FieldValue, expression.FieldType.Value);
                return $"{fieldName} <= {parameterName}";

            case LogicEnum.In:
                {
                    var ps = GetParameters(expression);
                    if (ps.Count == 0)
                        return "false";
                    return $"{fieldName} IN ({string.Join(",", ps)})";
                }

            case LogicEnum.NotIn:
                {
                    var ps = GetParameters(expression);
                    if (ps.Count == 0)
                        return "false";
                    return $"{fieldName} NOT IN ({string.Join(",", ps)})";
                }

            case LogicEnum.Like:
                _parameters[parameterName] = $"%{ChangeValueToFieldType(expression.FieldValue, expression.FieldType.Value)}%";
                return $"{fieldName} ILIKE {parameterName}";

            case LogicEnum.LikeLeft:
                _parameters[parameterName] = $"%{ChangeValueToFieldType(expression.FieldValue, expression.FieldType.Value)}";
                return $"{fieldName} ILIKE {parameterName}";

            case LogicEnum.LikeRight:
                _parameters[parameterName] = $"{ChangeValueToFieldType(expression.FieldValue, expression.FieldType.Value)}%";
                return $"{fieldName} ILIKE {parameterName}";

            case LogicEnum.NoLike:
                _parameters[parameterName] = $"%{ChangeValueToFieldType(expression.FieldValue, expression.FieldType.Value)}%";
                return $"{fieldName} NOT ILIKE {parameterName}";

            case LogicEnum.IsNullOrEmpty:
                return $"({fieldName} IS NULL OR {fieldName} = '')";

            case LogicEnum.IsNull:
                return $"{fieldName} IS NULL";

            case LogicEnum.IsNotNull:
                return $"{fieldName} IS NOT NULL";

            case LogicEnum.IsNotNullAndNotEmpty:
                return $"{fieldName} IS NOT NULL AND {fieldName} != ''";

            case LogicEnum.InLike:
                {
                    var parameters = GetParameters(expression);
                    if (parameters.Count == 0)
                        return "false";

                    var conditions = parameters.Select(p =>
                    {
                        return $"{fieldName} ILIKE {p}";
                    });
                    return $"({string.Join(" OR ", conditions)})";
                }

            case LogicEnum.Range:
                if (expression.FieldValue is IEnumerable rangeValues)
                {
                    List<object> values = [];
                    foreach (var item in rangeValues)
                        values.Add(item);

                    values = [.. values.Take(2)];
                    if (values.Count == 2)
                    {
                        var startParam = $"@p{_parameterIndex++}";
                        var endParam = $"@p{_parameterIndex++}";
                        _parameters[startParam] = expression.FieldType.ConvertValue(values[0]);
                        _parameters[endParam] = expression.FieldType.ConvertValue(values[1]);
                        return $"{fieldName} BETWEEN {startParam} AND {endParam}";
                    }
                }
                else if (expression.FieldValue is ITuple tuple)
                {
                    if (tuple.Length != 2)
                        throw new CRMException("Between and", ErrorCodeEnum.SystemError);
                    var startParam = $"@p{_parameterIndex++}";
                    var endParam = $"@p{_parameterIndex++}";
                    _parameters[startParam] = expression.FieldType.ConvertValue(tuple[0]);
                    _parameters[endParam] = expression.FieldType.ConvertValue(tuple[1]);
                    return $"{fieldName} BETWEEN {startParam} AND {endParam}";
                }
                return string.Empty;

            default:
                return string.Empty;
        }
    }

    private static object ChangeValueToFieldType(object value, DataType dataType)
    {
        var helper = ConverterHelper.GetConverter(dataType);
        return helper.Convert(value);

    }

    private List<string> GetParameters(LogicExpression expression, Func<object, object> format = null)
    {
        if (expression.FieldValue is IEnumerable inValues)
        {
            var parameters = new List<string>();
            foreach (var item in inValues)
            {
                var pName = $"@p{_parameterIndex++}";
                var value = expression.FieldType.ConvertValue(item);
                _parameters[pName] = format != null ? format(value) : value;
                parameters.Add(pName);
            }
            return parameters;
        }
        return [];
    }
}

public class ConditionFunc
{
    public LogicExpression Expression { get; set; }

    public string Condition { get; set; }
}

public enum LogicEnum
{
    /// <summary>
    /// And
    /// </summary>
    And = 1,

    /// <summary>
    /// Or
    /// </summary>
    Or = 2,

    /// <summary>
    /// Equal
    /// </summary>
    Equal = 3,

    /// <summary>
    /// Not equal
    /// </summary>
    NotEqual = 4,

    /// <summary>
    /// Greater than
    /// </summary>
    GreaterThan = 5,

    /// <summary>
    /// Greater than or equal
    /// </summary>
    GreaterThanOrEqual = 6,

    /// <summary>
    /// Less than
    /// </summary>
    LessThan = 7,

    /// <summary>
    /// Less than or equal
    /// </summary>
    LessThanOrEqual = 8,

    /// <summary>
    /// In [......] list
    /// </summary>
    In = 9,

    /// <summary>
    /// Not in [....] list
    /// </summary>
    NotIn = 10,

    /// <summary>
    /// Fuzzy match
    /// </summary>
    Like = 11,

    /// <summary>
    /// Left fuzzy match
    /// </summary>
    LikeLeft = 12,

    /// <summary>
    /// Right fuzzy match
    /// </summary>
    LikeRight = 13,

    /// <summary>
    /// Not equal (alternative)
    /// </summary>
    NoEqual = 14,

    /// <summary>
    /// Is NULL or empty
    /// </summary>
    IsNullOrEmpty = 15,

    /// <summary>
    /// Is not NULL (can only be used on NULL)
    /// </summary>
    IsNotNull = 16,

    /// <summary>
    /// Fuzzy match negation
    /// </summary>
    NoLike = 17,

    /// <summary>
    /// Is NULL (can only be used on NULL)
    /// </summary>
    IsNull = 18,

    /// <summary>
    /// Fuzzy match in [....] list
    /// </summary>
    InLike = 19,

    /// <summary>
    /// Within a range
    /// </summary>
    Range = 20,

    /// <summary>
    /// Used on TimeLine: 
    /// start >= filter.start && end <= filter.end
    /// </summary>
    WithIn = 21,

    /// <summary>
    /// Used on TimeLine: 
    /// start <= filter.start && end >= filter.end
    /// </summary>
    WithOut = 22,

    /// <summary>
    /// Can only be used on string type conditions
    /// </summary>
    IsNotNullAndNotEmpty = 32
}
