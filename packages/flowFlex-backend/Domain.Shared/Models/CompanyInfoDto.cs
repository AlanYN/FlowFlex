using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Company information DTO
    /// </summary>
    public class CompanyInfoDto
    {
        /// <summary>
        /// Company name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// City
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Postal code
        /// </summary>
        public string ZipCode { get; set; }

        /// <summary>
        /// Street address
        /// </summary>
        public string Street { get; set; }

        /// <summary>
        /// Province
        /// </summary>
        public string Province { get; set; }
    }
}
