using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.SessionState;

using workIT.Utilities;
using MC = workIT.Models.Common;
using MD = workIT.Models.Detail;
using ME = workIT.Models.Elastic;
using MPM = workIT.Models.ProfileModels;

namespace workIT.Services
{
	public class ServiceHelper
	{
		public static int DefaultMiles = 25;
		static string DEFAULT_GUID = "00000000-0000-0000-0000-000000000000";
		//
		/// <summary>
		/// Session variable for message to display in the system console
		/// </summary>
		public const string SYSTEM_CONSOLE_MESSAGE = "SystemConsoleMessage";

		#region === Security related Methods ===

		/// <summary>
		/// Encrypt the text using MD5 crypto service
		/// This is used for one way encryption of a user password - it can't be decrypted
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static string Encrypt( string data )
		{
			byte[] byDataToHash = ( new UnicodeEncoding() ).GetBytes( data );
			byte[] bytHashValue = new MD5CryptoServiceProvider().ComputeHash( byDataToHash );
			return BitConverter.ToString( bytHashValue );
		}

		/// <summary>
		/// Encrypt the text using the provided password (key) and CBC CipherMode
		/// </summary>
		/// <param name="text"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public static string Encrypt_CBC( string text, string password )
		{
			RijndaelManaged rijndaelCipher = new RijndaelManaged();

			rijndaelCipher.Mode = CipherMode.CBC;
			rijndaelCipher.Padding = PaddingMode.PKCS7;
			rijndaelCipher.KeySize = 128;
			rijndaelCipher.BlockSize = 128;

			byte[] pwdBytes = System.Text.Encoding.UTF8.GetBytes( password );

			byte[] keyBytes = new byte[ 16 ];

			int len = pwdBytes.Length;

			if ( len > keyBytes.Length )
				len = keyBytes.Length;

			System.Array.Copy( pwdBytes, keyBytes, len );

			rijndaelCipher.Key = keyBytes;
			rijndaelCipher.IV = keyBytes;

			ICryptoTransform transform = rijndaelCipher.CreateEncryptor();

			byte[] plainText = Encoding.UTF8.GetBytes( text );

			byte[] cipherBytes = transform.TransformFinalBlock( plainText, 0, plainText.Length );

			return Convert.ToBase64String( cipherBytes );

		}

		/// <summary>
		/// Decrypt the text using the provided password (key) and CBC CipherMode
		/// </summary>
		/// <param name="text"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public static string Decrypt_CBC( string text, string password )
		{
			RijndaelManaged rijndaelCipher = new RijndaelManaged();

			rijndaelCipher.Mode = CipherMode.CBC;
			rijndaelCipher.Padding = PaddingMode.PKCS7;
			rijndaelCipher.KeySize = 128;
			rijndaelCipher.BlockSize = 128;

			byte[] encryptedData = Convert.FromBase64String( text );

			byte[] pwdBytes = System.Text.Encoding.UTF8.GetBytes( password );

			byte[] keyBytes = new byte[ 16 ];

			int len = pwdBytes.Length;

			if ( len > keyBytes.Length )
				len = keyBytes.Length;

			System.Array.Copy( pwdBytes, keyBytes, len );

			rijndaelCipher.Key = keyBytes;
			rijndaelCipher.IV = keyBytes;

			ICryptoTransform transform = rijndaelCipher.CreateDecryptor();

			byte[] plainText = transform.TransformFinalBlock( encryptedData, 0, encryptedData.Length );

			return Encoding.UTF8.GetString( plainText );

		}

		#endregion

		#region Mapping for Finder API
		public static void MapEntitySearchLink( int orgId, string orgName, int entityCount, string labelTemplate, string searchType, ref List<MD.LabelLink> output, string roles = "6,7" )
		{
			//var output = new MD.LabelLink();
			if ( orgId  < 1)
				return;
			//note need the friendly name
			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );
			//
			//search?autosearch=true&amp;searchType=credential&amp;custom={n:'organizationroles',aid:957,rid:[6,7],p:'Bates+Technical+College',r:'',d:'Owns/Offers 2 Credential(s)'}
			//var label = string.Format( "Owns/Offers {0} Credential(s)", entityCount );
			try
			{
				var label = string.Format( labelTemplate, entityCount );
				var custom = string.Format( "custom=(n:'organizationroles',aid:{0},rid:[{3}],p:'{1}',r:'',d:'{2}')", orgId, orgName, label, roles );
				var url = baseSiteURL + string.Format( "search?autosearch=true&searchType={0}&custom=((n:'organizationroles',aid:{1},rid:[{2}],p:'{3}',r:'',d:'{4}'))", searchType, orgId, roles, orgName, label );
				url = url.Replace( "((", "{" ).Replace( "))", "}" );
				url = url.Replace( "'", "%27" ).Replace( " ", "%20" );
				//url = HttpUtility.UrlEncode( url );

				output.Add( new MD.LabelLink()
				{
					Label = label,
					Count = entityCount,
					URL = url
				});
			} catch(Exception ex)
			{
				LoggingHelper.DoTrace( 1, "" + ex.Message );
			}
			//return output;

		}

