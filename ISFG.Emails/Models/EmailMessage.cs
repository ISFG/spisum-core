using System.Collections.Generic;
using MimeKit;

namespace ISFG.Emails.Models
{
	public class EmailMessage
	{
		#region Constructors

		public EmailMessage()
		{
			ToAddresses = new List<MailboxAddress>();
			FromAddresses = new List<MailboxAddress>();
		}

		#endregion

		#region Properties

		public List<MailboxAddress> ToAddresses { get; set; }

		//public List<EmailAddress> CopiesAddresses { get; set; }
		public List<MailboxAddress> FromAddresses { get; set; }
		public string Subject { get; set; }
		public string HtmlContent { get; set; }
		public List<Attachment> Attachments { get; set; } = new List<Attachment>();

		#endregion
	}
}
