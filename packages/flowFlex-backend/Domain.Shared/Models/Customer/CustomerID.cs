using System.Diagnostics.CodeAnalysis;

namespace FlowFlex.Domain.Shared.Models.Customer;

public readonly struct CustomerID
{
    private readonly long _customerId;
    private readonly string _customerCode;

    public CustomerID()
    {

    }

    public CustomerID(long customerId)
    {
        _customerId = customerId;
    }

    public CustomerID(string customerCode)
    {
        _customerCode = customerCode;
    }

    public CustomerID(long customerId, string customerCode)
    {
        _customerId = customerId;
        _customerCode = customerCode;
    }

    public readonly long CustomerId => _customerId;

    public readonly string CustomerCode => _customerCode ?? string.Empty;

    public override bool Equals([NotNullWhen(true)] object obj)
    {
        if (obj is CustomerID target)
        {
            return CustomerId == target.CustomerId && CustomerCode == target.CustomerCode;
        }

        return false;
    }

    public static implicit operator CustomerID(string customerCode)
    {
        return new CustomerID(customerCode);
    }

    public static implicit operator CustomerID(long customerId)
    {
        return new CustomerID(customerId);
    }

    public static implicit operator long(CustomerID customerID)
    {
        return customerID.CustomerId;
    }

    public static implicit operator string(CustomerID customerID)
    {
        return customerID.CustomerCode;
    }

    public static bool operator ==(CustomerID left, CustomerID right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CustomerID left, CustomerID right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        return (CustomerId + CustomerCode).GetHashCode();
    }
}
