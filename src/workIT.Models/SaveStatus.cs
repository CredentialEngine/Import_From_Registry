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
			Timezone = TimeZone.CurrentTimeZone;
		}

		public string ValidationGroup { get; set; }
		public string Community { get; set; }
		//public List<string> Communities { get; set; } = new List<string>();
		public string Ctid { get; set; }
		//should not need this, as we have MappingHelperV3.CurrentOwningAgentUid
		//but could be useful further down the line.
		public Guid CurrentDataProvider { get; set; }
		public bool UpdateElasticIndex { get; set; }
		 
		public string EnvelopeId { get; set; }
		/// <summary>
		/// Helper to send info back to a parent method
		/// </summary>
		public int EntityTypeId { get; set; }
		public string ResourceURL { get; set; }
		public Guid DocumentRowId { get; set; }
		public int DocumentId { get; set; }
        /// <summary>
        /// CTID of the publishing org
        /// </summary>
        public string DocumentPublishedBy { get; set; }
		/// <summary>
		/// CTID of the owned by org
		/// </summary>
		public string DocumentOwnedBy { get; set; }
		//
		public string DetailPageUrl { get; set; }
		
		//statistics for actions such as pathway components added/updated, etc.
		public int RecordsAdded { get; set; }
		public int RecordsFailed { get; set; }
		public int RecordsUpdated { get; set; }
		public TimeZone Timezone { get; set; }
		public DateTime EnvelopeCreatedDate { get; set; }
		public DateTime EnvelopeUpdatedDate { get; set; }
		public DateTime LocalCreatedDate { get; set; }
		public DateTime LocalUpdatedDate { get; set; }
		public void SetEnvelopeCreated( DateTime date)
		{
			EnvelopeCreatedDate = date;
			//TimeZone zone = TimeZone.CurrentTimeZone;
			// Demonstrate ToLocalTime and ToUniversalTime.
			LocalCreatedDate = Timezone.ToLocalTime( date );
		}
		public void SetEnvelopeUpdated(DateTime date)
		{
			EnvelopeUpdatedDate = date;
			//TimeZone zone = TimeZone.CurrentTimeZone;
			// Demonstrate ToLocalTime and ToUniversalTime.
			LocalUpdatedDate = Timezone.ToLocalTime( date );
		}
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
		public bool WasSectionValid
		{
			get { return !HasSectionErrors; }
		}
		public void AddError( string message )
		{
			Messages.Add( new Models.StatusMessage() { Message = message } );
			HasErrors = true;
			HasSectionErrors = true;
		}
		public void AddErrorRange( List<string> messages )
		{
			foreach ( string msg in messages )
				Messages.Add( new Models.StatusMessage() { Message = msg } );
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
