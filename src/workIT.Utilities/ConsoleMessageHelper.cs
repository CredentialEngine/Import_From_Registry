using System.Collections.Generic;
using System.Web;

namespace workIT.Utilities
{
    public class ConsoleMessageHelper
	{
		public static void SetConsoleInfoMessage( string friendlyMessage, string technicalMessage = "", bool overwriteExisting = false )
		{
			SetConsoleMessage( friendlyMessage, technicalMessage, overwriteExisting, "info" );
		}
		//

		public static void SetConsoleSuccessMessage( string friendlyMessage, string technicalMessage = "", bool overwriteExisting = false)
		{
			SetConsoleMessage( friendlyMessage, technicalMessage, overwriteExisting, "success" );
		}
		//

		public static void SetConsoleErrorMessage( string friendlyMessage, string technicalMessage = "", bool overwriteExisting = false )
		{
			SetConsoleMessage( friendlyMessage, technicalMessage, overwriteExisting, "error" );
		}
		//

		public static void SetConsoleCustomMessage( string friendlyMessage, string technicalMessage, bool overwriteExisting, string messageType )
		{
			SetConsoleMessage( friendlyMessage, technicalMessage, overwriteExisting, messageType );
		}
		//

		private static void SetConsoleMessage( string friendlyMessage, string technicalMessage, bool overwriteExisting, string messageType )
		{
			var message = BuildConsoleMessage( friendlyMessage, technicalMessage, messageType );

			if ( overwriteExisting )
			{
				HttpContext.Current.Items[ "ConsoleMessage" ] = new List<ConsoleMessage>() { message };
			}
			else
			{
				var current = HttpContext.Current.Items[ "ConsoleMessage" ] as List<ConsoleMessage>;
				if ( current != null )
				{
					current.Add( message );
					HttpContext.Current.Items[ "ConsoleMessage" ] = current;
				}
				else
				{
					SetConsoleMessage( friendlyMessage, technicalMessage, true, messageType );
				}
			}
		}
		//

		public static ConsoleMessage BuildConsoleMessage( string friendlyMessage, string technicalMessage, string messageType )
		{
			return new ConsoleMessage()
			{
				FriendlyMessage = friendlyMessage,
				TechnicalMessage = technicalMessage,
				MessageType = messageType
			};
		}
		//

		public class ConsoleMessage
		{
			public int Id { get; set; } //Used client-side. Do not populate on the server.
			/// <summary>
			/// Optional title
			/// </summary>
			public string Title { get; set; }
			public string FriendlyMessage { get; set; }
			public string TechnicalMessage { get; set; }
			public string MessageType { get; set; }
		}
		//
	}
}
