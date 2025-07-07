using NPOI.Util;

using System.Collections.Generic;
using System.Linq;

namespace FlowFlex.Domain.Shared.Const
{
    public static class PermissionConsts
    {
        public static class CreditApplication
        {
            public const string Prefix = "creditapplication";

            public const string Approval = $"{Prefix}:approval";

            public const string Operation = $"{Prefix}:operation";

            public const string Delete = $"{Prefix}:delete";

            public const string Withdraw = $"{Prefix}:withdraw";

        }

        /// <summary>
        /// Permission setting module related codes, system reserved
        /// </summary>
        public static class PermissionSetting
        {
            public const string Prefix = "permission";

            public const string PermissionUrlCode = $"{Prefix}:permission";

            public static IEnumerable<string> Codes
            {
                get
                {
                    var codes = new List<string>
                    {
                        PermissionUrlCode
                    };
                    return codes.Select(c => c.ToUpper());
                }
                private set { }
            }
        }

        /// <summary>
        /// Address permission control
        /// </summary>
        public static class Addresses
        {
            public const string Prefix = "address";
            public const string Operation = $"{Prefix}:operation";
        }

        public static class OperationalAttr
        {
            public static class Basicinfo
            {
                public const string Prefix = "basicinfo";
                public const string Operation = $"{Prefix}:operation";
            }

            public static class Inbound
            {
                public const string Prefix = "inbound";
                public const string Operation = $"{Prefix}:operation";
            }

            public static class Outbound
            {
                public const string Prefix = "outbound";
                public const string Operation = $"{Prefix}:operation";
            }

            public static class StockRotation
            {
                public const string Prefix = "stockrotation";
                public const string Operation = $"{Prefix}:operation";
            }
        }

        /// <summary>
        /// Customer list approval
        /// </summary>
        public static class AccountApprove
        {
            public const string Prefix = "account";
            public const string Approval = $"{Prefix}:approval";
            public const string Delete = $"{Prefix}:delete";
        }

        /// <summary>
        /// Customer information
        /// </summary>
        public static class CustomerBasic
        {
            public const string Prefix = "basic";
            public const string Operation = $"{Prefix}:operation";
        }

        /// <summary>
        /// Customer details
        /// </summary>
        public static class CustomerDetails
        {
            public const string Prefix = "customerdetails";
            public const string Operation = $"{Prefix}:operation";
        }

        /// <summary>
        /// Customer contact
        /// </summary>
        public static class CustomerContact
        {
            public const string Prefix = "customercontact";
            public const string Operation = $"{Prefix}:operation";
        }

        /// <summary>
        /// Account holders
        /// </summary>
        public static class AccountHolders
        {
            public const string Prefix = "accountholders";
            public const string Operation = $"{Prefix}:operation";
        }

        /// <summary>
        /// Customer relationships
        /// </summary>
        public static class CustomerRelationships
        {
            /// <summary>
            /// Title
            /// </summary>
            public static class RelationshipsTitle
            {
                public const string Prefix = "relationshipstitle";
                public const string Operation = $"{Prefix}:operation";
            }

            /// <summary>
            /// Brand
            /// </summary>
            public static class RelationshipsBrand
            {
                public const string Prefix = "relationshipsbrand";
                public const string Operation = $"{Prefix}:operation";
            }

            /// <summary>
            /// Supplier
            /// </summary>
            public static class RelationshipsSupplier
            {
                public const string Prefix = "relationshipssupplier";
                public const string Operation = $"{Prefix}:operation";
            }

            /// <summary>
            /// Retailer
            /// </summary>
            public static class RelationshipsRetailer
            {
                public const string Prefix = "relationshipsretailer";
                public const string Operation = $"{Prefix}:operation";
            }
        }

        /// <summary>
        /// Billing
        /// </summary>
        public static class Billing
        {
            public const string Prefix = "billing";
            public const string Operation = $"{Prefix}:operation";
        }

        /// <summary>
        /// Financial
        /// </summary>
        public static class Financial
        {
            public const string Prefix = "financial";
            public const string Operation = $"{Prefix}:operation";
        }

        /// <summary>
        /// Program attribute
        /// </summary>
        public static class ProgramAttribute
        {
            public const string Prefix = "programattribute";
            public const string Operation = $"{Prefix}:operation";
        }

        /// <summary>
        /// Mapping
        /// </summary>
        public static class Mapping
        {
            public const string Prefix = "mapping";
            public const string Operation = $"{Prefix}:operation";
        }

        /// <summary>
        /// File
        /// </summary>
        public static class File
        {
            public const string Prefix = "file";
            public const string Operation = $"{Prefix}:operation";
        }

        /// <summary>
        /// Customer note
        /// </summary>
        public static class CustomerNote
        {
            public const string Prefix = "customernote";
            public const string Operation = $"{Prefix}:operation";
        }

        /// <summary>
        /// Bank payment details
        /// </summary>
        public static class BankPaymentDetails
        {
            public const string Prefix = "bankpaymentdetails";
            public const string Operation = $"{Prefix}:operation";
        }

        /// <summary>
        /// Reference materials
        /// </summary>
        public static class CustomerReference
        {
            /// <summary>
            /// Trade reference
            /// </summary>
            public static class TradeReference
            {
                public const string Prefix = "tradereference";
                public const string Operation = $"{Prefix}:operation";
            }

            /// <summary>
            /// Personal guarantee
            /// </summary>
            public static class PersonalGuarantee
            {
                public const string Prefix = "personalguarantee";
                public const string Operation = $"{Prefix}:operation";
            }
        }
    }
}
