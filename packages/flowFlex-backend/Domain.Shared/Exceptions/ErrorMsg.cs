namespace FlowFlex.Domain.Shared.Exceptions;

public static class ErrorMsg
{
    public static string CustomerDuplicateName
        => "A customer with the same name already exists. Please modify the company name then convert the leads.";

    public static string LineItemInvalid
        => "We have noticed that the information in the line item is incomplete. Please ensure that the following fields contain data \r\n [BILLING FREQUENCY, Contract start date and end date, Location, QUANTITY, PRICE, NAME]";

    public static string NotFound
        => "The resource does not exist";

    public static string BnpApiError
        => "bnp api error";

    public static string AlreadyConvertedCustomer
        => "this leads already converted customer";

    public static string CustomerStatusIsNotDraftNotAllowedToDelete
        => "Customer status is not allowed to be deleted if it is not a draft status";

    public static string ItemsWithDuplicateDamesAreNotAllowed
        => "Items with duplicate names are not allowed";

    public static string DoNotAllowDeletionOfSystemConfigurationItems
        => "Do not allow deletion of system configuration items";

    public static string Property_0HasBeenSealedPleaseDoNotCreatePropertyAgain =>
        "Property {0} has been sealed, please do not create property again";
}
