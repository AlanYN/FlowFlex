using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.AdobeSign
{
    /// <summary>
    /// Input DTO for the Send Reminder endpoint.
    /// Moved from AdobeSignController.cs (PR-234 review item #5).
    /// </summary>
    public class SendReminderInputDto
    {
        [Required]
        public List<string> SignerEmails { get; set; } = new();
    }
}
