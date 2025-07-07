using Amazon.Auth.AccessControlPolicy;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FlowFlex.Domain.Shared.Enums.Item;
using FlowFlex.Domain.Shared.Enums.Unis;
using FlowFlex.Domain.Shared.Extensions;

namespace FlowFlex.Domain.Shared.Models.Customer;

public abstract class CustomerBasicModelBase
{
    public long CustomerId { get; set; }

    public abstract string CustomerName { get; set; }

    public string CustomerCode { get; set; }

    public string BNPAccountID { get; set; }

    public CustomerStatusEnum CustomerStatus
    {
        get => Tag[CustomerTagEnum.Customer];
        set => Tag[CustomerTagEnum.Customer] = value;
    }

    public SourceEnum CustomerSource { get; set; }

    public abstract CustomerTypeEnum CustomerType { get; }

    public long? ParentAccountId { get; set; }

    public long? SubCustomerOf { get; set; }

    public string Email { get; set; }

    public bool? BillToOnly { get; set; }

    public DateTimeOffset? CustomerFirstApprovedDate { get; set; }

    public TagModel Tag { get; set; } = new();

    public int? UsageScenario { get; set; }

    public string Currency { get; set; }

    public string Country { get; set; }

    public long? LeadsId { get; set; }
}

public class TagModel : IEnumerable<TagItem>
{
    private readonly Dictionary<CustomerTagEnum, CustomerStatusEnum> _dict;

    public TagModel()
    {
        _dict = [];
    }

    public CustomerStatusEnum this[CustomerTagEnum tag]
    {
        get
        {
            if (_dict.TryGetValue(tag, out CustomerStatusEnum value))
                return value;

            if (tag == CustomerTagEnum.Customer)
                return CustomerStatusEnum.Draft;

            return CustomerStatusEnum.None;
        }
        set
        {
            if (!_dict.TryAdd(tag, value))
                _dict[tag] = value;
        }
    }

    public bool ContainsTag(CustomerTagEnum tag) => _dict.ContainsKey(tag);

    public bool RemoveTag(CustomerTagEnum tag) => _dict.Remove(tag);

    public IEnumerator<TagItem> GetEnumerator()
    {
        foreach (var item in _dict)
        {
            yield return new TagItem { Tag = item.Key, Status = item.Value };
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static List<TagItem> ToTagList(TagModel model)
    {
        var result = new List<TagItem>();

        foreach (var (Tag, Status) in model)
        {
            if (Tag != CustomerTagEnum.Customer)
                result.Add(new TagItem { Tag = Tag, Status = Status });
        }

        return result;
    }

    public static implicit operator List<TagItem>(TagModel values)
    {
        return ToTagList(values);
    }

    public static implicit operator TagModel(List<TagItem> values)
    {
        return values;
    }

    public static implicit operator long(TagModel values)
    {
        return values.Select(x => (int)x.Tag).ToList().SetCombineTag();
    }

    public static implicit operator TagState(TagModel values)
    {
        var dest = new TagState();
        var props = typeof(TagState).GetProperties();
        foreach (var item in values)
        {
            var p = props.FirstOrDefault(x => item.Tag.ToString().Equals(x.Name));
            if (p != null)
                p.SetValue(dest, item.Status);
        }

        return dest;
    }

    public static TagModel Combind(List<int> tagList, TagState state)
    {
        var tags = tagList.SetCombineTag();
        return Combind(tags, state);
    }

    public static TagModel Combind(long tags, TagState state)
    {
        var tagModel = new TagModel();

        var tagList = tags.GetTags();
        foreach (var tag in tagList)
        {
            tagModel[(CustomerTagEnum)tag] = CustomerStatusEnum.None;
        }

        if (state != null)
        {
            var tagValues = Enum.GetValues<CustomerTagEnum>();
            var props = typeof(TagState).GetProperties();
            foreach (var prop in props)
            {
                var value = prop.GetValue(state);
                if (value is CustomerStatusEnum status)
                {
                    var tag = tagValues.FirstOrDefault(x => x.ToString().Equals(prop.Name));
                    if (status != CustomerStatusEnum.None && tagModel.ContainsTag(tag))
                    {
                        tagModel[tag] = status;
                    }
                }
            }
        }

        return tagModel;
    }

    public static TagModel Combind(long tags, TagState state, CustomerStatusEnum customerStatus)
    {
        var result = Combind(tags, state);

        if (result.ContainsTag(CustomerTagEnum.Customer))
            result[CustomerTagEnum.Customer] = customerStatus;

        return result;
    }

    public void Deconstruct(out long tags, out CustomerStatusEnum CustomerStatus, out TagState state)
    {
        tags = this.Select(x => (int)x.Tag).ToList().SetCombineTag();

        CustomerStatus = this[CustomerTagEnum.Customer];

        state = new TagState();
        var props = typeof(TagState).GetProperties();
        foreach (var item in ToTagList(this))
        {
            var prop = props.FirstOrDefault(x => x.Name == item.Tag.ToString());
            prop?.SetValue(state, item.Status);
        }
    }
}

public class TagItem
{
    public CustomerTagEnum Tag { get; set; }

    public CustomerStatusEnum Status { get; set; }

    public void Deconstruct(out CustomerTagEnum Tag, out CustomerStatusEnum Status)
    {
        Tag = this.Tag;
        Status = this.Status;
    }
}
