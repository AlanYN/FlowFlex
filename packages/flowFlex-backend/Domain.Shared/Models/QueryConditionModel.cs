using System;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Models.DynamicData;
using FlowFlex.Domain.Shared.Utils;

namespace FlowFlex.Domain.Shared.Models;

/*
 * {
 *      "pageIndex": 1,
 *      "pageSize": 10,
 *      "condition": [
 *          { 
 *              "fieldName": "address", 
 *              "customExpList": [
 *                  { 
 *                      "operation": "=",
 *                      "value": "BeiJing"
 *                  }
 *              ] 
 *          }
 *      ],
 *      "sortProperties": ["address"]
 * }
 */

public interface IField
{
    long Id { get; set; }

    string FieldName { get; set; }

    bool IsComputed { get; set; }

    AdditionalInfo AdditionalInfo { get; set; }

    DataType FieldType { get; }
}

public class DefaultField : IField
{
    public long Id { get; set; }

    public string FieldName { get; set; }

    public bool IsComputed { get; set; }

    public AdditionalInfo AdditionalInfo { get; set; } = new();

    public DataType FieldType { get; set; }
}

public class QueryConditionBuilder
{
    private readonly QueryConditionModel _queryCondition;

    public QueryConditionBuilder()
    {
        _queryCondition = new QueryConditionModel()
        {
            Condition = [],
            SortProperties = [],
            SelectFields = []
        };
    }

    [Obsolete("Please use AddWhere")]
    public QueryConditionBuilder AddCondition(string fieldName, params CustomExp[] conditions)
    {
        _queryCondition.Condition.Add(new ConditionModel()
        {
            FieldName = fieldName,
            CustomExpList = [.. conditions]
        });

        return this;
    }

    [Obsolete("Please use AddWhere")]
    public QueryConditionBuilder AddCondition(string fieldName, Operation operation, object value)
    {
        _queryCondition.Condition.Add(new ConditionModel()
        {
            FieldName = fieldName,
            CustomExpList = [new CustomExp() { Operation = operation, Value = value }]
        });

        return this;
    }

    /// <summary>
    /// Can only be used once, for multiple expressions please use LogicExpression.Create to build
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public QueryConditionBuilder AddWhere(LogicExpression expression)
    {
        _queryCondition.LogicExpression = expression;
        return this;
    }

    public QueryConditionBuilder SelectFields(string[] fields)
    {
        _queryCondition.SelectFields = [.. fields];
        return this;
    }

    public QueryConditionBuilder UsePage(int pageIndex, int pageSize)
    {
        _queryCondition.PageIndex = pageIndex;
        _queryCondition.PageSize = pageSize;
        return this;
    }

    public QueryConditionBuilder UseOrderby(params SortColumn[] columns)
    {
        _queryCondition.SortProperties = [.. columns];
        return this;
    }

    public QueryConditionModel Build()
    {
        return _queryCondition;
    }

    public QueryConditionModel Build(List<IField> fieldMappings)
    {
        _queryCondition.FieldMappings = fieldMappings;
        return _queryCondition;
    }
}

public class QueryConditionModel
{
    public IEnumerable<IField> FieldMappings { get; set; } = [];

    [Obsolete("No longer used, will be removed in future")]
    public List<ConditionModel> Condition { get; set; } = [];

    public LogicExpression LogicExpression { get; set; }

    public int PageIndex { get; set; }

    public int PageSize { get; set; }

    public List<SortColumn> SortProperties { get; set; } = [];

    public List<string> SelectFields { get; set; } = [];
}

public class ConditionModel
{
    public string FieldName { get; set; }

    public List<CustomExp> CustomExpList { get; set; }

    /// <summary>
    /// Logical representation with the next expression
    /// </summary>
    public LogicalOperation Logical { get; set; } = LogicalOperation.And;
}

public class CustomExp
{
    public Operation Operation { get; set; }

    public object Value { get; set; }

    public LogicalOperation Logical { get; set; }
}

public enum LogicalOperation
{
    Or,
    And
}

public enum Operation
{
    [Operation("in")]
    In,

    [Operation("is")]
    Is,

    [Operation("=")]
    Equal,

    [Operation(">")]
    GreaterThan,

    [Operation("<")]
    LessThan,

    [Operation(">=")]
    GtEqual,

    [Operation("<>")]
    NotEqual,

    [Operation("<=")]
    LtEqual,

    [Operation("ilike")]
    Like,

    [Operation("Intelligence")]
    Intelligence
}

[AttributeUsage(AttributeTargets.Field)]
public class OperationAttribute : Attribute
{
    public string Operation { get; private set; }

    public OperationAttribute(string value)
    {
        Operation = value;
    }
}

public static class OperationExtension
{
    public static string GetExp(this Operation operation)
    {
        var field = typeof(Operation).GetField(operation.ToString());
        var attr = (OperationAttribute)Attribute.GetCustomAttribute(field, typeof(OperationAttribute));
        return attr.Operation;
    }
}

public class SortColumn
{
    public string Name { get; set; }

    public SortDirection Dirction { get; set; }
}

public enum SortDirection
{
    Asc = 1,
    Desc = 2
}