		public static void  MapQAPerformedLink( int orgId, string orgName, int entityCount, string labelTemplate, string searchType, ref List<MD.LabelLink> output )
		{
			//var output = new MD.LabelLink();
			if ( orgId < 1 )
				return;
			//note need the friendly name
			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );
			//
			//search?autosearch=true&amp;searchType=credential&amp;custom={n:'organizationroles',aid:957,rid:[6,7],p:'Bates+Technical+College',r:'',d:'Owns/Offers 2 Credential(s)'}
			//var label = string.Format( "Owns/Offers {0} Credential(s)", entityCount );
			try
			{
				var label = string.Format( labelTemplate, entityCount );
				var url = baseSiteURL + string.Format( "search?autosearch=true&searchType={0}&custom=((n:'organizationroles',aid:{1},rid:[1,2,10,12],p:'{2}',r:'',d:'{3}'))", searchType, orgId, orgName, label );
				url = url.Replace( "((", "{" ).Replace( "))", "}" );
				url = url.Replace( "'", "%27" ).Replace( " ", "%20" );
				//url = HttpUtility.UrlEncode( url );

				output.Add( new MD.LabelLink()
				{
					Label = label,
					Count = entityCount,
					URL = url
				});
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, "" + ex.Message );
			}
			//return output;

		}

		public static List<MD.LabelLink> MapPropertyLabelLinks( MC.Enumeration input, string searchType, bool formatUrl = true )
		{
			var output = new List<MD.LabelLink>();
			if ( input == null || input.Items == null || input.Items.Count() == 0 )
				return null;
			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );
			//
			//https://sandbox.credentialengine.org/finder/search?autosearch=true&searchType=organization&filters=7-1185
			foreach ( var item in input.Items)
			{
				var value = new MD.LabelLink()
				{
					Label = item.Name,//confirm this will be consistant					
				};
				if ( formatUrl )
					value.URL = baseSiteURL + string.Format( "search?autosearch=true&searchType={0}&filters={1}-{2}", searchType, input.Id, item.Id );    //may be difficult to set generically?

				output.Add( value );
			}

			return output;

		}
		public static MD.LabelLink MapPropertyLabelLink( MC.Enumeration input, string searchType, bool formatUrl = true )
		{
			var output = new MD.LabelLink();
			if ( input == null || input.Items == null || input.Items.Count() == 0 )
				return null;
			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );
			//
			//https://sandbox.credentialengine.org/finder/search?autosearch=true&searchType=organization&filters=7-1185
			foreach ( var item in input.Items )
			{
				var value = new MD.LabelLink()
				{
					Label = item.Name,//confirm this will be consistant					
				};
				if ( formatUrl )
					value.URL = baseSiteURL + string.Format( "search?autosearch=true&searchType={0}&filters={1}-{2}", searchType, input.Id, item.Id );    //may be difficult to set generically?

				output = value ;
				break;
			}

			return output;

		}
		/// <summary>
		/// prototype for industry 
		/// Current detail page just does a keyword search.
		/// Gray button link does an actual industry type search
		/// </summary>
		/// <param name="input"></param>
		/// <param name="searchType"></param>
		/// <returns></returns>
		public static List<MD.LabelLink> MapReferenceFrameworkLabelLink( MC.Enumeration input, string searchType )
		{
			var output = new List<MD.LabelLink>();
			if ( input == null || input.Items == null || input.Items.Count() == 0 )
				return output;
			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );
			//
			//https://sandbox.credentialengine.org/finder/search?autosearch=true&searchType=organization&filters=7-1185
			foreach ( var item in input.Items )
			{
				var value = new MD.LabelLink()
				{
					Label = item.Name,//confirm this will be consistant
					//URL = baseSiteURL + string.Format( "search?autosearch=true&searchType={0}&filters={1}-{2}", searchType, input.Id, item.Id )

					URL = baseSiteURL + string.Format( "search?autosearch=true&searchType={0}&keywords={1}", searchType, item.Name )

				};
				output.Add( value );
			}

			return output;

		}
		public static List<MD.LabelLink> MapPropertyLabelLinks( List<MPM.TextValueProfile> input, string searchType )
		{
			var output = new List<MD.LabelLink>();
			if ( input == null || input.Count() == 0 )
				return output;
			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );
			//
			//search?autosearch=true&amp;searchType=organization&amp;keywords=Career and Technical Education
			foreach ( var item in input )
			{
				if ( !string.IsNullOrWhiteSpace( item.TextValue ) )
				{
					var value = new MD.LabelLink()
					{
						Label = item.TextValue,//confirm this will be consistant
						URL = baseSiteURL + string.Format( "search?autosearch=true&searchType={0}&keywords={1}", searchType, item.TextValue )   
					};
					output.Add( value );
				}
			}

			return output;

		}
		public static List<string> MapTextValueProfileToStringList( List<MPM.TextValueProfile> input )
		{
			var output = new List<string>();
			if ( input == null || input.Count() == 0 )
				return output;
			//
			//search?autosearch=true&amp;searchType=organization&amp;keywords=Career and Technical Education
			foreach ( var item in input )
			{
				if ( !string.IsNullOrWhiteSpace( item.TextValue ) )
				{
					output.Add( item.TextValue.Trim() );
				}
			}

			return output;

		}
		//

		public static List<ME.JurisdictionProfile> MapJurisdiction( List<MC.JurisdictionProfile> input, string assertionType = "")
		{
			var output = new List<ME.JurisdictionProfile>();
			if ( input == null || input.Count() == 0 )
				return null;
			//
			foreach ( var item in input )
			{
				var pp = new ME.JurisdictionProfile()
				{
					Description = item.Description,
					GlobalJurisdiction = item.GlobalJurisdiction,
				};
				//map address-need a helper to format the jurisdiction - rare
				//**** need to handle GeoCoordinates
				if ( item.MainJurisdiction != null  )
				{
					//check - likely the data is in 
					if ( item.MainJurisdiction.Address != null )
					{
						pp.MainJurisdiction = new ME.Address()
						{
							Name = item.MainJurisdiction.Address.Name,
							City = item.MainJurisdiction.Address.City,
							AddressRegion = item.MainJurisdiction.Address.AddressRegion,
							Country = item.MainJurisdiction.Address.Country,
							Latitude = item.MainJurisdiction.Address.Latitude,
							Longitude = item.MainJurisdiction.Address.Longitude
						};
					} else
					{
						pp.MainJurisdiction = new ME.Address()
						{
							Name = item.MainJurisdiction.Name,
							AddressRegion = item.MainJurisdiction.Region,
							Country = item.MainJurisdiction.Country,
							Latitude = item.MainJurisdiction.Latitude,
							Longitude = item.MainJurisdiction.Longitude
						};
					}
				}
				if ( item.JurisdictionException != null && item.JurisdictionException.Any() )
				{
					pp.JurisdictionException = new List<ME.Address>();
					foreach ( var je in item.JurisdictionException )
					{
						if ( je.Address != null )
						{
							var j = new ME.Address()
							{
								Name = je.Address.Name,
								City = je.Address.City,
								AddressRegion = je.Address.AddressRegion,
								Country = je.Address.Country,
								Latitude = je.Address.Latitude,
								Longitude = je.Address.Longitude
							};
							pp.JurisdictionException.Add( j );
						}
						else
						{
							var j = new ME.Address()
							{
								Name = je.Name,
								AddressRegion = je.Region,
								Country = je.Country,
								Latitude = je.Latitude,
								Longitude = je.Longitude
							};
							pp.JurisdictionException.Add( j );
						}
					}
				}
				//other
				//for AssertedIns
				pp.AssertedBy = null;
				if ( !string.IsNullOrWhiteSpace( assertionType ) )
				{
					pp.AssertedInType = item.AssertedInType;
					if ( item.AssertedByOrganization != null && !string.IsNullOrWhiteSpace( item.AssertedByOrganization.Name ) )
					{
						pp.AssertedBy = MapToEntityReference( item.AssertedByOrganization );
					}
				}

				//
				output.Add( pp );

			};

			return output;

		}

		//
		public static List<MD.ProcessProfile> MapProcessProfile( int orgId, List<MPM.ProcessProfile> input )
		{
			var output = new List<MD.ProcessProfile>();
			if ( input == null || input.Count() == 0 )
				return output;
			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );

			//
			foreach ( var item in input )
			{
				var pp = new MD.ProcessProfile()
				{
					//ProcessProfileType = item.ProcessType,
					Description = item.Description,
					DateEffective = item.DateEffective,
					ProcessFrequency = item.ProcessFrequency,
					ProcessMethod = item.ProcessMethod,
					ProcessMethodDescription = item.ProcessMethodDescription,
					ProcessStandards = item.ProcessStandards,
					ProcessStandardsDescription = item.ProcessStandardsDescription,
					ScoringMethodDescription = item.ScoringMethodDescription,
					ScoringMethodExample = item.ScoringMethodExample,
					ScoringMethodExampleDescription = item.ScoringMethodExampleDescription,
					SubjectWebpage = item.SubjectWebpage,
					VerificationMethodDescription = item.VerificationMethodDescription,
				};
				pp.ExternalInput = MapPropertyLabelLinks( item.ExternalInput, "organization", false );
				pp.ProcessingAgent = null;
				if ( item.ProcessingAgent != null && item.ProcessingAgent.Id > 0 && item.ProcessingAgent.Id != orgId )
				{
					if ( item.ProcessingAgent != null && !string.IsNullOrWhiteSpace( item.ProcessingAgent.Name ) )
						pp.ProcessingAgent = MapToEntityReference( item.ProcessingAgent );
				}
			
				if ( item.TargetAssessment != null && item.TargetAssessment.Any() )
				{
					foreach (var target in item.TargetAssessment)
					{
						if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
							pp.TargetAssessment.Add( MapToEntityReference( target ) );
					}
				}
				if ( item.TargetCredential != null && item.TargetCredential.Any() )
				{
					foreach ( var target in item.TargetCredential )
					{
						if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
							pp.TargetCredential.Add( MapToEntityReference( target ) );
					}
				}
				if ( item.TargetLearningOpportunity != null && item.TargetLearningOpportunity.Any() )
				{
					foreach ( var target in item.TargetLearningOpportunity )
					{
						if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
							pp.TargetLearningOpportunity.Add( MapToEntityReference( target ) );
					}
				}
				output.Add( pp );
			}

			return output;
		}
		public static List<MC.TopLevelEntityReference> MapToEntityReference( List<MC.TopLevelObject> input )
		{
			var output = new List<MC.TopLevelEntityReference>();
			if ( input == null || !input.Any() )
				return null;

			foreach (var item in input)
			{
				var tlo = MapToEntityReference( item );
				if ( tlo != null && !string.IsNullOrWhiteSpace( tlo.Name ) ) 
				{
					output.Add( tlo );
				}
			}
			if ( !output.Any() )
				return null;

			return output;
		}

		public static List<MC.TopLevelEntityReference> MapToEntityReference( List<MPM.OrganizationRoleProfile> input )
		{
			var output = new List<MC.TopLevelEntityReference>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				var tlo = MapToEntityReference( item );
				if ( tlo != null && !string.IsNullOrWhiteSpace( tlo.Name ) )
				{
					output.Add( tlo );
				}
			}
			if ( !output.Any() )
				return null;

			return output;
		}
		public static MC.TopLevelEntityReference MapToEntityReference( MPM.OrganizationRoleProfile input )
		{

			if ( input == null || input.ActingAgent == null || string.IsNullOrWhiteSpace( input.ActingAgent.Name ) )
				return null;

			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );

			var output = new MC.TopLevelEntityReference()
			{
				Id = input.Id,//need for links, or may need to create link here
				Name = input.ActingAgent.Name,
				SubjectWebpage = input.ActingAgent.SubjectWebpage,
				Description = input.ActingAgent.Description,
				CTID = input.ActingAgent.CTID, 
				EntityTypeId = 2,
			};
			if ( !string.IsNullOrWhiteSpace( output.CTID ) )
				output.DetailURL = baseSiteURL + "resources/" + output.CTID;

			return output;

		}

		public static MC.TopLevelEntityReference MapToEntityReference( MC.TopLevelObject input )
		{

			if ( input == null || string.IsNullOrWhiteSpace( input.Name ) )
				return null;// new MC.TopLevelEntityReference();	//or NULL

			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );

			var output = new MC.TopLevelEntityReference()
			{
				Id = input.Id,//need for links, or may need to create link here
				Name = input.Name,
				SubjectWebpage = input.SubjectWebpage,
				Description = input.Description,
				CTID = input.CTID,
				EntityTypeId = input.EntityTypeId,
			};
			if ( !string.IsNullOrWhiteSpace( output.CTID ) )
				output.DetailURL = baseSiteURL + "resources/" + output.CTID;

			return output;

		}

		//==========================================

		public static List<ME.CostManifest> MapToCostManifests( List<MC.CostManifest> input )
		{

			if ( input == null || !input.Any() )
			{
				return null;
			}
			var output = new List<ME.CostManifest>();
			foreach ( var item in input )
			{
				//just in case
				if ( string.IsNullOrWhiteSpace( item.CostDetails ) )
					continue;
				var cm = new ME.CostManifest()
				{
					Name = item.Name,
					Description = item.Description,
					CostDetails = item.CostDetails,
					StartDate = item.StartDate,
					EndDate = item.EndDate,
					CTID = item.CTID,
				};
				//CostProfiles
				if ( item.EstimatedCost != null && item.EstimatedCost.Any() )
				{
					cm.EstimatedCost = ServiceHelper.MapToCostProfiles( item.EstimatedCost );
				}

				output.Add( cm );
			}
			if ( !output.Any() )
				return null;

			return output;

		}
		public static List<ME.CostProfile> MapToCostProfiles( List<MPM.CostProfile> input )
		{
			var output = new List<ME.CostProfile>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				var tlo = MapToCostProfile( item );
				if ( tlo != null && !string.IsNullOrWhiteSpace( tlo.Description ) )
				{
					output.Add( tlo );
				}
			}
			if ( !output.Any() )
				return null;

			return output;
		}
		public static ME.CostProfile MapToCostProfile( MPM.CostProfile input )
		{

			if ( input == null || string.IsNullOrWhiteSpace( input.Description ) )
				return null;

			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );

			var output = new ME.CostProfile()
			{
				Name = input.ProfileName,
				CostDetails = input.CostDetails,
				Description = input.Description,
				Currency = input.Currency,
				CurrencySymbol = input.CurrencySymbol,
				StartDate = input.StartDate,
				EndDate = input.EndDate,
			};
			output.Condition = MapTextValueProfileTextValue( input.Condition );
			output.Jurisdiction = MapJurisdiction( input.Jurisdiction );
			//output.Region = MapJurisdiction( input.Region );
			//items

			if(input.Items  != null && input.Items.Any())
			{
				foreach(var item in input.Items )
				{
					var cpi = new ME.CostProfileItem() 
					{ 
						Price = item.Price,
						PaymentPattern = item.PaymentPattern,
						AudienceType = MapPropertyLabelLinks( item.AudienceType, "organization", false ),
						ResidencyType = MapPropertyLabelLinks( item.ResidencyType, "organization", false ),
					};
					cpi.DirectCostType = MapPropertyLabelLink( item.DirectCostType, "organization", false );

					output.CostItems.Add( cpi );
				}
			}

			return output;

		}

		public static List<ME.ConditionProfile> MapToConditionProfiles( List<MPM.ConditionProfile> input )
		{
			var output = new List<ME.ConditionProfile>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				var tlo = MapToConditionProfile( item );
				if ( tlo != null && !string.IsNullOrWhiteSpace( tlo.Description ) )
				{
					output.Add( tlo );
				}
			}
			if ( !output.Any() )
				return null;

			return output;
		}
		public static ME.ConditionProfile MapToConditionProfile( MPM.ConditionProfile input, string searchType = "" )
		{

			if ( input == null || string.IsNullOrWhiteSpace( input.Description ) )
				return null;

			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );

			var output = new ME.ConditionProfile()
			{
				Name = input.ProfileName,
				SubjectWebpage = input.SubjectWebpage,
				Description = input.Description,
				Experience = input.Experience,
				AudienceLevelType = MapPropertyLabelLinks( input.AudienceLevelType, searchType, false ),
				AudienceType = MapPropertyLabelLinks( input.AudienceType, searchType, false ),
				CreditUnitTypeDescription = input.CreditUnitTypeDescription,
				SubmissionOfDescription = input.SubmissionOfDescription,
				Weight = input.Weight,
				YearsOfExperience = input.YearsOfExperience
			};
			output.Condition = MapTextValueProfileTextValue( input.Condition );
			output.Jurisdiction = MapJurisdiction( input.Jurisdiction );
			output.ResidentOf = MapJurisdiction( input.ResidentOf );
			output.SubmissionOf = MapTextValueProfileTextValue( input.SubmissionOf );
			//CreditValue

			//CommonCosts
			output.CommonCosts = MapToCostManifests( input.CommonCosts );
			//EstimatedCosts
			output.EstimatedCost = MapToCostProfiles( input.EstimatedCost );

			//targets
			if ( input.TargetAssessment != null && input.TargetAssessment.Any() )
			{
				output.TargetAssessment = new List<MC.TopLevelEntityReference>();
				foreach ( var target in input.TargetAssessment )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						output.TargetAssessment.Add( MapToEntityReference( target ) );
				}
			}
			if ( input.TargetCredential != null && input.TargetCredential.Any() )
			{
				output.TargetCredential = new List<MC.TopLevelEntityReference>();
				foreach ( var target in input.TargetCredential )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						output.TargetCredential.Add( MapToEntityReference( target ) );
				}
			}
			if ( input.TargetLearningOpportunity != null && input.TargetLearningOpportunity.Any() )
			{
				output.TargetLearningOpportunity = new List<MC.TopLevelEntityReference>();
				foreach ( var target in input.TargetLearningOpportunity )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						output.TargetLearningOpportunity.Add( MapToEntityReference( target ) );
				}
			}
			return output;

		}
		public static List<string> MapTextValueProfileTextValue(List<MPM.TextValueProfile> input)
		{
			var output = new List<string>();
			if ( input == null || !input.Any() )
				return null;
			foreach(var item in input)
			{
				if ( !string.IsNullOrWhiteSpace( item.TextValue ) )
					output.Add( item.TextValue );
			}
			if ( !output.Any() )
				return null;

			return output;
		}
		#endregion
		#region Helpers and validaton
		public static bool IsLocalHost()
		{
			return IsTestEnv();
		}
		public static bool IsTestEnv()
		{
			string host = HttpContext.Current.Request.Url.Host.ToString();
			return ( host.Contains( "localhost" ) || host.Contains( "209.175.164.200" ) );
		}
		public static int StringToInt( string value, int defaultValue )
		{
			int returnValue = defaultValue;
			if ( Int32.TryParse( value, out returnValue ) == true )
				return returnValue;
			else
				return defaultValue;
		}


		public static bool StringToDate( string value, ref DateTime returnValue )
		{
			if ( System.DateTime.TryParse( value, out returnValue ) == true )
				return true;
			else
				return false;
		}

		/// <summary>
		/// IsInteger - test if passed string is an integer
		/// </summary>
		/// <param name="stringToTest"></param>
		/// <returns></returns>
		public static bool IsInteger( string stringToTest )
		{
			int newVal;
			bool result = false;
			try
			{
				newVal = Int32.Parse( stringToTest );

				// If we get here, then number is an integer
				result = true;
			}
			catch
			{

				result = false;
			}
			return result;

		}


		/// <summary>
		/// IsDate - test if passed string is a valid date
		/// </summary>
		/// <param name="stringToTest"></param>
		/// <returns></returns>
		public static bool IsDate( string stringToTest, bool doingReasonableCheck = true )
		{

			DateTime newDate;
			bool result = false;
			try
			{
				newDate = System.DateTime.Parse( stringToTest );
				result = true;
				//check if reasonable
				if ( doingReasonableCheck && newDate < new DateTime( 1900, 1, 1 ) )
					result = false;
			}
			catch
			{

				result = false;
			}
			return result;

		} //end
        public static bool IsValidCtid( string ctid, ref List<string> messages, bool isRequired = false, bool skippingErrorMessages = true )
        {
            bool isValid = true;

            if ( string.IsNullOrWhiteSpace( ctid ) )
            {
                if ( isRequired )
                {
                    messages.Add( "Error - A valid CTID property must be entered." );
                }
                return false;
            }

            ctid = ctid.ToLower();
            if ( ctid.Length != 39 )
            {
                if ( !skippingErrorMessages )
                    messages.Add( "Error - Invalid CTID format. The proper format is ce-UUID. ex. ce-84365AEA-57A5-4B5A-8C1C-EAE95D7A8C9B" );
                return false;
            }

            if ( !ctid.StartsWith( "ce-" ) )
            {
                if ( !skippingErrorMessages )
                    messages.Add( "Error - The CTID property must begin with ce-" );
                return false;
            }
            //now we have the proper length and format, the remainder must be a valid guid
            if ( !IsValidGuid( ctid.Substring( 3, 36 ) ) )
            {
                if ( !skippingErrorMessages )
                    messages.Add( "Error - Invalid CTID format. The proper format is ce-UUID. ex. ce-84365AEA-57A5-4B5A-8C1C-EAE95D7A8C9B" );
                return false;
            }

            return isValid;
        }

        public static bool IsValidGuid( Guid field )
		{
			if ( ( field == null || field == Guid.Empty ) )
				return false;
			else
				return true;
		}
		public static bool IsValidGuid( string field )
		{
			Guid guidOutput;
			if ( ( field == null || field.ToString() == DEFAULT_GUID ) )
				return false;
			else if ( !Guid.TryParse( field, out guidOutput ) )
				return false;
			else 
				return true;
		}
		/// <summary>
		/// Check if the passed dataset is indicated as one containing an error message (from a web service)
		/// </summary>
		/// <param name="wsDataset">DataSet for a web service method</param>
		/// <returns>True if dataset contains an error message, otherwise false</returns>
		public static bool HasErrorMessage( DataSet wsDataset )
		{

			if ( wsDataset.DataSetName == "ErrorMessage" )
				return true;
			else
				return false;

		} //

		/// <summary>
		/// Convert a comma-separated list (as a string) to a list of integers
		/// </summary>
		/// <param name="csl">A comma-separated list of integers</param>
		/// <returns>A List of integers. Returns an empty list on error.</returns>
		public static List<int> CommaSeparatedListToIntegerList( string csl )
		{
			try
			{
				return CommaSeparatedListToStringList( csl ).Select( int.Parse ).ToList();
			}
			catch
			{
				return new List<int>();
			}

		}

		/// <summary>
		/// Convert a comma-separated list (as a string) to a list of strings
		/// </summary>
		/// <param name="csl">A comma-separated list of strings</param>
		/// <returns>A List of strings. Returns an empty list on error.</returns>
		public static List<string> CommaSeparatedListToStringList( string csl )
		{
			try
			{
				return csl.Trim().Split( new string[] { "," }, StringSplitOptions.RemoveEmptyEntries ).ToList();
			}
			catch
			{
				return new List<string>();
			}
		}

		#endregion

		#region === Dataset helper Methods ===
		/// <summary>
		/// Check is dataset is valid and has at least one table with at least one row
		/// </summary>
		/// <param name="ds"></param>
		/// <returns></returns>
		public static bool DoesDataSetHaveRows( DataSet ds )
		{

			try
			{
				if ( ds != null && ds.Tables.Count > 0 && ds.Tables[ 0 ].Rows.Count > 0 )
					return true;
				else
					return false;
			}
			catch
			{

				return false;
			}
		}//

		/// <summary>
		/// Helper method to retrieve a string column from a row but will ignore missing columns (unlike GetRowColumn)
		/// </summary>
		/// <param name="row"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public static string GetRowPossibleColumn( DataRow row, string column )
		{
			string colValue = "";

			try
			{
				colValue = row[ column ].ToString();

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = "";

			}
			catch ( Exception ex )
			{
				//this method will ignore not found
				colValue = "";
			}
			return colValue;

		} // end method
		public static int GetRowPossibleColumn( DataRow row, string column, int defaultValue )
		{
			int colValue = 0;

			try
			{
				colValue = Int32.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				//this method will ignore not found
				colValue = defaultValue;
			}
			return colValue;

		} // end method
		/// <summary>
		/// Helper method to retrieve a string column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <returns></returns>
		public static string GetRowColumn( DataRow row, string column )
		{
			return GetRowColumn( row, column, "" );
		} // end method

		/// <summary>
		/// Helper method to retrieve a string column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static string GetRowColumn( DataRow row, string column, string defaultValue )
		{
			string colValue = "";

			try
			{
				colValue = row[ column ].ToString();

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				if ( column.IndexOf( "CUSTOMER_STATUS" ) > -1 )
				{
					//ignore

				}
				else
				{

					string exType = ex.GetType().ToString();
					LoggingHelper.LogError( exType + " Exception in GetRowColumn( DataRow row, string column, string defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				}
				colValue = defaultValue;
			}
			return colValue;

		} // end method
		/// <summary>
		/// Helper method to retrieve an int column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static int GetRowColumn( DataRow row, string column, int defaultValue )
		{
			int colValue = 0;

			try
			{
				colValue = Int32.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{


				LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, int defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		} // end method

		/// <summary>
		/// Helper method to retrieve a bool column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static bool GetRowColumn( DataRow row, string column, bool defaultValue )
		{
			bool colValue;

			try
			{
				colValue = Boolean.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, bool defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		} // end method

		/// <summary>
		/// Helper method to retrieve a DateTime column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>      
		public static System.DateTime GetRowColumn( DataRow row, string column, System.DateTime defaultValue )
		{
			System.DateTime colValue;

			try
			{
				colValue = System.DateTime.Parse( row[ column ].ToString() );
			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, System.DateTime defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
			}
			return colValue;

		} // end method


		/// <summary>
		/// Helper method to retrieve a column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static decimal GetRowColumn( DataRow row, string column, decimal defaultValue )
		{
			decimal colValue = 0;

			try
			{
				colValue = Convert.ToDecimal( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, decimal defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
			}
			return colValue;

		} // end method

		public static string GetRowColumn( DataRowView row, string column )
		{
			return GetRowColumn( row, column, "" );
		} // end method

		/// <summary>
		/// Helper method to retrieve a string column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static string GetRowColumn( DataRowView row, string column, string defaultValue )
		{
			string colValue = "";

			try
			{
				colValue = row[ column ].ToString();

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				string exType = ex.GetType().ToString();
				LoggingHelper.LogError( exType + " Exception in GetRowColumn( DataRowView row, string column, string defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
			}
			return colValue;

		} // end method

		/// <summary>
		/// Helper method to retrieve a string column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRowView</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static int GetRowColumn( DataRowView row, string column, int defaultValue )
		{
			int colValue = 0;

			try
			{
				colValue = Int32.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRowView row, string column, int defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		} // end method

		/// <summary>
		/// Helper method to retrieve a bool column from a row while handling invalid values
		/// </summary>
		/// <param name="row"></param>
		/// <param name="column"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static bool GetRowColumn( DataRowView row, string column, bool defaultValue )
		{
			bool colValue;

			try
			{
				colValue = Boolean.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				LoggingHelper.LogError( "Exception in GetRowColumn( DataRowView row, string column, bool defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		} // end method

		/// <summary>
		/// Helper method to retrieve a column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRowView</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static decimal GetRowColumn( DataRowView row, string column, decimal defaultValue )
		{
			decimal colValue = 0;

			try
			{
				colValue = Convert.ToDecimal( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( "Exception in GetRowColumn( DataRowView row, string column, decimal defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString(), true );
				colValue = defaultValue;
			}
			return colValue;

		} // end method

		#endregion

		#region Common Utility Methods
		public static string HandleApostrophes( string strValue )
		{

			if ( strValue.IndexOf( "'" ) > -1 )
			{
				strValue = strValue.Replace( "'", "''" );
			}
			if ( strValue.IndexOf( "''''" ) > -1 )
			{
				strValue = strValue.Replace( "''''", "''" );
			}

			return strValue;
		}
		public static String CleanText( String text )
		{
			if ( String.IsNullOrEmpty( text.Trim() ) )
				return String.Empty;

			String output = String.Empty;
			try
			{
				String rxPattern = "<(?>\"[^\"]*\"|'[^']*'|[^'\">])*>";
				Regex rx = new Regex( rxPattern );
				output = rx.Replace( text, String.Empty );
				if ( output.ToLower().IndexOf( "<script" ) > -1
					|| output.ToLower().IndexOf( "javascript" ) > -1 )
				{
					output = "";
				}
			}
			catch ( Exception ex )
			{

			}

			return output;
		}
		/// <summary>
		/// Format a string item for a search string (where)
		/// </summary>
		/// <param name="sqlWhere"></param>
		/// <param name="colName"></param>
		/// <param name="colValue"></param>
		/// <param name="booleanOperator"></param>
		/// <returns></returns>
		public static string FormatSearchItem( string sqlWhere, string colName, string colValue, string booleanOperator )
		{
			string item = "";
			string boolean = " ";

			if ( colValue.Length == 0 )
				return "";

			if ( sqlWhere.Length > 0 )
			{
				boolean = " " + booleanOperator + " ";
			}
			//allow asterisks
			colValue = colValue.Replace( "*", "%" );

			if ( colValue.IndexOf( "%" ) > -1 )
			{
				item = boolean + " (" + colName + " like '" + colValue + "') ";
			}
			else
			{
				item = boolean + " (" + colName + " = '" + colValue + "') ";
			}

			return item;

		}	// End method

		/// <summary>
		/// Format an integer item for a search string (where)
		/// </summary>
		/// <param name="sqlWhere"></param>
		/// <param name="colName"></param>
		/// <param name="colValue"></param>
		/// <param name="booleanOperator"></param>
		/// <returns></returns>
		public static string FormatSearchItem( string sqlWhere, string colName, int colValue, string booleanOperator )
		{
			string item = "";
			string boolean = " ";

			if ( sqlWhere.Length > 0 )
			{
				boolean = " " + booleanOperator + " ";
			}

			item = boolean + " (" + colName + " = " + colValue + ") ";

			return item;

		}	// End method

		/// <summary>
		/// Format an item for a search string (where)
		/// </summary>
		/// <param name="sqlWhere"></param>
		/// <param name="filter"></param>
		/// <param name="booleanOperator"></param>
		/// <returns></returns>
		public static string FormatSearchItem( string sqlWhere, string filter, string booleanOperator )
		{
			string item = "";
			string boolean = " ";

			if ( filter.Trim().Length == 0 )
				return "";

			if ( sqlWhere.Length > 0 )
			{
				boolean = " " + booleanOperator + " ";
			}

			item = boolean + " (" + filter + ") ";

			return item;

		}	// End method

		#endregion


		#region HttpSessionState Methods
		public static void SessionSet( string key, System.Object sysObject )
		{
			if ( HttpContext.Current.Session != null )
			{
				SessionSet( HttpContext.Current.Session, key, sysObject );
			}

		} //
		/// <summary>
		/// Helper Session method - future use if required to chg to another session provider such as SQL Server 
		/// </summary>
		/// <param name="session"></param>
		/// <param name="key"></param>
		/// <param name="sysObject"></param>
		public static void SessionSet( HttpSessionState session, string key, System.Object sysObject )
		{

			session[ key ] = sysObject;

		} //
		/// <summary>
		/// Get a key from a session, default to blank if not found
		/// </summary>
		/// <param name="key">Key for session</param>
		/// <returns>string</returns>
		public static string SessionGet( string key )
		{
			if ( HttpContext.Current.Session != null )
			{
				return SessionGet( HttpContext.Current.Session, key, "" );
			}
			else
				return null;
		} //

		public static string SessionGet( string key, string defaultValue )
		{
			if ( HttpContext.Current.Session != null )
			{
				return SessionGet( HttpContext.Current.Session, key, defaultValue );
			}
			else
				return null;
		} //
		/// <summary>
		/// Get a key from a session, default to blank if not found
		/// </summary>
		/// <param name="session">HttpSessionState</param>
		/// <param name="key">Key for session</param>
		/// <returns>string</returns>
		public static string SessionGet( HttpSessionState session, string key, string defaultValue )
		{

			string value = "";
			try
			{
				if ( session[ key ] != null )
					value = session[ key ].ToString();
				else
					value = defaultValue;

			}
			catch ( Exception ex )
			{
				value = defaultValue;
			}


			return value;
		} //
		#endregion
	}
}
