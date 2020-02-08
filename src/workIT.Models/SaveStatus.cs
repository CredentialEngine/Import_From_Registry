using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models
{
	public class SaveStatus
	{
		public SaveStatus()
		{
			Messages = new List<StatusMessage>();
			HasErrors = false;
			CodeValidationType = "warn";
			DetailPageUrl = "";
		}

		public string ValidationGroup { get; set; }
		public string Community { get; set; }
		public List<string> Communities { get; set; } = new List<string>();
		public string EnvelopeId { get; set; }
		public Guid DocumentRowId { get; set; }
		public int DocumentId { get; set; }
		public string DetailPageUrl { get; set; }
		public string Ctid { get; set; }

		/// <summary>
		/// CodeValidationType - actions for code validation
		/// rigid-concepts must match ctdl 
		/// warn - allow exceptions, return a warning message
		/// skip - no validation of concept scheme concepts
		/// </summary>
		public string CodeValidationType { get; set; }
        public bool DoingDownloadOnly { get; set; }
        public List<StatusMessage> Messages { get; set; }
		/// <summary>
		/// If true, error encountered somewhere during workflow
		/// </summary>
		public bool HasErrors { get; set; }
		/// <summary>
		/// Reset HasSectionErrors to false at the start of a new section of validation. Then check at th end of the section for any errors in the section
		/// </summary>
		public bool HasSectionErrors { get; set; }
		public void AddError( string message )
		{
			Messages.Add( new Models.StatusMessage() { Message = message } );
			HasErrors = true;
			HasSectionErrors = true;
		}
		public void AddWarning( string message )
		{
			Messages.Add( new Models.StatusMessage() { Message = message, IsWarning = true } );
		}
		public void AddWarningRange( List<string> messages )
		{
			foreach (string msg in messages)
				Messages.Add( new Models.StatusMessage() { Message = msg, IsWarning = true } );
		}
		public List<string> GetAllMessages()
		{
			List<string> messages = new List<string>();
			string prefix = "";
			foreach ( StatusMessage msg in Messages.OrderBy( m => m.IsWarning ) )
			{
                if (msg.IsWarning)
                    if (!msg.Message.ToLower().StartsWith( "warning" ))
                        prefix = "Warning - ";
                    else
                    if (!msg.Message.ToLower().StartsWith( "error" ))
                        prefix = "Error - ";
                messages.Add( prefix + msg.Message );
			}

			return messages;
        }

        public string GetErrorsAsString(string separator = "\r\n")
        {
            if (Messages.Count > 0)
                return string.Join(separator, GetAllMessages());
            else
                return "";
        }

		public void SetMessages( List<string> messages, bool isAllWarning  )
		{
			//just treat all as errors for now
			//string prefix = "";
			foreach ( string msg in messages )
			{
				if ( isAllWarning )
					AddWarning( msg );
				else
					AddError( msg );
			}
		}
	}

	public class StatusMessage
	{
		public string Message { get; set; }
		public bool IsWarning { get; set; }
	}
}
