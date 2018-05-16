using System;

using System.Configuration;

namespace workIT.Utilities
{
    public class ConfigHelper
	{
		/// <summary>
		/// Get a configuration value - return default value on null or error
		/// </summary>
		/// <param name="target"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static string GetConfigValue( string target, string defaultValue )
		{
			try
			{
				var value = ConfigurationManager.AppSettings[ target ];
				if ( string.IsNullOrWhiteSpace( value ) )
				{
					throw new Exception();
				}

				return value;
			}
			catch
			{
				return defaultValue;
			}
		}
		//

		/// <summary>
		/// Get a configuration value from Keys.config - return default value on null or error
		/// </summary>
		/// <param name="target"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static string GetApiKey( string target, string defaultValue )
		{
			try
			{
				var path = AppDomain.CurrentDomain.BaseDirectory + "Keys.config";
				var fileMap = new ExeConfigurationFileMap() { ExeConfigFilename = path };
				var value = ConfigurationManager.OpenMappedExeConfiguration( fileMap, ConfigurationUserLevel.None ).AppSettings.Settings[ target ].Value;
				if ( string.IsNullOrWhiteSpace( value ) )
				{
					throw new Exception();
				}

				return value;
			}
			catch
			{
				return defaultValue;
			}
		}
		//

	}
}