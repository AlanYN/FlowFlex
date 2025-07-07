using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System;
using System.Collections;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Enums.Product;
using FlowFlex.Domain.Shared.Models.Models;

namespace FlowFlex.Domain.Shared.Models.LineItem;

public class LineItemDto : DynamicModelBase
{
    public override int ModuleId => ModuleType.LineItem;

    public long DealId { get; set; }

    public long? ProductId { get; set; }

    /// <summary>
    /// Name of the product
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// Description of the product
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Unique code for the product
    /// </summary>
    public string ProductCode { get; set; }

    /// <summary>
    /// Type of rate applied to the product
    /// </summary>
    public RateTypeEnum? RateType { get; set; }

    /// <summary>
    /// Price per unit of the product
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Cost per unit of the product
    /// </summary>
    public decimal? UnitCost { get; set; }

    /// <summary>
    /// Unit of Measure for the product
    /// </summary>
    public UomEnum? UnitOfMeasurement { get; set; }

    /// <summary>
    /// The source identifier
    /// </summary>
    public string SourceId { get; set; }

    /// <summary>
    /// Frequency of billing for the product
    /// </summary>
    public BillingFrequencyEnum? BillingFrequency { get; set; }

    /// <summary>
    /// Type of discount applied to the unit
    /// </summary>
    public UnitDiscountEnum? UnitDiscountType { get; set; }

    /// <summary>
    /// Value of the discount applied to the unit
    /// </summary>
    public decimal? UnitDiscountValue { get; set; }

    /// <summary>
    /// Annual price increase is pending
    /// </summary>
    public decimal? AnnualPriceIncrease { get; set; }

    /// <summary>
    /// Count
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Unit Discount
    /// </summary>
    public decimal? UnitDiscount { get; set; }

    /// <summary>
    /// NetPrice
    /// </summary>
    public decimal NetPrice
    {
        get
        {
            decimal totalPrice = RateType == RateTypeEnum.FlatRate ? Price : Price * Quantity;
            decimal discount;
            if (UnitDiscountType == UnitDiscountEnum.ByPercentage)
            {
                discount = totalPrice * ((UnitDiscount ?? 0) / 100);
            }
            else
            {
                discount = (UnitDiscount == null ? 0 : Convert.ToDecimal(UnitDiscount)) * Quantity;
            }
            return totalPrice - discount;
        }
    }

    public int? InvoiceMonth { get; set; }

    public int? InvoiceDate { get; set; }

    public string CreateBy { get; set; }

    public DateTimeOffset CreateDate { get; set; }

    public string ModifyBy { get; set; }

    public DateTimeOffset ModifyDate { get; set; }

    public static LineItemDto LoadFromJson(string json)
    {
        var lineItemDto = JsonConvert.DeserializeObject<LineItemDto>(json);
        var jObj = JObject.Parse(json);

        JToken location = null;
        if (jObj[FieldName.LocationId] != null)
            location = jObj[FieldName.LocationId];
        else if (jObj["LocationId"] != null)
            location = jObj["LocationId"];

        if (location != null)
        {
            var ids = new List<string>();
            foreach (var id in location)
            {
                ids.Add(id.ToString());
            }

            lineItemDto[FieldName.LocationId] = ids;
        }

        return lineItemDto;
    }

    public override string ToString()
    {
        var jObj = JObject.FromObject(this);

        foreach (var fieldName in GetFieldNameList())
        {
            var value = this[fieldName];

            jObj[fieldName] = value is IList ? JArray.FromObject(value) : JObject.FromObject(value);
        }

        return jObj.ToString();
    }
}
