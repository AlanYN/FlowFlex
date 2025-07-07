using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Shared.Enums.Unis;
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models.Customer
{
    public class CubeworkCustomerImportModel
    {
        //[DisplayName("T1_Field_Name")]
        //[StringLength(200, ErrorMessage = "The length of T1_Field_Name cannot exceed 200 characters.")]
        //public string T1_Field_Name { get; set; }

        [DisplayName("Category")]
        [StringLength(200, ErrorMessage = "The length of T2_Category cannot exceed 200 characters.")]
        public string T2_Category { get; set; }

        [DisplayName("Customer_Type")]
        [StringLength(200, ErrorMessage = "The length of T3_Customer_Type cannot exceed 200 characters.")]
        [AllowedValuesEnum(typeof(CustomerTypeEnum))]
        public string T3_Customer_Type { get; set; }

        [DisplayName("Customer_Code")]
        [StringLength(50, ErrorMessage = "The length of T4_Customer_Code cannot exceed 200 characters.")]
        public string T4_Customer_Code { get; set; }

        [DisplayName("Customer_Name")]
        [Required(ErrorMessage = "Please enter the customer name.")]
        [StringLength(200, ErrorMessage = "The length of T5_Customer_Name cannot exceed 200 characters.")]
        public string T5_Customer_Name { get; set; }

        [DisplayName("Print_Name")]
        [StringLength(200, ErrorMessage = "The length of T6_Print_Name cannot exceed 200 characters.")]
        public string T6_Print_Name { get; set; }

        //[DisplayName("T7_Group")]
        //[StringLength(200, ErrorMessage = "The length of T7_Group cannot exceed 200 characters.")]
        //public string T7_Group { get; set; }

        //[DisplayName("T8_Sub_Customer_of")]
        //public long? T8_Sub_Customer_of { get; set; } // Changed to long?

        //[DisplayName("T9_Billto__only")]
        //public bool? T9_Billto__only { get; set; } // Changed to bool?
        [DisplayName("Tags")]
        [AllowedValuesEnum(typeof(CustomerTagEnum))]
        public string T10_Tags { get; set; }

        //[DisplayName("T11_Logo")]
        //public long? T11_Logo { get; set; } // Changed to long?

        //[DisplayName("T12_Logo_Zebra_Code")]
        //public string T12_Logo_Zebra_Code { get; set; }

        [DisplayName("Status")]
        [Description("Customer status: Draft/Active/Inactive/OnHold")]
        [AllowedValuesEnum(typeof(CustomerStatusEnum))]
        public string T13_Status { get; set; }

        //[DisplayName("T14_Email")]
        //[EmailAddress(ErrorMessage = "The email format is incorrect.")]
        //[StringLength(100, ErrorMessage = "The length of T14_Email cannot exceed 100 characters.")]
        //public string T14_Email { get; set; }

        //[DisplayName("T15_Phone")]
        //[StringLength(50, ErrorMessage = "The length of T15_Phone cannot exceed 50 characters.")]
        //public string T15_Phone { get; set; }

        //[DisplayName("T16_Website")]
        //[StringLength(200, ErrorMessage = "The length of T16_Website cannot exceed 200 characters.")]
        //public string T16_Website { get; set; }

        //[DisplayName("T17_Dun_Bradstreet")]
        //[StringLength(50, ErrorMessage = "The length of T17_Dun_Bradstreet cannot exceed 50 characters.")]
        //public string T17_Dun_Bradstreet { get; set; }

        //[DisplayName("T18_Tax_ID")]
        //[StringLength(20, ErrorMessage = "The length of T18_Tax_ID cannot exceed 20 characters.")]
        //public string T18_Tax_ID { get; set; }

        //[DisplayName("T19_Represent_Company")]
        //public string T19_Represent_Company { get; set; }

        //[DisplayName("T20_Type_of_Business")]
        //public string T20_Type_of_Business { get; set; } // Changed to int?

        //[DisplayName("T21_Line_of_Business")]
        //public string T21_Line_of_Business { get; set; } // Changed to int?

        //[DisplayName("T22_Annual_Spending")]
        //public decimal? T22_Annual_Spending { get; set; } // Changed to decimal?

        //[DisplayName("T23_Monthly_Credit_Required")]
        //[StringLength(50, ErrorMessage = "The length of T23_Monthly_Credit_Required cannot exceed 50 characters.")]
        //public string T23_Monthly_Credit_Required { get; set; }

        //[DisplayName("T24_Number_of_Employees")]
        //public int? T24_Number_of_Employees { get; set; } // Changed to int?

        //[DisplayName("T25_Date_Established")]
        //public DateTime? T25_Date_Established { get; set; } // Changed to DateTime?

        //[DisplayName("T26_Parent_Company")]
        //public string T26_Parent_Company { get; set; }

        //[DisplayName("T27_MC")]
        //public string T27_MC { get; set; }
        [DisplayName("Corporate_Address")]
        [StringLength(200, ErrorMessage = "The length of Corporate Address cannot exceed 200 characters.")]
        public string T28_Corporate_Address { get; set; }

        [DisplayName("Country")]
        [AllowedValuesEnum(typeof(CountryEnum))]
        [StringLength(200, ErrorMessage = "The length of Country cannot exceed 200 characters.")]
        public string T29_Country { get; set; }

        [DisplayName("City")]
        [StringLength(200, ErrorMessage = "The length of City cannot exceed 200 characters.")]
        public string T30_City { get; set; }

        [DisplayName("State")]
        [StringLength(200, ErrorMessage = "The length of State cannot exceed 200 characters.")]
        public string T31_State { get; set; }

        [DisplayName("Zip_Code")]
        [StringLength(50, ErrorMessage = "The length of Zip Code cannot exceed 50 characters.")]
        public string T32_Zip_Code { get; set; }

        //[DisplayName("T33_Registered_Address")]
        //[StringLength(200, ErrorMessage = "The length of Registered Address cannot exceed 200 characters.")]
        //public string T33_Registered_Address { get; set; }

        //[DisplayName("T34_Country")]
        //[StringLength(200, ErrorMessage = "The length of Country cannot exceed 200 characters.")]
        //public string T34_Country { get; set; }

        //[DisplayName("T35_City")]
        //[StringLength(200, ErrorMessage = "The length of City cannot exceed 200 characters.")]
        //public string T35_City { get; set; }

        //[DisplayName("T36_State")]
        //[StringLength(200, ErrorMessage = "The length of State cannot exceed 200 characters.")]
        //public string T36_State { get; set; }

        //[DisplayName("T37_Zip_Code")]
        //[StringLength(50, ErrorMessage = "The length of Zip Code cannot exceed 50 characters.")]
        //public string T37_Zip_Code { get; set; }

        //[DisplayName("T38_Notes")]
        //[StringLength(200, ErrorMessage = "The length of Notes cannot exceed 200 characters.")]
        //public string T38_Notes { get; set; }
        //[DisplayName("T39_Contract_Name")]
        //[StringLength(100, ErrorMessage = "合同名称不能超过100个字符")]
        //public string T39_Contract_Name { get; set; }

        //[DisplayName("T40_Effective_Date")]
        //public DateTimeOffset T40_Effective_Date { get; set; }

        //[DisplayName("T41_Contract_Terms")]
        //[StringLength(50, ErrorMessage = "合同条款不能超过50个字符")]
        //public string T41_Contract_Terms { get; set; }

        //[DisplayName("T42_Shrinkage_Allowance")]
        //[StringLength(50, ErrorMessage = "收缩容差不能超过50个字符")]
        //public string T42_Shrinkage_Allowance { get; set; }
        //[DisplayName("T43_Status")]
        //[StringLength(50, ErrorMessage = "The length of Status cannot exceed 50 characters.")]
        //public string T43_Status { get; set; }

        //[DisplayName("T44_Type")]
        //[StringLength(50, ErrorMessage = "The length of Type cannot exceed 50 characters.")]
        //public string T44_Type { get; set; }

        //[DisplayName("T45_Name")]
        //[StringLength(100, ErrorMessage = "The length of Name cannot exceed 100 characters.")]
        //public string T45_Name { get; set; }

        //[DisplayName("T46_Phone_Number")]
        //[StringLength(50, ErrorMessage = "The length of Phone Number cannot exceed 50 characters.")]
        //public string T46_Phone_Number { get; set; }

        //[DisplayName("T47_Email")]
        //[EmailAddress(ErrorMessage = "The email format is incorrect.")]
        //[StringLength(100, ErrorMessage = "The length of Email cannot exceed 100 characters.")]
        //public string T47_Email { get; set; }

        //[DisplayName("T48_Fax")]
        //[StringLength(50, ErrorMessage = "The length of Fax cannot exceed 50 characters.")]
        //public string T48_Fax { get; set; }

        //[DisplayName("T49_Street")]
        //[StringLength(200, ErrorMessage = "The length of Street cannot exceed 200 characters.")]
        //public string T49_Street { get; set; }

        //[DisplayName("T50_Unit")]
        //[StringLength(50, ErrorMessage = "The length of Unit cannot exceed 50 characters.")]
        //public string T50_Unit { get; set; }

        //[DisplayName("T51_City")]
        //[StringLength(200, ErrorMessage = "The length of City cannot exceed 200 characters.")]
        //public string T51_City { get; set; }

        //[DisplayName("T52_State")]
        //[StringLength(200, ErrorMessage = "The length of State cannot exceed 200 characters.")]
        //public string T52_State { get; set; }

        //[DisplayName("T53_Zip_Code")]
        //[StringLength(50, ErrorMessage = "The length of Zip Code cannot exceed 50 characters.")]
        //public string T53_Zip_Code { get; set; }

        //[DisplayName("T54_Country")]
        //[StringLength(200, ErrorMessage = "The length of Country cannot exceed 200 characters.")]
        //public string T54_Country { get; set; }

        //[DisplayName("T55_Notes")]
        //[StringLength(200, ErrorMessage = "The length of Notes cannot exceed 200 characters.")]
        //public string T55_Notes { get; set; }

        [DisplayName("Contact_Status")]
        [AllowedValuesEnum(typeof(CustomerContactStatusEnum))]
        public string T56_Status { get; set; }

        [DisplayName("Contact_Type")]
        [AllowedValuesEnum(typeof(CustomerContactTypeEnum))]
        [StringLength(1000, ErrorMessage = "The length of Type cannot exceed 1000 characters.")]
        public string T57_Type { get; set; }

        [DisplayName("Contact_Name")]
        [StringLength(500, ErrorMessage = "The length of Name cannot exceed 500 characters.")]
        public string T58_Name { get; set; }

        [DisplayName("Contact_Email")]
        [EmailAddress(ErrorMessage = "The email format is incorrect.")]
        [StringLength(100, ErrorMessage = "The length of Email cannot exceed 100 characters.")]
        public string T59_Email { get; set; }

        //[DisplayName("Contact_Title")]
        //[StringLength(500, ErrorMessage = "The length of Title cannot exceed 500 characters.")]
        //public string T60_Title { get; set; }

        //[DisplayName("T61_Facility")]
        //[StringLength(500, ErrorMessage = "The length of Facility cannot exceed 500 characters.")]
        //public string T61_Facility { get; set; }

        //[DisplayName("T62_Work_Phone_Number")]
        //[StringLength(50, ErrorMessage = "The length of Work Phone Number cannot exceed 50 characters.")]
        //public string T62_Work_Phone_Number { get; set; }

        //[DisplayName("T63_Cell_Phone_Number")]
        //[StringLength(50, ErrorMessage = "The length of Cell Phone Number cannot exceed 50 characters.")]
        //public string T63_Cell_Phone_Number { get; set; }

        //[DisplayName("T64_Fax")]
        //[StringLength(500, ErrorMessage = "The length of Fax cannot exceed 500 characters.")]
        //public string T64_Fax { get; set; }

        //[DisplayName("T65_Note")]
        //[StringLength(500, ErrorMessage = "The length of Note cannot exceed 500 characters.")]
        //public string T65_Note { get; set; }

        //[DisplayName("T66_Facility")]
        //[StringLength(100, ErrorMessage = "Facility name cannot exceed 100 characters")]
        //public string T66_Facility { get; set; }

        //[DisplayName("T67_Category")]
        //public string T67_Category { get; set; }

        //[DisplayName("T68_Assignee")]
        //[StringLength(50, ErrorMessage = "Assignee name cannot exceed 50 characters")]
        //public string T68_Assignee { get; set; }

        //[DisplayName("T69_Email")]
        //[StringLength(100, ErrorMessage = "Email address cannot exceed 100 characters")]
        //[EmailAddress(ErrorMessage = "The email format is incorrect.")]
        //public string T69_Email { get; set; }

        //[DisplayName("T70_Phone_Number")]
        //[StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters")]
        //public string T70_Phone_Number { get; set; }

        //[DisplayName("T71_Note")]
        //[StringLength(500, ErrorMessage = "Note cannot exceed 500 characters")]
        //public string T71_Note { get; set; }

        //[DisplayName("T72_Active")]
        //public bool T72_Active { get; set; }

        //[DisplayName("T73_Default_Invoice_Format")]
        //[StringLength(1000, ErrorMessage = "The default invoice format cannot exceed 1000 characters.")]
        //public string T73_Default_Invoice_Format { get; set; }

        //[DisplayName("T74_Invoice_Attachment_File_Category")]
        //[StringLength(1000, ErrorMessage = "The invoice attachment file category cannot exceed 1000 characters.")]
        //public string T74_Invoice_Attachment_File_Category { get; set; }

        //[DisplayName("T75_Sending_Option")]
        //public string T75_Sending_Option { get; set; }

        //[DisplayName("T76_Sending_Frequencty")]
        //public string T76_Sending_Frequencty { get; set; }

        //[DisplayName("T77_Invoice_Notes_in_Remittance_Info")]
        //public bool T77_Invoice_Notes_in_Remittance_Info { get; set; }

        //[DisplayName("T78_Invoice_Notes")]
        //public string T78_Invoice_Notes { get; set; }

        //[DisplayName("T79_Billing_Code_Prefix")]
        //[StringLength(1000, ErrorMessage = "The billing code prefix cannot exceed 1000 characters.")]
        //public string T79_Billing_Code_Prefix { get; set; }

        //[DisplayName("T80_Allow_Increments_of_15_min")]
        //public bool T80_Allow_Increments_of_15_min { get; set; }

        //[DisplayName("T81_SendEDI")]
        //public bool T81_SendEDI { get; set; }

        //[DisplayName("T82_Current_Balance")]
        //public decimal T82_Current_Balance { get; set; }

        //[DisplayName("T83_Credit_Limit")]
        //public decimal T83_Credit_Limit { get; set; }

        //[DisplayName("T84_Grace_Period_Expiration_Date")]
        //public DateTimeOffset T84_Grace_Period_Expiration_Date { get; set; }

        //[DisplayName("T85_Credit_Hold")]
        //public bool T85_Credit_Hold { get; set; }

        //[DisplayName("T86_Default_AR_Account")]
        //public string T86_Default_AR_Account { get; set; }

        [DisplayName("Net_Term")]
        public string T87_Net_Term { get; set; }

        //[DisplayName("T88_Currency_Code")]
        //public string T88_Currency_Code { get; set; }

        //[DisplayName("T89_Class")]
        //public string T89_Class { get; set; }

        //[DisplayName("T90_Revenue_Code")]
        //public string T90_Revenue_Code { get; set; }

        //[DisplayName("T91_Paper_work_excempt")]
        //public bool T91_Paper_work_excempt { get; set; }

        //[DisplayName("T92_Insurance_Required")]
        //public bool T92_Insurance_Required { get; set; }

        //[DisplayName("T93_Insurance_Details")]
        //[StringLength(200, ErrorMessage = "Insurance details cannot exceed 200 characters")]
        //public string T93_Insurance_Details { get; set; }

        //[DisplayName("T94_Credit_Score_Ranking")]
        //[StringLength(200)]
        //public string T94_Credit_Score_Ranking { get; set; }

        //[DisplayName("T95_Credit_Score")]
        //[StringLength(200)]
        //public string T95_Credit_Score { get; set; }

        //[DisplayName("T96_Experian")]
        //[StringLength(200)]
        //public string T96_Experian { get; set; }

        //[DisplayName("T97_Commercial_Score")]
        //[StringLength(200)]
        //public string T97_Commercial_Score { get; set; }

        //[DisplayName("T98_FSR_Score")]
        //[StringLength(200)]
        //public string T98_FSR_Score { get; set; }

        //[DisplayName("T99_Collection_Score")]
        //[StringLength(200)]
        //public string T99_Collection_Score { get; set; }

        //[DisplayName("T100_Last_Operate_Date")]
        //public DateTimeOffset T100_Last_Operate_Date { get; set; }

        //[DisplayName("T101_Credit_Safe")]
        //[StringLength(200)]
        //public string T101_Credit_Safe { get; set; }

        //[DisplayName("T102_Credit_Score")]
        //[StringLength(200)]
        //public string T102_Credit_Score { get; set; }

        //[DisplayName("T103_Rating")]
        //[StringLength(200)]
        //public string T103_Rating { get; set; }

        //[DisplayName("T104_Description")]
        //[StringLength(200)]
        //public string T104_Description { get; set; }

        //[DisplayName("T105_Past_Credit_Score")]
        //[StringLength(200)]
        //public string T105_Past_Credit_Score { get; set; }

        //[DisplayName("T106_Past_Rating")]
        //[StringLength(200)]
        //public string T106_Past_Rating { get; set; }

        //[DisplayName("T107_Description")]
        //[StringLength(200)]
        //public string T107_Description { get; set; }

        //[DisplayName("T108_Current_Limit")]
        //public decimal T108_Current_Limit { get; set; }

        //[DisplayName("T109_Date_Approved")]
        //public DateTimeOffset T109_Date_Approved { get; set; }

        //[DisplayName("T110_Grace_Period_Days")]
        //[StringLength(200)]
        //public string T110_Grace_Period_Days { get; set; }

        //[DisplayName("T111_Avg_Aging_Days")]
        //[StringLength(200)]
        //public string T111_Avg_Aging_Days { get; set; }

        //[DisplayName("T112_Avg_Monthly_Invoice_Total")]
        //public decimal T112_Avg_Monthly_Invoice_Total { get; set; }

        //[DisplayName("T113_Current_Payterm")]
        //[StringLength(200)]
        //public string T113_Current_Payterm { get; set; }

        //[DisplayName("T114_AccountActivate_Date")]
        //public DateTimeOffset T114_AccountActivate_Date { get; set; }

        //[DisplayName("T115_First_Invoice_Start_Date")]
        //public DateTimeOffset T115_First_Invoice_Start_Date { get; set; }

        //[DisplayName("T116_ITD_Invoice_Amount")]
        //public decimal T116_ITD_Invoice_Amount { get; set; }

        //[DisplayName("T117_ITD_Payment_Recieved")]
        //public string T117_ITD_Payment_Recieved { get; set; }

        //[DisplayName("T118_Card_Number")]
        //[StringLength(256)]
        //public string T118_Card_Number { get; set; }

        //[DisplayName("T119_Card_Holder")]
        //[StringLength(50)]
        //public string T119_Card_Holder { get; set; }

        //[DisplayName("T120_CVV")]
        //[StringLength(256)]
        //public string T120_CVV { get; set; }

        //[DisplayName("T121_Expires")]
        //public DateTimeOffset T121_Expires { get; set; }

        //[DisplayName("T122_Address_1")]
        //[StringLength(200)]
        //public string T122_Address_1 { get; set; }

        //[DisplayName("T123_Address_2")]
        //[StringLength(200)]
        //public string T123_Address_2 { get; set; }

        //[DisplayName("T124_City")]
        //[StringLength(200)]
        //public string T124_City { get; set; }

        //[DisplayName("T125_State")]
        //[StringLength(200)]
        //public string T125_State { get; set; }

        //[DisplayName("T126_Zip")]
        //[StringLength(200)]
        //public string T126_Zip { get; set; }

        //[DisplayName("T127_Country")]
        //[StringLength(200)]
        //public string T127_Country { get; set; }

        //[DisplayName("T128_Account_Holder_Name")]
        //[StringLength(100)]
        //public string T128_Account_Holder_Name { get; set; }

        //[DisplayName("T129_Routing_Number")]
        //[StringLength(256)]
        //public string T129_Routing_Number { get; set; }

        //[DisplayName("T130_Account_Number")]
        //[StringLength(256)]
        //public string T130_Account_Number { get; set; }

        //[DisplayName("T131_Trade_Reference")]
        //public string T131_Trade_Reference { get; set; }

        //[DisplayName("T132_Bank_Related_Reference")]
        //public string T132_Bank_Related_Reference { get; set; }

        //[DisplayName("T133_Transportation_Reference_1")]
        //public string T133_Transportation_Reference_1 { get; set; }

        //[DisplayName("T134_Transportation_Reference_2")]
        //public string T134_Transportation_Reference_2 { get; set; }

        //[DisplayName("T135_Transportation_Reference_3")]
        //public string T135_Transportation_Reference_3 { get; set; }

        //[DisplayName("T136_Personal_Guarantee")]
        //public string T136_Personal_Guarantee { get; set; }

        //[DisplayName("T137_Inbound")]
        //public string T137_Inbound { get; set; }

        //[DisplayName("T138_Outbound")]
        //public string T138_Outbound { get; set; }

        //[DisplayName("T139_Stock_Rotation")]
        //public string T139_Stock_Rotation { get; set; }

        //[DisplayName("T140_Basic_Info")]
        //public string T140_Basic_Info { get; set; }

        //[DisplayName("Program Attribute_Program")]
        //public string T141_Program { get; set; }

        //[DisplayName("3rd_Party_Customer_Name")]
        //public string T142_3rd_Party_Customer_Name { get; set; }

        //[DisplayName("3rd_Party_Customer_Code")]
        //public string T143_3rd_Party_Customer_Code { get; set; }

        //[DisplayName("T144_Facility_Name")]
        //public string T144_Facility_Name { get; set; }

        //[DisplayName("T145_Facility_ID")]
        //public string T145_Facility_ID { get; set; }

        //[DisplayName("T146_Country")]
        //public string T146_Country { get; set; }

        //[DisplayName("T147_State_")]
        //public string T147_State_ { get; set; }

        //[DisplayName("T148_City")]
        //public string T148_City { get; set; }

        //[DisplayName("T149_Address")]
        //public string T149_Address { get; set; }

        //[DisplayName("T150_Bill_to")]
        //public string T150_Bill_to { get; set; }

        //[DisplayName("T151_Customer_Name")]
        //public string T151_Customer_Name { get; set; }

        //[DisplayName("T152_Customer_Code")]
        //public string T152_Customer_Code { get; set; }

        //[DisplayName("T153_Title_Name")]
        //public string T153_Title_Name { get; set; }

        //[DisplayName("T154_Title_Code")]
        //public string T154_Title_Code { get; set; }
    }
}
