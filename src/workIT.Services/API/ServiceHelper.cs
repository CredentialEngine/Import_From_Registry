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
using Newtonsoft.Json;

using workIT.Utilities;
using MC = workIT.Models.Common;
using WMA = workIT.Models.API;
using ME = workIT.Models.Elastic;
using MQD = workIT.Models.QData;
using WMP = workIT.Models.ProfileModels;
using WMS = workIT.Models.Search;
using workIT.Factories;


namespace workIT.Services.API
{
	public class ServiceHelper
	{
		//externalFinderSiteURL is ????????????
		public static string reactFinderSiteURL = UtilityManager.GetAppKeyValue( "credentialFinderMainSite" );
		public static string oldCredentialFinderSite = UtilityManager.GetAppKeyValue( "oldCredentialFinderSite" );
		public static string finderApiSiteURL = UtilityManager.GetAppKeyValue( "finderApiSiteURL" );

		#region Mapping for Finder API
		/// <summary>
		/// Format role based organization search links
		/// </summary>
		/// <param name="orgId"></param>
		/// <param name="orgName"></param>
		/// <param name="entityCount"></param>
		/// <param name="labelTemplate"></param>
		/// <param name="searchType"></param>
		/// <param name="output"></param>
		/// <param name="roles"></param>
		public static void MapEntitySearchLink( int orgId, string orgName, int entityCount, string labelTemplate, string searchType, ref List<WMA.LabelLink> output, string roles = "6,7", string orgCTID="" )
		{
			//var output = new WMA.LabelLink();
			if ( orgId < 1 )
				return;
			//21-07-15 mp - the following now results in the QA and owner QA to be skipped?
			if ( entityCount < 1 && roles != "1,2,10,12")
				return;
			//note need the friendly name
			//
			//search?autosearch=true&amp;searchType=credential&amp;custom={n:'organizationroles',aid:957,rid:[6,7],p:'Bates+Technical+College',d:'Owns/Offers 2 Credential(s)'}
			//var label = string.Format( "Owns/Offers {0} Credential(s)", entityCount );
			try
			{
				//=====OLD ========================
				var roleList = roles.Split( ',' ).ToList();
				var label = string.Format( labelTemplate, entityCount );
				var urlLabel = orgName + ": " + label;
				//var customTest = string.Format( "custom=(n:'organizationroles',aid:{0},rid:[{3}],p:'{1}',r:'',d:'{2}')", orgId, orgName, label, roles );
				//retaining r:'', for test url otherwise get undefined
				var oldUrl = string.Format( "search?autosearch=true&searchType={0}&custom=((n:'organizationroles',aid:{1},rid:[{2}],p:'{3}',d:'{4}',r:''))", searchType, orgId, roles, orgName, HttpUtility.UrlPathEncode( label ) );
				oldUrl = oldUrl.Replace( "((", "{" ).Replace( "))", "}" );
				oldUrl = oldUrl.Replace( "'", "%27" ).Replace( " ", "%20" );
				//oldUrl = HttpUtility.UrlPathEncode ( oldUrl );

				//new --------------------------------------
				var part1 = string.Format( "search?searchType={0}&filteritemtext={1}", searchType, HttpUtility.UrlPathEncode( urlLabel ) );
				if ( !string.IsNullOrWhiteSpace( orgCTID ) )
					part1 += "&keywords=" + orgCTID.Trim();
				//json format,probably not
				var part2 = "{" + string.Format( "\"n\":\"organizationroles\",\"aid\":{0},\"rid\":[{1}]", orgId, roles ) + "}";
				var part3 = HttpUtility.UrlPathEncode( part2 );
				var part4 = "&filterparameters=" + part3;

				output.Add( new WMA.LabelLink()
				{
					Label = label,
					Total = entityCount,
					URL = reactFinderSiteURL + part1 + part4,
					TestURL = oldCredentialFinderSite + oldUrl
				} );



				//output.Add( new WMA.LabelLink()
				//{
				//	Label = label,
				//	Count = entityCount,
				//	URL = reactFinderSiteURL + part1 + part4
				//} );


				//OR use class
				//var parms = new RolesFilter()
				//{
				//	n = "organizationroles",
				//	aid = orgId,
				//	rid = roleList
				//};
				//var pj = JsonConvert.SerializeObject( parms, JsonHelper.GetJsonSettings() );
				//part3 = HttpUtility.UrlPathEncode ( pj );
				//part4 = "&filterparameters=" + part3;

				//filter = ( new WMA.LabelLink()
				//{
				//	Label = label,
				//	Count = entityCount,
				//	URL = reactFinderSiteURL + part1 + part4
				//} );

				//output.Add( filter );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, "" + ex.Message );
			}
			//return output;

		}

		public static void MapQAPerformedLink( int orgId, string orgName, int entityCount, string labelTemplate, string searchType, ref List<WMA.LabelLink> output )
		{
			//var output = new WMA.LabelLink();
			if ( orgId < 1 )
				return;
			//
			//search?autosearch=true&amp;searchType=credential&amp;custom={n:'organizationroles',aid:957,rid:[6,7],p:'Bates+Technical+College',d:'Owns/Offers 2 Credential(s)'}
			//var label = string.Format( "Owns/Offers {0} Credential(s)", entityCount );
			try
			{
				var roles = "1,2,10,12";

				var label = string.Format( labelTemplate, entityCount );
				var urlLabel = orgName + ": " + label;
				var url = string.Format( "search?autosearch=true&searchType={0}&custom=((n:'organizationroles',aid:{1},rid:[1,2,10,12],p:'{2}',d:'{3}',r:''))", searchType, orgId, orgName, HttpUtility.UrlPathEncode( label ) );
				url = url.Replace( "((", "{" ).Replace( "))", "}" );
				url = url.Replace( "'", "%27" ).Replace( " ", "%20" );
				//url = HttpUtility.UrlPathEncode ( url );

				//new --------------------------------------
				//var roleList = roles.Split( ',' ).ToList();
				var part1 = string.Format( "search?searchType={0}&filteritemtext={1}", searchType, HttpUtility.UrlPathEncode( urlLabel ) );
				//json format,probably not
				var part2 = "{" + string.Format( "\"n\":\"organizationroles\",\"aid\":{0},\"rid\":[{1}]", orgId, roles ) + "}";
				var part3 = HttpUtility.UrlPathEncode( part2 );
				var part4 = "&filterparameters=" + part3;

				output.Add( new WMA.LabelLink()
				{
					Label = label,
					Total = entityCount,
					URL = reactFinderSiteURL + part1 + part4,
					TestURL = oldCredentialFinderSite + url
				} );



				//output.Add( new WMA.LabelLink()
				//{
				//	Label = label,
				//	Count = entityCount,
				//	URL = reactFinderSiteURL + part1 + part4
				//} );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, "ServiceHelper.MapQAPerformedLink" + ex.Message );
			}
			//return output;

		}

		public static bool AreOnlyRolesOwnsOffers( List<WMP.OrganizationRoleProfile> input )
		{
			//if ( input == null || !input.Any()  )
			//	return false;
			//if ( input.Count() == 1 )
			//{
			//	if ( input[ 0 ].AgentRole == null || input[ 0 ].AgentRole.Items == null || input[ 0 ].AgentRole.Items.Count != 2 )
			//		return false;
			//	//
			//	var check = input[ 0 ].AgentRole.Items.Where( s => s.Id == 6 || s.Id == 7 ).ToList();
			//	if ( check != null && check.Count == 2 )
			//		return true;
			//	else
			//		return false;
			//}

			//var check2 = input.AgentRole.Items.Select( s => s.Id == 6 || s.Id == 7 ).ToList();
			//var check3 = input.Select( s => s.AgentRole.Items.Select( x => x.Id == 6 || x.Id == 7 )).ToList();
			//var check4 = input.Select( s => s.AgentRole.Items.Where( x => x.Id == 6 || x.Id == 7 ) ).ToList();
			//var check5 = input.Select( s => s.AgentRole ).ToList();
			//var check6 = check5.Select( x => x.Items ).ToList();
			//var check7 = check6.Where( x => x.).ToList();


			return false;

		}


		/// <summary>
		/// Method to map renewed by and revoked by 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="searchType"></param>
		/// <param name="roleTypeId"></param>
		/// <returns></returns>
		public static List<WMA.Outline> MapRoleReceived( List<WMP.OrganizationRoleProfile> input, string searchType, int roleTypeId )
		{
			if ( input == null || !input.Any() )
				return null;
			//
			var output = new List<WMA.Outline>();
			var searchRoles = "11,13";

			try
			{
				foreach ( var item in input )
				{
					var orp = new WMA.Outline()
					{
						Label = string.IsNullOrWhiteSpace( item.ProfileName ) ? item.ParentSummary : item.ProfileName,
						Description = item.Description ?? ""
					};
					if ( string.IsNullOrWhiteSpace( orp.Label ) )
					{
						if ( item.ActingAgent != null && item.ActingAgent.Id > 0 )
						{
							orp.Label = item.ActingAgent.Name;
							orp.Description = item.ActingAgent.Description;
						}
					}
					if ( string.IsNullOrEmpty( item.ActingAgent.CTID ) )
						orp.URL = item.ActingAgent.SubjectWebpage;
					else
						orp.URL = reactFinderSiteURL + string.Format( "organization/{0}/{1}", item.ActingAgent.Id, string.IsNullOrWhiteSpace( item.ActingAgent.Name ) ? "" : item.ActingAgent.FriendlyName );
					bool isPublishedByRole = false;
					if ( item.AgentRole != null && item.AgentRole.Items.Any() )
					{
						foreach ( var ar in item.AgentRole.Items )
						{
							if ( ar.Id == roleTypeId )
							{
								//should this be the reverseTitle?
								if ( item.ActingAgent != null && item.ActingAgent.Id > 0 )
								{
									//for now include both renews and revokes in the link
									//21-09-24 mp - when called from a credential, we don't want the search link?
									//				- also 0 is passed for EntityCount. This will result in no tags?
									MapEntitySearchLink( item.ActingAgent.Id, item.ActingAgent.Name, 0, ar.Name, searchType, ref orp.Tags, searchRoles );//ar.Id.ToString()
									//21-09-24 mp add to Tags anyway
									orp.Tags.Add( new WMA.LabelLink() { Label = ar.Name } );
								}
								else
									orp.Tags.Add( new WMA.LabelLink() { Label = ar.Name } );
							}
						}
					}
					if ( !isPublishedByRole && orp.Tags.Any() )
						output.Add( orp );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, "ServiceHelper.MapRoleReceived" + ex.Message );
			}
			return output;

		}
		public static List<WMA.Outline> MapQAReceived( List<WMP.OrganizationRoleProfile> input, string searchType )
		{
			if ( input == null || !input.Any() )
				return null;
			//
			var output = new List<WMA.Outline>();
			var qaroles = "1,2,10,12";

			try
			{
				foreach ( var item in input )
				{
					var orp = new WMA.Outline()
					{
						Label = string.IsNullOrWhiteSpace( item.ProfileName ) ? item.ParentSummary : item.ProfileName,
						Description = item.Description ?? ""
					};
					if ( string.IsNullOrWhiteSpace( orp.Label ) )
					{
						if ( item.ActingAgent != null && item.ActingAgent.Id > 0 )
						{
							orp.Label = item.ActingAgent.Name;
							orp.Description = item.ActingAgent.Description;
						}
					}
					if ( string.IsNullOrEmpty( item.ActingAgent.CTID ) )
						orp.URL = item.ActingAgent.SubjectWebpage;
					else
					{
						orp.URL = reactFinderSiteURL + string.Format( "organization/{0}/{1}", item.ActingAgent.Id, string.IsNullOrWhiteSpace(item.ActingAgent.FriendlyName) ? "" : item.ActingAgent.FriendlyName );
					}
					bool isPublishedByRole = false;
					if ( item.AgentRole != null && item.AgentRole.Items.Any() )
					{
						foreach ( var ar in item.AgentRole.Items )
						{
							//no link
							if ( ar.Id == 30 )
							{
								//if published by, probably will not have other roles!
								//unless of course the WDI testing 
								//continue;
								isPublishedByRole = true;
								break;
							}
							else if ( ar.Id == 20 || ar.Id == 21 || ar.Id == 22 )
							{
								//skip dept, subsidiary and parent
								continue;
							} else if ( ar.Id == 0 || ar.Id == 6 || ar.Id == 7 || ar.Id == 11 || ar.Id == 13 )
							{
								//skip where owns/offers/revokes/renews
								continue;
							}
							//should this be the reverseTitle?
							if ( item.ActingAgent != null && item.ActingAgent.Id > 0 )
							{
								//if role is QA, include all 4 in link
								MapEntitySearchLink( item.ActingAgent.Id, item.ActingAgent.Name, 0, ar.Name, searchType, ref orp.Tags, qaroles );//ar.Id.ToString()
							}
							else
								orp.Tags.Add( new WMA.LabelLink() { Label = ar.Name } );
						}
					}
					//!isPublishedByRole && 
					if ( orp.Tags.Any() )
						output.Add( orp );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, "ServiceHelper.MapQAReceived" + ex.Message );
			}
			return output;

		}

		/// <summary>
		/// Map to Outline for a more generic display, like owned and offered by 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="roleTypeId">If GT zero, only roles with the latter will be formatted and returned. Use zero where all related orgs are to be returned</param>
		/// <returns></returns>
		public static List<WMA.Outline> MapOrganizationRoleProfileToOutline( List<WMP.OrganizationRoleProfile> input, int roleTypeId )
		{

			if ( input == null || !input.Any() )
				return null;
			var output = new List<WMA.Outline>();
			try
			{
				//var list = new List<MC.TopLevelEntityReference>();
				foreach ( var item in input )
				{
					var orp = new WMA.Outline()
					{
						Label = string.IsNullOrWhiteSpace( item.ProfileName ) ? item.ParentSummary : item.ProfileName,
						Description = item.Description ?? ""
					};
					if ( string.IsNullOrWhiteSpace( orp.Label ) )
					{
						if ( item.ActingAgent != null && item.ActingAgent.Id > 0 )
						{
							orp.Label = item.ActingAgent.Name;
							orp.Description = item.ActingAgent.Description;
							orp.Meta_Id = item.ActingAgent.Id;
							orp.Image = item.ActingAgent.Image;
						}
					}
					if ( string.IsNullOrEmpty( item.ActingAgent.CTID ) )
						orp.URL = item.ActingAgent.SubjectWebpage;
					else
					{
						orp.URL = reactFinderSiteURL + string.Format( "organization/{0}", item.ActingAgent.Id ) + ( !string.IsNullOrWhiteSpace( item.FriendlyName ) ? "/" + item.FriendlyName : "" );

					}
					//
					//not sure if we want all roles here - current case could be for parent org
					if ( roleTypeId > 0 && item.AgentRole != null && item.AgentRole.Items.Any() )
					{
						foreach ( var ar in item.AgentRole.Items )
						{
							if ( ar.Id == roleTypeId )
							{
								//should this be the reverseTitle?
								//if ( item.ActingAgent != null && item.ActingAgent.Id > 0 )
								//{
								//	//for now include both renews and revokes in the link
								//	//MapEntitySearchLink( item.ActingAgent.Id, item.ActingAgent.Name, 0, ar.Name, searchType, ref orp.Tags, searchRoles );//ar.Id.ToString()
								//}
								//else
								orp.Tags.Add( new WMA.LabelLink() { Label = ar.Name } );
							}
						}
						if ( orp.Tags.Any() )
							output.Add( orp );
					} else
					{
						if ( roleTypeId == 0 )
							output.Add( orp );
					}

					//output.Add( orp );
				}
				if ( !output.Any() )
					return null;

				return output;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "ServiceHelper.MapOrganizationRoleProfileToOutline" );
				return null;
			}

		}

		/// <summary>
		/// Format Concept based search links
		/// </summary>
		/// <param name="input"></param>
		/// <param name="searchType"></param>
		/// <param name="formatUrl"></param>
		/// <returns></returns>
		public static List<WMA.LabelLink> MapPropertyLabelLinks( MC.Enumeration input, string searchType, bool formatUrl = true )
		{
			var output = new List<WMA.LabelLink>();
			if ( input == null || input.Items == null || input.Items.Count() == 0 )
				return null;

			//
			//https://sandbox.credentialengine.org/finder/search?autosearch=true&searchType=organization&filters=7-1185
			foreach ( var item in input.Items )
			{
				var value = new WMA.LabelLink()
				{
					SearchType = searchType,
					Label = item.Name,//confirm this will be consistant					
				};
				var oldUrl = oldCredentialFinderSite + string.Format( "search?autosearch=true&searchType={0}&filters={1}-{2}", searchType, input.Id, item.Id );
				var url = reactFinderSiteURL + string.Format( "search?searchType={0}&filterid={1}&filteritemid={2}", searchType, input.Id, item.Id );
				if ( formatUrl && !string.IsNullOrWhiteSpace( searchType ) )
				{
					value.URL = url;
					value.TestURL = oldUrl;
				}

				output.Add( value );

			}

			return output;

		}
		/// <summary>
		/// Format Concept based search links - single result
		/// </summary>
		/// <param name="input"></param>
		/// <param name="searchType"></param>
		/// <param name="formatUrl"></param>
		/// <returns></returns>
		public static WMA.LabelLink MapPropertyLabelLink( MC.Enumeration input, string searchType, bool formatUrl = true )
		{
			var output = new WMA.LabelLink();
			if ( input == null || input.Items == null || input.Items.Count() == 0 )
				return null;
			//
			foreach ( var item in input.Items )
			{
				var value = new WMA.LabelLink()
				{
					Label = item.Name,//confirm this will be consistant	

				};
				var oldUrl = oldCredentialFinderSite + string.Format( "search?autosearch=true&searchType={0}&filters={1}-{2}", searchType, input.Id, item.Id );
				var url = reactFinderSiteURL + string.Format( "search?searchType={0}&filterid={1}&filteritemid={2}", searchType, input.Id, item.Id );

				if ( formatUrl && !string.IsNullOrWhiteSpace( searchType ) )
				{
					value.URL = url;
					value.TestURL = oldUrl;
				}

				output = value;
				break;
			}

			return output;

		}

		public static WMA.LabelLink MapDetailLink( string entityType, string label, int id )
		{
			if ( string.IsNullOrWhiteSpace( entityType ) || string.IsNullOrWhiteSpace( label ) )
				return null;
			//
			var oldUrl = oldCredentialFinderSite + string.Format( "{0}/{1}/{2}", entityType, id, label );
			var url = reactFinderSiteURL + string.Format( "{0}/{1}/{2}", entityType, id, label );
			var output = new WMA.LabelLink()
			{
				Label = label,
				URL = url,
				TestURL = oldUrl
			};

			return output;

		}
		public static WMA.LabelLink MapDetailLink( string entityType, string label, int id, string friendlyName  )
		{
			if ( string.IsNullOrWhiteSpace( entityType ) || string.IsNullOrWhiteSpace( label ) )
				return null;
			//
			var oldUrl = oldCredentialFinderSite + string.Format( "{0}/{1}/{2}", entityType, id, label );
			var url = reactFinderSiteURL + string.Format( "{0}/{1}/{2}", entityType, id, string.IsNullOrWhiteSpace( friendlyName ) ? "" : friendlyName );
			var output = new WMA.LabelLink()
			{
				Label = label,
				URL = url,
				TestURL = oldUrl
			};

			return output;

		}
		public static WMA.LabelLink MapPropertyLabelLink( string input, string label, bool formatUrl = true )
		{
			if ( string.IsNullOrWhiteSpace( input ) )
				return null;
			//
			var output = new WMA.LabelLink()
			{
				Label = label,
				URL = formatUrl ? input : ""
			};

			return output;

		}
		public static WMA.LabelLink MapPropertyLabelLink( string input, string label, string description, bool formatUrl = true )
		{
			if ( string.IsNullOrWhiteSpace( input ) && string.IsNullOrWhiteSpace( description ) )
				return null;
			//
			var output = new WMA.LabelLink()
			{
				Label = label,
				Description = description,
				URL = formatUrl ? input : ""
			};

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
		public static List<WMA.LabelLink> MapReferenceFrameworkLabelLink( MC.Enumeration input, string searchType, int frameworkCategoryId = 10 )
		{
			var output = new List<WMA.LabelLink>();
			if ( input == null || input.Items == null || input.Items.Count() == 0 )
				return output;
			//
			//https://sandbox.credentialengine.org/finder/search?autosearch=true&searchType=organization&filters=7-1185
			foreach ( var item in input.Items )
			{
				var label = HttpUtility.UrlPathEncode( item.ItemSummary );
				var oldUrl = oldCredentialFinderSite + string.Format( "search?autosearch=true&searchType={0}&keywords={1}", searchType, label );
				var url = reactFinderSiteURL + string.Format( "search?searchType={0}&filterid={1}&filteritemtext={2}", searchType, frameworkCategoryId, label );

				var value = new WMA.LabelLink()
				{
					Label = item.ItemSummary,
					URL = url,
					TestURL = oldUrl

				};
				output.Add( value );
			}

			return output;

		}

		public static List<WMA.ReferenceFramework> MapReferenceFramework( List<MC.CredentialAlignmentObjectProfile> input, string searchType, int frameworkCategoryId, int widgetId = 0 )
		{
			var output = new List<WMA.ReferenceFramework>();
			if ( input == null || input.Count() == 0 )
				return output;
			//
			foreach ( var item in input )
			{
				var label = HttpUtility.UrlPathEncode( item.ItemSummary );

				var url = reactFinderSiteURL + string.Format( "search?searchType={0}&filterid={1}&filteritemtext={2}", searchType, frameworkCategoryId, label );

				var value = new WMA.ReferenceFramework()
				{
					Label = item.ItemSummary,
					CodedNotation = item.CodedNotation,
					TargetNode = item.TargetNode,
					Description = item.Description,
					URL = url,
					Framework = item.Framework,
					FrameworkName = item.FrameworkName,

				};
				output.Add( value );
			}

			return output;

		}

		/// <summary>
		/// Format search links for main search
		/// </summary>
		/// <param name="input"></param>
		/// <param name="searchType"></param>
		/// <returns></returns>
		public static List<WMA.LabelLink> MapPropertyLabelLinks( List<WMP.TextValueProfile> input, string searchType, int categoryId = 0 )
		{
			var output = new List<WMA.LabelLink>();
			if ( input == null || input.Count() == 0 )
				return output;
			//
			//search?autosearch=true&amp;searchType=organization&amp;keywords=Career and Technical Education
			var filterId = "";
			if ( categoryId > 0 )
				filterId = string.Format( "&filterid={0}", categoryId );
			foreach ( var item in input )
			{
				var keyword = HttpUtility.UrlPathEncode( item.TextValue );

				if ( !string.IsNullOrWhiteSpace( item.TextValue ) )
				{
					var value = new WMA.LabelLink()
					{
						Label = item.TextValue,//confirm this will be consistant
						URL = reactFinderSiteURL + string.Format( "search?autosearch=true&searchType={0}&keywords={1}{2}", searchType, keyword, filterId )
					};
					output.Add( value );
				}
			}

			return output;

		}

		#endregion
		public static List<string> MapTextValueProfileToStringList( List<WMP.TextValueProfile> input )
		{
			var output = new List<string>();
			if ( input == null || input.Count() == 0 )
				return output;
			//
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

		public static List<ME.JurisdictionProfile> MapJurisdiction( List<MC.JurisdictionProfile> input, string assertionType = "" )
		{
			var output = new List<ME.JurisdictionProfile>();
			if ( input == null || input.Count() == 0 )
				return null;
			//
			foreach ( var item in input )
			{
				var pp = new ME.JurisdictionProfile()
				{
					Description = string.IsNullOrWhiteSpace( item.Description ) ? null : item.Description,
					GlobalJurisdiction = item.GlobalJurisdiction,
					MainJurisdiction = null
				};
				//map address-need a helper to format the jurisdiction - rare
				//**** need to handle GeoCoordinates
				if ( item.MainJurisdiction != null )
				{
					//check - likely the data is in 
					if ( item.MainJurisdiction.Address != null )
					{
						pp.MainJurisdiction = new ME.Address()
						{
							Name = item.MainJurisdiction.Address.Name,
							Description = item.MainJurisdiction.Address.Description,
							AddressLocality = item.MainJurisdiction.Address.AddressLocality,
							AddressRegion = item.MainJurisdiction.Address.AddressRegion,
							AddressCountry = item.MainJurisdiction.Address.AddressCountry,
							Latitude = item.MainJurisdiction.Address.Latitude,
							Longitude = item.MainJurisdiction.Address.Longitude
						};
					}
					else
					{
						if ( item.MainJurisdiction.HasData() )
						{
							pp.MainJurisdiction = new ME.Address()
							{
								Name = item.MainJurisdiction.Name,
								//Description = item.MainJurisdiction.Description,
								AddressRegion = item.MainJurisdiction.Region,
								AddressCountry = item.MainJurisdiction.Country,
								Latitude = item.MainJurisdiction.Latitude,
								Longitude = item.MainJurisdiction.Longitude
							};
						}

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
								Description = je.Address.Description,
								AddressLocality = je.Address.AddressLocality,
								AddressRegion = je.Address.AddressRegion,
								AddressCountry = je.Address.AddressCountry,
								Latitude = je.Address.Latitude,
								Longitude = je.Address.Longitude
							};
							pp.JurisdictionException.Add( j );
						}
						else
						{
							if ( je.HasData() )
							{
								var j = new ME.Address()
								{
									Name = je.Name,
									AddressRegion = je.Region,
									AddressCountry = je.Country,
									Latitude = je.Latitude,
									Longitude = je.Longitude
								};
								pp.JurisdictionException.Add( j );
							}
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
						//pp.AssertedBy = MapToEntityReference( item.AssertedByOrganization, "organization" );
						pp.AssertedBy = MapToOutline( item.AssertedByOrganization, "organization" );
						//
					}
				}

				//
				output.Add( pp );

			};

			return output;

		}
		//
		public static List<WMA.Address> MapAddress( List<MC.Address> input )
		{
			var output = new List<WMA.Address>();
			//addresses
			if ( input != null && input.Any() )
			{
				foreach ( var item in input )
				{
					var address = new WMA.Address()
					{
						Name = item.Name,
						StreetAddress = item.StreetAddress,
						Description = item.Description,
						PostOfficeBoxNumber = item.PostOfficeBoxNumber,
						AddressLocality = item.AddressLocality,
						SubRegion = item.SubRegion ?? "",
						AddressRegion = item.AddressRegion,
						PostalCode = item.PostalCode,
						AddressCountry = item.AddressCountry,
						//identifiers - probably need to customize?
						//Identifier = MapIdentifierValue( item.IdentifierOLD )
					};
					// assign lat/lng. If the latter are not available and an address exists provide the default 'center'
					if ( item.Latitude != 0 )
						address.Latitude = item.Latitude;
					else if ( string.IsNullOrWhiteSpace( item.StreetAddress ) ) //lat: 39.8283, lng: -98.5795 
						address.Latitude = 39.8283;
					if ( item.Longitude != 0 )
						address.Longitude = item.Longitude;
					else if ( string.IsNullOrWhiteSpace( item.StreetAddress ) ) //lat: 39.8283, lng: -98.5795 
						address.Longitude = -98.5795;

					if ( item.HasContactPoints() )
					{
						//???
						//output.ContactPoint = new List<WMA.ContactPoint>();
						address.TargetContactPoint = new List<WMA.ContactPoint>();
						foreach ( var cp in item.ContactPoint )
						{
							var cpOutput = new WMA.ContactPoint()
							{
								Name = cp.Name,
								ContactType = cp.ContactType,
								Email = cp.Emails,
								Telephone = cp.PhoneNumbers,
								FaxNumber = cp.FaxNumber,
								SocialMedia = cp.SocialMediaPages
							};
							//should be one or the other
							if ( cp.PhoneNumber != null && cp.PhoneNumber.Any() )
								cpOutput.Telephone = ServiceHelper.MapTextValueProfileToStringList( cp.PhoneNumber );

							if ( cp.SocialMedia != null && cp.SocialMedia.Any() )
								cpOutput.SocialMedia = ServiceHelper.MapTextValueProfileToStringList( cp.SocialMedia );

							if ( cpOutput.Email.Any() || cpOutput.Telephone.Any() || cpOutput.SocialMedia.Any() || cp.FaxNumber.Any() )
								address.TargetContactPoint.Add( cpOutput );

							//address.TargetContactPoint.Add( cpOutput );
						}
					}
					output.Add( address );
				}

			}

			return output;
		}

		//
		public static List<WMA.IdentifierValue> MapIdentifierValue( List<WMP.Entity_IdentifierValue> input, string label = "" )
		{
			var output = new List<WMA.IdentifierValue>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				if ( item.HasData() ) {
					var iv = new WMA.IdentifierValue()
					{
						IdentifierType = item.IdentifierType,
						IdentifierTypeName = string.IsNullOrWhiteSpace( item.IdentifierTypeName ) ? label : item.IdentifierTypeName,
						IdentifierValueCode = item.IdentifierValueCode
					};
					output.Add( iv );
				}
			}

			return output;
		}
		//
		public static List<WMA.IdentifierValue> MapIdentifierValue( List<MC.IdentifierValue> input, string label = "" )
		{
			var output = new List<WMA.IdentifierValue>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				if ( !string.IsNullOrWhiteSpace(item.IdentifierValueCode) )
				{
					var iv = new WMA.IdentifierValue()
					{
						IdentifierType = item.IdentifierType,
						IdentifierTypeName = string.IsNullOrWhiteSpace( item.IdentifierTypeName ) ? label : item.IdentifierTypeName,
						IdentifierValueCode = item.IdentifierValueCode
					};
					output.Add( iv );
				}
			}

			return output;
		}
		//
		public static List<WMA.ValueProfile> MapValueProfile( List<MC.ValueProfile> input, string searchType = "" )
		{
			var output = new List<WMA.ValueProfile>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				if ( item.HasData() )
				{
					var iv = new WMA.ValueProfile()
					{
						Description = item.Description,
					};
					//try to ensure props without data is not returned
					if ( item.Value > 0 )
						iv.Value = item.Value;
					if ( item.MinValue > 0 )
						iv.MinValue = item.MinValue;
					if ( item.MaxValue > 0 )
						iv.MaxValue = item.MaxValue;
					if ( item.Percentage > 0 )
						iv.Percentage = item.Percentage;
					if ( item.Subject != null && item.Subject.Items.Any() )
						iv.Subject = ServiceHelper.MapPropertyLabelLinks( item.Subject, searchType, false );
					//
					if ( item.CreditUnitType != null && item.CreditUnitType.Items.Any() )
						iv.CreditUnitType = ServiceHelper.MapPropertyLabelLinks( item.CreditUnitType, searchType, false );
					if ( item.CreditLevelType != null && item.CreditLevelType.Items.Any() )
						iv.CreditLevelType = ServiceHelper.MapPropertyLabelLinks( item.CreditLevelType, searchType, false );
					//
					output.Add( iv );
				}
			}

			return output;
		}

		//
		public static WMS.AJAXSettings MapAJAXProcessProfile( string label, string searchType, List<WMP.ProcessProfile> input )
		{
			if ( input == null || input.Count() == 0 )
				return null;

			var output = new WMS.AJAXSettings();
			var list = MapProcessProfile( searchType, input );
			if ( list != null && list.Any() )
			{
				output.Label = label;
				output.Total = list.Count;
				List<object> obj = list.Select( f => ( object )f ).ToList();
				output.Values = obj;
			}
			return output;
		}

		//
		public static List<WMA.ProcessProfile> MapProcessProfile( string searchType, List<WMP.ProcessProfile> input )
		{
			var output = new List<WMA.ProcessProfile>();
			if ( input == null || input.Count() == 0 )
				return output;

			//
			foreach ( var item in input )
			{
				var pp = new WMA.ProcessProfile()
				{
					//include id for use in modal windows
					Meta_Id = item.Id,

					//ProcessProfileType = item.ProcessType,
					Description = item.Description,
					DateEffective = item.DateEffective,
					ProcessFrequency = item.ProcessFrequency,
					ScoringMethodDescription = item.ScoringMethodDescription,
					SubjectWebpage = item.SubjectWebpage,
					VerificationMethodDescription = item.VerificationMethodDescription,
				};
				pp.ProcessMethod = ServiceHelper.MapPropertyLabelLink( item.ProcessMethod, "ProcessMethod", item.ProcessMethodDescription );
				pp.ProcessStandards = ServiceHelper.MapPropertyLabelLink( item.ProcessStandards, "ProcessStandards", item.ProcessStandardsDescription );
				pp.ScoringMethodExample = ServiceHelper.MapPropertyLabelLink( item.ScoringMethodExample, "ScoringMethodExample", item.ScoringMethodExampleDescription );

				pp.DataCollectionMethodType = MapPropertyLabelLinks( item.DataCollectionMethodType, searchType, false );
				pp.ExternalInputType = MapPropertyLabelLinks( item.ExternalInputType, searchType, false );
				//
				pp.Jurisdiction = MapJurisdiction( item.Jurisdiction );
				//
				pp.ProcessingAgent = null;
				//&& item.ProcessingAgent.Id != orgId
				if ( item.ProcessingAgent != null && item.ProcessingAgent.Id > 0 && !string.IsNullOrWhiteSpace( item.ProcessingAgent.Name ) )
				{
					var ab = ServiceHelper.MapToOutline( item.ProcessingAgent, "organization" );
					pp.ProcessingAgent = ServiceHelper.MapOutlineToAJAX( ab, "Asserted by {0} Organization(s)" );

					//if ( item.ProcessingAgent != null && !string.IsNullOrWhiteSpace( item.ProcessingAgent.Name ) )
					//	pp.ProcessingAgent = MapToEntityReference( item.ProcessingAgent, "organization" );
				}

				if ( item.TargetAssessment != null && item.TargetAssessment.Any() )
				{
					pp.TargetAssessment = MapAssessmentToAJAXSettings( item.TargetAssessment, "Has {0} Target Assessment(s)" );
				}
				if ( item.TargetCredential != null && item.TargetCredential.Any() )
				{
					pp.TargetCredential = MapCredentialToAJAXSettings( item.TargetCredential, "Has {0} Target Credential(s)" );
				}
				if ( item.TargetLearningOpportunity != null && item.TargetLearningOpportunity.Any() )
				{
					pp.TargetLearningOpportunity = MapLearningOppToAJAXSettings( item.TargetLearningOpportunity, "Has {0} Target Learning Opportunity(ies)" );
				}
				if ( item.TargetCompetencyFramework != null && item.TargetCompetencyFramework.Any() )
				{
					pp.TargetCompetencyFramework = MapCompetencyFrameworkToAJAXSettings( item.TargetCompetencyFramework, "Has {0} Target Target Competency Framework(s)" );
				}
				output.Add( pp );
			}

			return output;
		}
		//public static List<MC.TopLevelEntityReference> MapToEntityReference( List<MC.TopLevelObject> input, string entityType = "" )
		//{
		//	var output = new List<MC.TopLevelEntityReference>();
		//	if ( input == null || !input.Any() )
		//		return null;

		//	foreach ( var item in input )
		//	{
		//		var tlo = MapToEntityReference( item, entityType );
		//		if ( tlo != null && !string.IsNullOrWhiteSpace( tlo.Name ) )
		//		{
		//			output.Add( tlo );
		//		}
		//	}
		//	if ( !output.Any() )
		//		return null;

		//	return output;
		//}

		//public static List<MC.TopLevelEntityReference> MapToEntityReference( List<WMP.OrganizationRoleProfile> input )
		//{
		//	var output = new List<MC.TopLevelEntityReference>();
		//	if ( input == null || !input.Any() )
		//		return null;

		//	foreach ( var item in input )
		//	{
		//		var tlo = MapToEntityReference( item );
		//		if ( tlo != null && !string.IsNullOrWhiteSpace( tlo.Name ) )
		//		{
		//			output.Add( tlo );
		//		}
		//	}
		//	if ( !output.Any() )
		//		return null;

		//	return output;
		//}
		public static WMS.AJAXSettings MapRevocationProfile( string searchType, List<WMP.RevocationProfile> input )
		{
			if ( input == null || input.Count() == 0 )
				return null;
			//
			var output = new WMS.AJAXSettings()
			{
				//Type=null,
				Label = string.Format( "Has {0} Revocation Profile(s)", input.Count ),
				Total = input.Count
			};
			//
			var list = new List<WMA.RevocationProfile>();
			foreach ( var item in input )
			{
				var pp = new WMA.RevocationProfile()
				{
					//ProcessProfileType = item.ProcessType,
					Description = item.Description,
					DateEffective = item.DateEffective,
				};
				pp.RevocationCriteria = ServiceHelper.MapPropertyLabelLink( item.RevocationCriteriaUrl, "Revocation Criteria", item.RevocationCriteriaDescription );

				pp.Jurisdiction = MapJurisdiction( item.Jurisdiction );
				//pp.Region = MapJurisdiction( input.Region );
				list.Add( pp );
			}
			if ( list != null && list.Any() )
			{
				output.Label = string.Format( "Has Revocation Profile ({0})", list.Count );
				output.Total = list.Count;
				List<object> obj = list.Select( f => ( object )f ).ToList();
				output.Values = obj;
			}
			return output;
		}

		public static MC.TopLevelEntityReference MapToEntityReference( MC.TopLevelObject input, string entityType = "" )
		{

			if ( input == null || string.IsNullOrWhiteSpace( input.Name ) )
				return null;// new MC.TopLevelEntityReference();	//or NULL


			var output = new MC.TopLevelEntityReference()
			{
				Id = input.Id,//need for links, or may need to create link here
				Name = input.Name,
				SubjectWebpage = input.SubjectWebpage,
				Description = input.Description,
				CTID = input.CTID,
				EntityTypeId = input.EntityTypeId,
				Image = input.Image
			};
			if ( !string.IsNullOrWhiteSpace( entityType ) )
				output.DetailURL = reactFinderSiteURL + entityType + "/" + output.Id;
			else
			if ( !string.IsNullOrWhiteSpace( output.CTID ) )
				output.DetailURL = reactFinderSiteURL + "resources/" + output.CTID;

			return output;
		}
		public static WMS.AJAXSettings MapAssessmentToAJAXSettings( List<WMP.AssessmentProfile> input, string labelTemplate )
		{

			if ( input == null || !input.Any() )
				return null;
			try
			{
				var work = new List<WMA.Outline>();
				foreach ( var target in input )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						work.Add( MapToOutline( target, "assessment" ) );
				}
				//
				var output = new WMS.AJAXSettings()
				{
					//Type=null,
					//Label = string.Format( label, input.Count ),
					Label = input.Count > 0 ? labelTemplate.Replace( "{#}", input.Count.ToString() ).Replace( "(s)", input.Count == 1 ? "" : "s" ) : "",

					Total = input.Count
				};
				List<object> obj = work.Select( f => ( object )f ).ToList();
				output.Values = obj;
				return output;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "ServiceHelper.MapAssessmentToAJAXSettings" );
				return null;
			}

		}
		public static WMS.AJAXSettings MapCredentialToAJAXSettings( List<MC.Credential> input, string labelTemplate )
		{

			if ( input == null || !input.Any() )
				return null;
			try
			{
				var work = new List<WMA.Outline>();
				foreach ( var target in input )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						work.Add( MapToOutline( target, "credential" ) );
				}
				//
				var output = new WMS.AJAXSettings()
				{
					//Type=null,
					//Label = string.Format( label, input.Count ),
					Label = input.Count > 0 ? labelTemplate.Replace( "{#}", input.Count.ToString() ).Replace( "(s)", input.Count == 1 ? "" : "s" ) : "",

					Total = input.Count
				};
				List<object> obj = work.Select( f => ( object )f ).ToList();
				output.Values = obj;
				return output;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "ServiceHelper.MapCredentialToAJAXSettings" );
				return null;
			}

		}
		public static WMS.AJAXSettings MapLearningOppToAJAXSettings( List<WMP.LearningOpportunityProfile> input, string labelTemplate )
		{

			if ( input == null || !input.Any() )
				return null;
			try
			{
				var work = new List<WMA.Outline>();
				foreach ( var target in input )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						work.Add( MapToOutline( target, "LearningOpportunity" ) );
				}
				//var work = new List<MC.TopLevelEntityReference>();
				//foreach ( var target in input )
				//{
				//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
				//		work.Add( MapToEntityReference( target, "LearningOpportunity" ) );
				//}

				var output = new WMS.AJAXSettings()
				{
					//Type=null,
					//Label = string.Format( labelTemplate, input.Count ),
					Label = input.Count > 0 ? labelTemplate.Replace( "{#}", input.Count.ToString() ).Replace( "(ies)", input.Count == 1 ? "y" : "ies" ) : "",
					Total = input.Count
				};
				//				Label = input.Count > 0 ? labelTemplate.Replace( "{#}", input.Count.ToString() ).Replace( "{ies}", input.Count == 1 ? "y" : "ies" ) : ""

				List<object> obj = work.Select( f => ( object )f ).ToList();
				output.Values = obj;
				return output;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "ServiceHelper.MapLearningOppToAJAXSettings" );
				return null;
			}

		}
		//
		public static WMS.AJAXSettings MapCompetencyFrameworkToAJAXSettings( List<WMP.CompetencyFramework> input, string label )
		{

			if ( input == null || !input.Any() )
				return null;
			try
			{
				var work = new List<WMA.Outline>();
				foreach ( var target in input )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						work.Add( MapToOutline( target, "CompetencyFramework" ) );
				}
				var output = new WMS.AJAXSettings()
				{
					//Type=null,
					Label = string.Format( label, input.Count ),
					Total = input.Count
				};
				List<object> obj = work.Select( f => ( object )f ).ToList();
				output.Values = obj;
				return output;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "ServiceHelper.MapCompetencyFrameworkToAJAXSettings" );
				return null;
			}

		}

		public static WMS.AJAXSettings MapPathwayToAJAXSettings( List<MC.Pathway> input, string label )
		{

			if ( input == null || !input.Any() )
				return null;
			try
			{
				var work = new List<WMA.Outline>();
				foreach ( var target in input )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						work.Add( MapToOutline( target, "Pathway" ) );
				}
				var output = new WMS.AJAXSettings()
				{
					//Type=null,
					Label = string.Format( label, input.Count ),
					Total = input.Count
				};
				List<object> obj = work.Select( f => ( object )f ).ToList();
				output.Values = obj;
				return output;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "ServiceHelper.MapPathwayToAJAXSettings" );
				return null;
			}

		}


		public static WMS.AJAXSettings MapOutlineToAJAX( List<WMA.Outline> input, string label )
		{

			if ( input == null || !input.Any() )
				return null;
			try
			{
				var output = new WMS.AJAXSettings()
				{
					//Type=null,
					Label = string.Format( label, input.Count ),
					Total = input.Count
				};
				List<object> obj = input.Select( f => ( object )f ).ToList();
				output.Values = obj;
				return output;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "ServiceHelper.MapOutlineToAJAX" );
				return null;
			}

		}
		public static WMS.AJAXSettings MapOutlineToAJAX( WMA.Outline input, string label )
		{

			if ( input == null || string.IsNullOrWhiteSpace( input.Label ) )
				return null;
			try
			{
				var output = new WMS.AJAXSettings()
				{
					//Type=null,
					Label = string.Format( label, 1 ),
					Total = 1
				};
				object obj = ( object )input;
				output.Values = new List<object>() { obj };
				return output;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "ServiceHelper.MapOutlineToAJAX" );
				return null;
			}

		}
		public static WMA.Outline MapToOutline( MC.TopLevelObject input, string entityType = "" )
		{

			if ( input == null || string.IsNullOrWhiteSpace( input.Name ) )
				return null;// new MC.TopLevelEntityReference();	//or NULL


			var output = new WMA.Outline()
			{
				//Id = input.Id,//need for links, or may need to create link here
				Label = input.Name,
				//URL = input.SubjectWebpage,
				Description = input.Description,
				//CTID = input.CTID,
				//EntityTypeId = input.EntityTypeId,
			};
			output.Image = input.Image;
			//need to distinguish if a reference object and when to point externally
			//perhaps for all except organizations. May need a process to identify if there is enough to display for a reference
			//		a transfer value lopp might be an example
			//		or the presence of a description
			// && 
			if ( ( entityType ?? "" ).ToLower() == "organization" )
			{
				output.URL = reactFinderSiteURL + entityType + "/" + input.Id + ( !string.IsNullOrWhiteSpace( input.FriendlyName ) ? "/" + input.FriendlyName : "" ); 
			}
			else if ( ( entityType ?? "" ).ToLower() == "competencyframework" )
			{
				//may not have a description
				if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
					output.URL = reactFinderSiteURL + entityType + "/" + input.Id;
				else if ( !string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
					output.URL = input.SubjectWebpage;
			}
			else if ( !string.IsNullOrWhiteSpace( entityType ) && ( !input.IsReferenceEntity || !string.IsNullOrWhiteSpace( input.Description ) ) )
			{
				output.URL = reactFinderSiteURL + entityType + "/" + input.Id + ( !string.IsNullOrWhiteSpace( input.FriendlyName ) ? "/" + input.FriendlyName : "" );
			}
			else if ( !string.IsNullOrWhiteSpace( input.CTID ) )    //TODO - use entityTypeId or add entityType
				output.URL = reactFinderSiteURL + "resources/" + input.CTID;
			else if ( !string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
				output.URL = input.SubjectWebpage; // reactFinderSiteURL + "resources/" + input.CTID;

			if ( ( entityType ?? "" ).ToLower() == "credential" )
			{
				if ( input.GetType() == typeof( MC.Credential ) )
				{
					var et = ( MC.Credential )input;

					output.Tags.Add( ServiceHelper.MapPropertyLabelLink( et.CredentialTypeEnum, entityType ) );

					if ( et.AudienceType != null && et.AudienceType.Items.Any() )
					{
						output.Tags.AddRange( MapPropertyLabelLinks( et.AudienceType, entityType ) );
					}
					if ( et.AudienceLevelType != null && et.AudienceLevelType.Items.Any() )
					{
						output.Tags.AddRange( MapPropertyLabelLinks( et.AudienceLevelType, entityType ) );
					}
				}
			}
			if ( ( entityType ?? "" ).ToLower() == "assessment" )
			{
				if ( input.GetType() == typeof( WMP.AssessmentProfile ) )
				{
					var et = ( WMP.AssessmentProfile )input;
					if ( et.AudienceType != null && et.AudienceType.Items.Any() )
					{
						output.Tags.AddRange( MapPropertyLabelLinks( et.AudienceType, entityType ) );
					}
					if ( et.AudienceLevelType != null && et.AudienceLevelType.Items.Any() )
					{
						output.Tags.AddRange( MapPropertyLabelLinks( et.AudienceLevelType, entityType ) );
					}
				}
			}
			if ( ( entityType ?? "" ).ToLower() == "learningopportunity" )
			{
				if ( input.GetType() == typeof( WMP.LearningOpportunityProfile ) )
				{
					//21-08-23 mp - this doesn't work, cannot cast from TLO to lopp
					var et = ( WMP.LearningOpportunityProfile )input;
					if ( et.AudienceType != null && et.AudienceType.Items.Any() )
					{
						output.Tags.AddRange( MapPropertyLabelLinks( et.AudienceType, entityType ) );
					}
					if ( et.AudienceLevelType != null && et.AudienceLevelType.Items.Any() )
					{
						output.Tags.AddRange( MapPropertyLabelLinks( et.AudienceLevelType, entityType ) );
					}
				}
			}
			if ( input.OwningOrganizationId > 0 && !string.IsNullOrWhiteSpace( input.OrganizationName ) )
			{
				//should this be an Outline?
				output.Provider = new WMA.Outline()
				{
					Label = input.OrganizationName,
					Meta_Id = input.OwningOrganizationId,
					URL = reactFinderSiteURL + "organization/" + input.OwningOrganizationId + ( !string.IsNullOrWhiteSpace( input.OrganizationFriendlyName ) ? "/" + input.OrganizationFriendlyName : "" )
			};
			}
			return output;

		}
		public static List<WMA.Outline> MapToOutline( List<MC.PathwayComponent> list, string entityType = "" )
		{

			if ( list == null || !list.Any() )
				return null;
			var output = new List<WMA.Outline>();

			foreach ( var input in list )
			{
				var item = new WMA.Outline()
				{
					Label = input.Name,
					//URL = input.SourceData,
					Description = input.Description,
					//CTID = input.CTID,
					//EntityTypeId = input.EntityTypeId,
				};
				//NO URL for now
				//if ( !string.IsNullOrWhiteSpace( entityType ) && ( !input.IsReferenceEntity || !string.IsNullOrWhiteSpace( input.Description ) ) )
				//	item.URL = reactFinderSiteURL + entityType + "/" + input.Id;

				//else if ( !string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
				//	item.URL = input.SubjectWebpage; // reactFinderSiteURL + "resources/" + input.CTID;


				output.Add( item );
			}

			return output;

		}

		public static WMS.AJAXSettings MapOrganizationRoleProfileToAJAX( int currentOrgId, List<WMP.OrganizationRoleProfile> input, string label )
		{

			if ( input == null || !input.Any() )
				return null;
			try
			{
				//21-06-15
				//this should be mapped to an Outline
				var output = new List<WMA.Outline>();
				foreach ( var item in input )
				{
					var orp = new WMA.Outline()
					{
					};
					//dept/suborg will be participant
					if ( item.ParticipantAgent != null && item.ParticipantAgent.Id > 0 && item.ParticipantAgent.Id != currentOrgId )
					{
						orp.Label = item.ParticipantAgent.Name;
						orp.Description = item.ParticipantAgent.Description;
						orp.Meta_Id = item.ParticipantAgent.Id;
						orp.Image = item.ParticipantAgent.Image;

						if ( string.IsNullOrEmpty( item.ParticipantAgent.CTID ) )
						{
							//21-06-15 mparsons - reference orgs can have data. In this context (dept/sub) perhaps not
							orp.URL = item.ParticipantAgent.SubjectWebpage;
						}
						else
						{
							orp.URL = reactFinderSiteURL + string.Format( "organization/{0}", item.ParticipantAgent.Id, item.ParticipantAgent.FriendlyName );
						}
						output.Add( orp );
					}
					//parent org will be the acting org
					else if ( item.ActingAgent != null && item.ActingAgent.Id > 0 && item.ActingAgent.Id != currentOrgId )
					{
						orp.Label = item.ActingAgent.Name;
						orp.Description = item.ActingAgent.Description;
						orp.Meta_Id = item.ActingAgent.Id;
						orp.Image = item.ActingAgent.Image;

						if ( string.IsNullOrEmpty( item.ActingAgent.CTID ) )
						{
							//21-06-15 mparsons - reference orgs can have data. In this context (dept/sub) perhaps not
							orp.URL = item.ActingAgent.SubjectWebpage;
						}
						else
						{
							orp.URL = reactFinderSiteURL + string.Format( "organization/{0}/{1}", item.ActingAgent.Id, item.ActingAgent.FriendlyName );
						}
						output.Add( orp );
					}

				}
				if ( !output.Any() )
					return null;
				//skip relationship, as will have a heading
				//var work = ServiceHelper.MapOrganizationRoleProfileToOutline( input, 0 );
				var output2 = ServiceHelper.MapOutlineToAJAX( output, "" );
				return output2;
				//var output = new WMS.AJAXSettings()
				//{
				//	//Type=null,
				//	Label = string.Format( label, input.Count ),
				//	Total = input.Count
				//};
				//List<object> obj = list.Select( f => ( object )f ).ToList();
				//output.Values = obj;
				//return output;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "ServiceHelper.MapOrganizationRoleProfileToAJAX" );
				return null;
			}

		}
		public static MC.TopLevelEntityReference MapToEntityReference( WMP.OrganizationRoleProfile input )
		{

			if ( input == null || input.ActingAgent == null || string.IsNullOrWhiteSpace( input.ActingAgent.Name ) )
				return null;
			var output = new MC.TopLevelEntityReference();
			if ( input.ParticipantAgent != null && input.ParticipantAgent.Id > 0 )
			{
				output = new MC.TopLevelEntityReference()
				{
					Id = input.Id,//need for links, or may need to create link here
					Name = input.ParticipantAgent.Name,
					SubjectWebpage = input.ParticipantAgent.SubjectWebpage,
					Description = input.ParticipantAgent.Description,
					CTID = input.ParticipantAgent.CTID,
					EntityTypeId = 2,
					Image = input.ParticipantAgent.Image

				};
			}
			else
			{
				//OR
				output = new MC.TopLevelEntityReference()
				{
					Id = input.Id,//need for links, or may need to create link here
					Name = input.ActingAgent.Name,
					SubjectWebpage = input.ActingAgent.SubjectWebpage,
					Description = input.ActingAgent.Description,
					CTID = input.ActingAgent.CTID,
					EntityTypeId = 2,
					Image = input.ActingAgent.Image

				};
			}
			//actually reference orgs can have detail pages
			if ( !string.IsNullOrWhiteSpace( output.CTID ) && output.Id > 0 )
				output.DetailURL = reactFinderSiteURL + "organization/" + output.Id;
			else
				output.DetailURL = input.ActingAgent.SubjectWebpage;
			//
			return output;

		}


		public static List<WMA.QuantitativeValue> MapQuantitativeValue( List<MC.QuantitativeValue> input, bool isCurrencyProperty = false )
		{
			var output = new List<WMA.QuantitativeValue>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				var qv = new WMA.QuantitativeValue()
				{
					Label = item.Label,
					Description = item.Description
				};
				if ( item.Value != 0 )
					qv.Value = item.Value;
				if ( item.MinValue != 0 )
					qv.MinValue = item.MinValue;
				if ( item.MaxValue != 0 )
					qv.MaxValue = item.MaxValue;
				if ( item.Percentage != 0 )
					qv.Percentage = item.Percentage;

				if ( isCurrencyProperty )
				{
					if ( !string.IsNullOrWhiteSpace( item.UnitText ) )
					{
						var code = CodesManager.GetCurrencyItem( qv.UnitText );
						if ( code != null && code.NumericCode > 0 )
						{							
							qv.UnitText = code.Currency;
							if ( code.Currency == "USD" )
								qv.CurrencySymbol = "$";
							else
								qv.CurrencySymbol = code.HtmlCodes;
						}
					}
				} else
				{
					qv.UnitText = !string.IsNullOrWhiteSpace( item.UnitText ) ? item.UnitText : string.Join( ",", item.CreditUnitType.Items.ToArray().Select( m => m.Name ) );

				}

				output.Add( qv );
			}

			return output;
		}


		//==========================================
		//

		public static List<WMA.ConditionManifest> MapConditionManifests( List<MC.ConditionManifest> input, string searchType )
		{
			if ( input == null || !input.Any() )
			{
				return null;
			}
			var output = new List<WMA.ConditionManifest>();
			var displayAdditionalInfo = UtilityManager.GetAppKeyValue( "DisplayAdditionalInformationForManifests", false );

			foreach ( var item in input )
			{
				//just in case
				if ( string.IsNullOrWhiteSpace( item.Name ) )
					continue;
				var cm = new WMA.ConditionManifest()
				{
					Name = item.Name,
					Description = item.Description,
					SubjectWebpage = item.SubjectWebpage,
					CTID = item.CTID,
					EntityLastUpdated = item.LastUpdated,
					//Meta_LastUpdated = item.LastUpdated
					CredentialRegistryURL = RegistryServices.GetResourceUrl( item.CTID ),
					DisplayAdditionalInformation = displayAdditionalInfo,
					RegistryData = ServiceHelper.FillRegistryData( item.CTID, "Condition Manifest" )

				};
				//condition profiles
				cm.Corequisite = ServiceHelper.MapToConditionProfiles( item.Corequisite, searchType );
				cm.EntryCondition = ServiceHelper.MapToConditionProfiles( item.EntryCondition, searchType );
				cm.Recommends = ServiceHelper.MapToConditionProfiles( item.Recommends, searchType );
				cm.Renewal = ServiceHelper.MapToConditionProfiles( item.Renewal, searchType );
				cm.Requires = ServiceHelper.MapToConditionProfiles( item.Requires, searchType );
				output.Add( cm );
			}

			if ( !output.Any() )
				return null;

			return output;
		}
		public static List<WMA.CostManifest> MapCostManifests( List<MC.CostManifest> input, string searchType )
		{

			if ( input == null || !input.Any() )
			{
				return null;
			}
			var displayAdditionalInfo= UtilityManager.GetAppKeyValue( "DisplayAdditionalInformationForManifests", false );
			var output = new List<WMA.CostManifest>();
			foreach ( var item in input )
			{
				//just in case
				if ( string.IsNullOrWhiteSpace( item.CostDetails ) )
					continue;
				var cm = new WMA.CostManifest()
				{
					Name = item.Name,
					Description = item.Description,
					CostDetails = item.CostDetails,
					StartDate = item.StartDate,
					EndDate = item.EndDate,
					CTID = item.CTID,
					EntityLastUpdated = item.LastUpdated,
					CredentialRegistryURL = RegistryServices.GetResourceUrl( item.CTID ),
					DisplayAdditionalInformation= displayAdditionalInfo,
					RegistryData = ServiceHelper.FillRegistryData( item.CTID, "Cost Manifest" )
				};
				//CostProfiles
				if ( item.EstimatedCost != null && item.EstimatedCost.Any() )
				{
					cm.EstimatedCost = ServiceHelper.MapCostProfiles( item.EstimatedCost, searchType );
				}
				//hide N/A inherited properties
				cm.InLanguage = null;
				cm.Meta_Language = null;
				cm.Meta_StateId = null;
				output.Add( cm );
			}
			if ( !output.Any() )
				return null;

			return output;

		}
		public static List<ME.CostProfile> MapCostProfiles( List<WMP.CostProfile> input, string searchType )
		{
			var output = new List<ME.CostProfile>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				var tlo = MapCostProfile( item, searchType );
				if ( tlo != null && !string.IsNullOrWhiteSpace( tlo.Description ) )
				{
					output.Add( tlo );
				}
			}
			if ( !output.Any() )
				return null;

			return output;
		}
		public static ME.CostProfile MapCostProfile( WMP.CostProfile input, string searchType )
		{

			if ( input == null || string.IsNullOrWhiteSpace( input.Description ) )
				return null;

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

			if ( input.Items != null && input.Items.Any() )
			{
				foreach ( var item in input.Items )
				{
					var cpi = new ME.CostProfileItem()
					{
						Price = item.Price,
						PaymentPattern = item.PaymentPattern,
						AudienceType = MapPropertyLabelLinks( item.AudienceType, searchType, false ),
						ResidencyType = MapPropertyLabelLinks( item.ResidencyType, searchType, false ),
					};
					cpi.DirectCostType = MapPropertyLabelLink( item.DirectCostType, searchType, false );

					output.CostItems.Add( cpi );
				}
			}

			return output;

		}

		public static List<WMA.ConditionProfile> MapToConditionProfiles( List<WMP.ConditionProfile> input, string searchType = "" )
		{
			var output = new List<WMA.ConditionProfile>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				var tlo = MapToConditionProfile( item, searchType );
				if ( tlo != null && ( !string.IsNullOrWhiteSpace( tlo.Name ) || !string.IsNullOrWhiteSpace( tlo.Description ) ))
				{
					output.Add( tlo );
				}
			}
			if ( !output.Any() )
				return null;

			return output;
		}
		public static WMA.ConditionProfile MapToConditionProfile( WMP.ConditionProfile input, string searchType )
		{

			if ( input == null || ( string.IsNullOrWhiteSpace(input.Name) && string.IsNullOrWhiteSpace( input.Description ) ))
				return null;

			var output = new WMA.ConditionProfile()
			{
				Meta_ProfileType = !string.IsNullOrWhiteSpace(input.ConnectionProfileType) ? input.ConnectionProfileType : input.ConnectionProfileTypeId.ToString(),
				Meta_Id = input.Id,
				Name = input.ProfileName,
				SubjectWebpage = input.SubjectWebpage,
				Description = input.Description,
				Experience = input.Experience,
				AudienceLevelType = MapPropertyLabelLinks( input.AudienceLevelType, searchType, false ),
				AudienceType = MapPropertyLabelLinks( input.AudienceType, searchType, false ),
				CreditUnitTypeDescription = input.CreditUnitTypeDescription,
				SubmissionOfDescription = input.SubmissionOfDescription,
			};
			if ( !string.IsNullOrWhiteSpace( input.DateEffective ) )
				output.DateEffective = input.DateEffective;
			//only return if > 0
			if ( input.MinimumAge > 0 )
				output.MinimumAge = input.MinimumAge;
			if ( input.Weight > 0 )
				output.Weight = input.Weight;
			if ( input.YearsOfExperience > 0 )
				output.YearsOfExperience = input.YearsOfExperience;

			//
			if ( input.AssertedBy != null && !string.IsNullOrWhiteSpace( input.AssertedBy.Name ) )
			{
				var ab = ServiceHelper.MapToOutline( input.AssertedBy, "organization" );
				output.AssertedBy = ServiceHelper.MapOutlineToAJAX( ab, "Asserted by {0} Organization(s)" );
			}
			//
			output.Condition = MapTextValueProfileTextValue( input.Condition );
			//CreditValue
			output.CreditValue = ServiceHelper.MapValueProfile( input.CreditValueList, searchType );
			//
			if ( input.AlternativeCondition != null && input.AlternativeCondition.Any() )
			{
				output.AlternativeCondition = ServiceHelper.MapToConditionProfiles( input.AlternativeCondition, searchType );
			}

			output.Jurisdiction = MapJurisdiction( input.Jurisdiction );
			output.ResidentOf = MapJurisdiction( input.ResidentOf );
			output.SubmissionOf = MapTextValueProfileTextValue( input.SubmissionOf );

			//CommonCosts
			output.CommonCosts = MapCostManifests( input.CommonCosts, searchType );
			//EstimatedCosts
			output.EstimatedCost = MapCostProfiles( input.EstimatedCost, searchType );

			//targets
			if ( input.TargetAssessment != null && input.TargetAssessment.Any() )
			{
				output.TargetAssessment = MapAssessmentToAJAXSettings( input.TargetAssessment, "Has {0} Target Assessment(s)" );
				//foreach ( var target in input.TargetAssessment )
				//{
				//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
				//		output.TargetAssessment.Add( MapToEntityReference( target, "assessment" ) );
				//}
			}
			if ( input.TargetCredential != null && input.TargetCredential.Any() )
			{
				output.TargetCredential = MapCredentialToAJAXSettings( input.TargetCredential, "Has {0} Target Credential(s)" );
				///
				//output.TargetCredential = new WMS.AJAXSettings();
				//var work = new List<MC.TopLevelEntityReference>();
				//foreach ( var target in input.TargetCredential )
				//{
				//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
				//		work.Add( MapToEntityReference( target, "credential" ) );
				//}
			}
			if ( input.TargetLearningOpportunity != null && input.TargetLearningOpportunity.Any() )
			{
				output.TargetLearningOpportunity = MapLearningOppToAJAXSettings( input.TargetLearningOpportunity, "Has {0} Target Learning Opportunity(ies)" );
				//foreach ( var target in input.TargetLearningOpportunity )
				//{
				//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
				//		output.TargetLearningOpportunity.Add( MapToEntityReference( target, "learningopportunity" ) );
				//}
			}

			//21-09-21 mp - TargetCompetencies was only being used by the import, use RequiresCompetenciesFrameworks
			if ( input.RequiresCompetenciesFrameworks != null && input.RequiresCompetenciesFrameworks.Any() )
			{
				output.TargetCompetency = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Requires {#} Competenc{ies}", input.RequiresCompetenciesFrameworks );
			}
			
			return output;

		}

		//
		public static List<WMA.ConditionProfile> AppendConditions( List<WMA.ConditionProfile> input, List<WMA.ConditionProfile> existing )
		{
			if ( input != null && input.Any() )
			{
				if ( existing == null )
					existing = new List<WMA.ConditionProfile>();
				existing.AddRange( input );
			}

			return existing;
		}
		//=================
		#region QDATA
		public static List<WMA.AggregateDataProfile> MapToAggregateDataProfile( List<MC.AggregateDataProfile> input, string searchType = "" )
		{
			var output = new List<WMA.AggregateDataProfile>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				var tlo = MapToAggregateDataProfile( item, searchType );
				if ( tlo != null && !string.IsNullOrWhiteSpace( tlo.Description ) )
				{
					output.Add( tlo );
				}
			}
			if ( !output.Any() )
				return null;

			return output;
		}
		public static WMA.AggregateDataProfile MapToAggregateDataProfile( MC.AggregateDataProfile input, string searchType )
		{

			if ( input == null || string.IsNullOrWhiteSpace( input.Description ) )
				return null;

			var output = new WMA.AggregateDataProfile()
			{
				Name = input.Name,
				Description = input.Description,
				DemographicInformation = input.DemographicInformation,
				Source = input.Source,
				Currency = input.Currency,
				CurrencySymbol = input.CurrencySymbol
			};
			if ( !string.IsNullOrWhiteSpace( input.DateEffective ) )
				output.DateEffective = input.DateEffective;
			if ( !string.IsNullOrWhiteSpace( input.ExpirationDate ) )
				output.ExpirationDate = input.ExpirationDate;

			//only return if > 0
			if ( input.HighEarnings > 0 )
				output.HighEarnings = input.HighEarnings;
			if ( input.LowEarnings > 0 )
				output.LowEarnings = input.LowEarnings;
			if ( input.MedianEarnings > 0 )
				output.MedianEarnings = input.MedianEarnings;
			if ( input.NumberAwarded > 0 )
				output.NumberAwarded = input.NumberAwarded;
			if ( input.PostReceiptMonths > 0 )
				output.PostReceiptMonths = input.PostReceiptMonths;
			//job obtained
			output.JobsObtainedList = MapQuantitativeValue( input.JobsObtained, false );
			//
			output.Jurisdiction = MapJurisdiction( input.Jurisdiction );

			//datasets
			output.RelevantDataSet = MapToDatasetProfile( input.RelevantDataSet, searchType );
			return output;
		}
		public static List<WMA.DataSetProfile> MapToDatasetProfile( List<MQD.DataSetProfile> list, string searchType )
		{

			if ( list == null || !list.Any() )
				return null;

			var output = new List<WMA.DataSetProfile>();
			foreach ( var input in list )
			{
				var profile = new WMA.DataSetProfile()
				{
					CTID = input.CTID,
					EntityLastUpdated = input.LastUpdated,
					Description = input.Description,
					Name = input.Name,
					Source = input.Source,
					DataSuppressionPolicy = input.DataSuppressionPolicy,
					SubjectIdentification = input.SubjectIdentification,
					CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
					RegistryData=null
					//RegistryData = ServiceHelper.FillRegistryData( input.CTID )
				};
				if ( input.DistributionFile != null && input.DistributionFile.Any() )
				{
					profile.DistributionFile = input.DistributionFile;
				}
				//dataProvider
				if ( input.DataProvider != null && !string.IsNullOrWhiteSpace( input.DataProvider.Label ) )
					profile.DataProvider = input.DataProvider;
				//
				profile.Jurisdiction = MapJurisdiction( input.Jurisdiction );
				//profile.InstructionalProgramType = ServiceHelper.MapReferenceFrameworkLabelLink( input.InstructionalProgramType, searchType, CodesManager.PROPERTY_CATEGORY_CIP );
				profile.InstructionalProgramType = ServiceHelper.MapReferenceFramework( input.InstructionalProgramTypes, searchType, CodesManager.PROPERTY_CATEGORY_CIP );

				//about
				if ( input.About != null && input.About.Any() )
				{
					profile.About = input.About;
				}

				//admin ProcessProfile
				if ( input.AdministrationProcess.Any() )
				{
					profile.AdministrationProcess = ServiceHelper.MapAJAXProcessProfile( "Administration Process", "", input.AdministrationProcess );
				}

				//dataSetTimePeriod
				profile.DataSetTimePeriod = MapToDataSetTimeFrame( input.DataSetTimePeriod, searchType );

				output.Add( profile );
			}

			return output;

		}

		public static List<WMA.DataSetTimeFrame> MapToDataSetTimeFrame( List<MQD.DataSetTimeFrame> list, string searchType )
		{

			if ( list == null || !list.Any() )
				return null;

			var output = new List<WMA.DataSetTimeFrame>();
			foreach ( var input in list )
			{
				var profile = new WMA.DataSetTimeFrame()
				{
					Description = input.Description,
					Name = input.Name,
					StartDate = input.StartDate,
					EndDate = input.EndDate,
				};
				//datasource coverage
				profile.DataSourceCoverageType = ServiceHelper.MapPropertyLabelLinks( input.DataSourceCoverageType, searchType );
				profile.DataAttributes = MapToDataProfile( input.DataAttributes, searchType );
				//
				output.Add( profile );
			}
	
			return output;

		}


		public static List<WMA.DataProfile> MapToDataProfile( List<MQD.DataProfile> list, string searchType )
		{

			if ( list == null || !list.Any() )
				return null;

			var output = new List<WMA.DataProfile>();
			foreach ( var input in list )
			{
				if ( input == null )
					continue;

				var profile = new WMA.DataProfile()
				{
					Description = input.Description,

				};
				profile.AdministrativeRecordType = ServiceHelper.MapPropertyLabelLinks( input.AdministrativeRecordType, searchType );

				//from DataProfileAttributes
				//if ( input.DataProfileAttributes != null )
				//{
				//}
				profile.Adjustment = input.DataProfileAttributes.Adjustment;
				//EarningsAmount
				profile.EarningsAmount = null;
				if (input.DataProfileAttributes.EarningsAmount != null && input.DataProfileAttributes.EarningsAmount.Any())
				{
					profile.EarningsAmount = new List<WMA.MonetaryAmount>();
					foreach(var item in input.DataProfileAttributes.EarningsAmount )
					{
						var ea = new WMA.MonetaryAmount();
						if ( item == null || !item.HasData() )
							continue;
						//
						ea.Currency = item.Currency;
						//==> CurrencySymbol assigned in import now
						if ( !string.IsNullOrWhiteSpace( item.Currency ) )
						{
							if ( item.Currency == "USD" )
								ea.CurrencySymbol = "$";
						}
						
						ea.Description = item.Description;
						ea.UnitText = item.UnitText;
						//will zeroes be allowed?
						if ( item.Value > 0 )
							ea.Value = item.Value;
						if ( item.MaxValue > 0 )
						{
							ea.MaxValue = item.MaxValue;
							ea.MinValue = item.MinValue;
						}
						profile.EarningsAmount.Add( ea );
					}
				}
				//
				profile.EarningsDefinition = input.DataProfileAttributes.EarningsDefinition;
				//EarningsDistribution
				profile.EarningsDistribution = null;
				if ( input.DataProfileAttributes.EarningsDistribution != null && input.DataProfileAttributes.EarningsDistribution.Any() )
				{
					profile.EarningsDistribution = new List<WMA.MonetaryAmountDistribution>();
					foreach ( var item in input.DataProfileAttributes.EarningsDistribution )
					{
						var ea = new WMA.MonetaryAmountDistribution();
						if ( item == null || !item.HasData() )
							continue;
						//
						ea.Currency = item.Currency;
						//==> CurrencySymbol assigned in import now
						if ( item.Currency == "USD" )
							ea.CurrencySymbol = "$";
						//will zeroes be allowed?
						if ( item.Median > 0 )
							ea.Median = item.Median;
						if ( item.Percentile10 > 0 )
							ea.Percentile10 = item.Percentile10;
						if ( item.Percentile25 > 0 )
							ea.Percentile25 = item.Percentile25;
						if ( item.Percentile75 > 0 )
							ea.Percentile75 = item.Percentile75;
						if ( item.Percentile90 > 0 )
							ea.Percentile90 = item.Percentile90;

						profile.EarningsDistribution.Add( ea );
					}
				}
				//
				profile.EarningsThreshold = input.DataProfileAttributes.EarningsThreshold;
				profile.EmploymentDefinition = input.DataProfileAttributes.EmploymentDefinition;

				profile.IncomeDeterminationType = ServiceHelper.MapPropertyLabelLinks( input.IncomeDeterminationType, searchType );
				profile.WorkTimeThreshold = input.DataProfileAttributes.WorkTimeThreshold;

				//QV
				profile.DataAvailable = MapQuantitativeValue( input.DataProfileAttributes.DataAvailable, false );
				profile.DataNotAvailable = MapQuantitativeValue( input.DataProfileAttributes.DataNotAvailable, false );

				profile.DemographicEarningsRate = MapQuantitativeValue( input.DataProfileAttributes.DemographicEarningsRate, true );

				profile.DemographicEmploymentRate = MapQuantitativeValue( input.DataProfileAttributes.DemographicEmploymentRate, true );

				profile.EmploymentRate = MapQuantitativeValue( input.DataProfileAttributes.EmploymentRate, true );
				profile.HoldersInSet = MapQuantitativeValue( input.DataProfileAttributes.HoldersInSet, false );
				profile.IndustryRate = MapQuantitativeValue( input.DataProfileAttributes.IndustryRate, false );

				profile.InsufficientEmploymentCriteria = MapQuantitativeValue( input.DataProfileAttributes.InsufficientEmploymentCriteria, false );
				profile.MeetEmploymentCriteria = MapQuantitativeValue( input.DataProfileAttributes.MeetEmploymentCriteria, false );
				//---------
				profile.NonCompleters = MapQuantitativeValue( input.DataProfileAttributes.NonCompleters, false );
				profile.NonHoldersInSet = MapQuantitativeValue( input.DataProfileAttributes.NonHoldersInSet, false );
				profile.OccupationRate = MapQuantitativeValue( input.DataProfileAttributes.OccupationRate, false );

				profile.PassRate = MapQuantitativeValue( input.DataProfileAttributes.PassRate, false );
				profile.RegionalEarningsDistribution = MapQuantitativeValue( input.DataProfileAttributes.RegionalEarningsDistribution, true );
				//---------
				profile.RegionalEmploymentRate = MapQuantitativeValue( input.DataProfileAttributes.RegionalEmploymentRate, true );
				profile.RelatedEmployment = MapQuantitativeValue( input.DataProfileAttributes.RelatedEmployment, false );
				profile.SubjectExcluded = MapQuantitativeValue( input.DataProfileAttributes.SubjectExcluded, false );

				profile.SubjectsInSet = MapQuantitativeValue( input.DataProfileAttributes.SubjectsInSet, false );
				profile.SufficientEmploymentCriteria = MapQuantitativeValue( input.DataProfileAttributes.SufficientEmploymentCriteria, false );

				//
				profile.UnrelatedEmployment = MapQuantitativeValue( input.DataProfileAttributes.UnrelatedEmployment, false );
				profile.TotalWIOACompleters = MapQuantitativeValue( input.DataProfileAttributes.TotalWIOACompleters, false );

				profile.TotalWIOAParticipants = MapQuantitativeValue( input.DataProfileAttributes.TotalWIOAParticipants, false );
				profile.TotalWIOAExiters = MapQuantitativeValue( input.DataProfileAttributes.TotalWIOAExiters, false );

				output.Add( profile );
			}
			return output;

		}
		#endregion
		//
		/// <summary>
		/// Get duration profiles and wrap in a ??????
		/// There could be multiple, say lopps, and can't really combine them?
		/// </summary>
		/// <param name="input"></param>
		/// <param name="label"></param>
		public static void GetAllDurations( List<MC.IBaseObject> input, string label )
		{
			if ( input == null || !input.Any() )
				return;
			var list = new List<WMA.DurationProfile>();
			foreach ( var item in input )
			{
				if ( item.EstimatedDuration == null || !item.EstimatedDuration.Any() )
					continue;

				var edlist = MapDurationProfiles( item.EstimatedDuration );
				if ( edlist != null && edlist.Any() )
				{
					list.AddRange( edlist );
				}
					 
			}
			var output = new WMS.AJAXSettings();

			if ( list != null && list.Any() )
			{
				output.Label = label;
				output.Total = list.Count;
				List<object> obj = list.Select( f => ( object )f ).ToList();
				output.Values = obj;
			}
		}
		public static WMS.AJAXSettings GetAllDurations( List<MC.Credential> input, string label )
		{
			if ( input == null || !input.Any() )
				return null;
			var outputList = new List<WMA.CredentialDetail>();
			foreach ( var item in input )
			{
				if ( item.EstimatedDuration == null || !item.EstimatedDuration.Any() )
					continue;

				var edlist = MapDurationProfiles( item.EstimatedDuration );
				if ( edlist != null && edlist.Any() )
				{
					outputList.Add( new WMA.CredentialDetail()
					{
						Name = item.Name,
						Description = item.Description,
						CTID = item.CTID,
						URL = reactFinderSiteURL + string.Format( "{0}/{1}/{2}", "Credential", item.Id, item.FriendlyName ),
						EstimatedDuration = edlist,
						//clear out stuff to not show
						RegistryData = null,
						EntityLastUpdated = item.EntityLastUpdated
						//oldCredentialFinderSite + string.Format( "{0}/{1}/{2}", "Assessment", item.Id, item.FriendlyName )
						//Values = edlist.Select( f => ( object )f ).ToList()
					} );
				}
			}
			var output = new WMS.AJAXSettings();

			if ( outputList != null && outputList.Any() )
			{
				output.Label = "Related Credentials Duration(s)";
				output.Total = outputList.Count;
				List<object> obj = outputList.Select( f => ( object )f ).ToList();
				output.Values = obj;
			}
			return output;
		}
		/// <summary>
		/// Speciality method to get duration profiles for assessments
		/// 21-07-12 This should return AjaxSettings with a list of Assessment Outlines, not AjaxSettings
		/// </summary>
		/// <param name="input"></param>
		/// <param name="label"></param>
		/// <returns></returns>
		public static WMS.AJAXSettings GetAllDurations( List<WMP.AssessmentProfile> input, string label )
		{
			if ( input == null || !input.Any() )
				return null;
			var list = new List<WMA.DurationProfile>();

			var outputListOld = new List<WMS.AJAXSettings>();
			//var outputList2 = new List<WMA.Outline>();
			var outputList = new List<WMA.AssessmentDetail>();
			foreach ( var item in input )
			{
				if ( item.EstimatedDuration == null || !item.EstimatedDuration.Any() )
					continue;

				var edlist = MapDurationProfiles( item.EstimatedDuration );
				if ( edlist != null && edlist.Any() )
				{
					list.AddRange( edlist );
					//outputListOld.Add( new WMS.AJAXSettings()
					//{
					//	Label = item.Name,
					//	URL = oldCredentialFinderSite + string.Format( "{0}/{1}/{2}", "Assessment", item.Id, item.FriendlyName ),
					//	Values = edlist.Select( f => ( object )f ).ToList()
					//} );

					//or outline????
					//doesn't make sense. Don't have Values for the duration profiles
					//outputList2.Add( new WMA.Outline()
					//{
					//	Label = item.Name,
					//	URL = oldCredentialFinderSite + string.Format( "{0}/{1}/{2}", "Assessment", item.Id, item.FriendlyName ),
					//	//Values = edlist.Select( f => ( object )f ).ToList()
					//} );
					//or asmt????
					//doesn't make sense. Don't have Values for the duration profiles
					outputList.Add( new WMA.AssessmentDetail()
					{
						Name = item.Name,
						Description = item.Description,
						CTID = item.CTID,
						URL = reactFinderSiteURL + string.Format( "{0}/{1}/{2}", "Assessment", item.Id, item.FriendlyName ),						
						EstimatedDuration = edlist,
						//clear out stuff to not show
						RegistryData=null,
						EntityLastUpdated =item.EntityLastUpdated
						//oldCredentialFinderSite + string.Format( "{0}/{1}/{2}", "Assessment", item.Id, item.FriendlyName )
						//Values = edlist.Select( f => ( object )f ).ToList()
					} ); ;
					//			output.AssessmentExample = ServiceHelper.MapPropertyLabelLink( input.AssessmentExample, "Example Data", input.AssessmentExampleDescription );

				}
			}
			var output = new WMS.AJAXSettings();

			if ( outputList != null && outputList.Any() )
			{
				output.Label = "Related Assessments Duration(s)";
				output.Total = outputList.Count;
				List<object> obj = outputList.Select( f => ( object )f ).ToList();
				output.Values = obj;
				return output;
			} else
				return null;
			
		}
		public static WMS.AJAXSettings GetAllDurations( List<WMP.LearningOpportunityProfile> input, string label )
		{
			if ( input == null || !input.Any() )
				return null;

			var outputList = new List<WMA.LearningOpportunityDetail>();
			var outputListOld = new List<WMS.AJAXSettings>();
			foreach ( var item in input )
			{
				if ( item.EstimatedDuration == null || !item.EstimatedDuration.Any() )
					continue;

				var edlist = MapDurationProfiles( item.EstimatedDuration );
				if ( edlist != null && edlist.Any() )
				{
					//outputListOld.Add( new WMS.AJAXSettings()
					//{
					//	Label = item.Name,
					//	URL = oldCredentialFinderSite + string.Format( "{0}/{1}/{2}", "LearningOpportunity", item.Id, item.FriendlyName ),
					//	Total = edlist.Count(),
					//	Values = edlist.Select( f => ( object )f ).ToList()
					//} );
					outputList.Add( new WMA.LearningOpportunityDetail()
					{
						Name = item.Name,
						Description = item.Description,
						CTID = item.CTID,
						URL = reactFinderSiteURL + string.Format( "{0}/{1}/{2}", "LearningOpportunity", item.Id, item.FriendlyName ),
						EstimatedDuration = edlist,
						//clear out stuff to not show
						RegistryData = null,
						EntityLastUpdated = item.EntityLastUpdated
						//oldCredentialFinderSite + string.Format( "{0}/{1}/{2}", "Assessment", item.Id, item.FriendlyName )
						//Values = edlist.Select( f => ( object )f ).ToList()
					} ); 
				}
			}
			var output = new WMS.AJAXSettings();

			if ( outputList != null && outputList.Any() )
			{
				output.Label = "Related Learning Opportunities Duration(s)";
				output.Total = outputList.Count;
				List<object> obj = outputList.Select( f => ( object )f ).ToList();
				output.Values = obj;
				return output;
			}
			else
				return null;
		}
		public static List<WMA.DurationProfile> MapDurationProfiles( List<WMP.DurationProfile> input )
		{
			var output = new List<WMA.DurationProfile>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				var tlo = MapDurationProfile( item );
				if ( tlo != null && 
					( !string.IsNullOrWhiteSpace( item.DurationSummary ) || !string.IsNullOrWhiteSpace( item.Description ) ) 
				)
				{
					output.Add( tlo );
				}
			}
			if ( !output.Any() )
				return null;

			return output;
		}
		public static WMA.DurationProfile MapDurationProfile( WMP.DurationProfile input )
		{
			var output = new WMA.DurationProfile();
			if ( input == null || !input.HasData )
				return null;
			output.Description = input.Description;
			if ( input.IsRange )
			{
				output.DurationSummary = input.MinimumDuration.Print() + " - " + input.MaximumDuration.Print();
			}
			else
			{
				output.DurationSummary = input.ExactDuration.Print();
			}

			if ( string.IsNullOrWhiteSpace( output.DurationSummary) && string.IsNullOrWhiteSpace( output.Description ) )
				return null;

			return output;
		}
		public static string MapDurationItem( WMP.DurationItem input )
		{
			if ( input == null || !input.HasValue )
				return null;

			return input.Print();
		}
		public static List<WMA.FinancialAssistanceProfile> MapFinancialAssistanceProfiles( List<MC.FinancialAssistanceProfile> input, string searchType )
		{
			var output = new List<WMA.FinancialAssistanceProfile>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				var fap = new WMA.FinancialAssistanceProfile()
				{
					Name = item.Name,
					SubjectWebpage = item.SubjectWebpage,
					Description = item.Description,
				};
				if (fap.Name == "Other(Specify)" && !string.IsNullOrWhiteSpace(fap.Description))
                {
					fap.Name = fap.Description;
					fap.Description = "";
                }
				fap.FinancialAssistanceType = ServiceHelper.MapPropertyLabelLinks( item.FinancialAssistanceType, searchType, false );

				//provide QV as well in case needed
				if ( item.FinancialAssistanceValue != null && item.FinancialAssistanceValue.Any() )
				{
					fap.FinancialAssistanceValue = new List<string>();
					fap.FinancialAssistanceValue2 = new List<WMA.QuantitativeValue>();

					foreach ( var fat in item.FinancialAssistanceValueSummary )
					{
						fap.FinancialAssistanceValue.Add( fat );
					}
					fap.FinancialAssistanceValue2 = MapQuantitativeValue( item.FinancialAssistanceValue, true );
					//foreach ( var fvi in item.FinancialAssistanceValue )
					//{
					//	var fv = new WMA.QuantitativeValue()
					//	{
					//	};
					//}
				}
				//
				if ( fap != null && (!string.IsNullOrWhiteSpace( fap.Name ) || !string.IsNullOrWhiteSpace( fap.Description )) )
				{
					output.Add( fap );
				}
			}
			if ( !output.Any() )
				return null;

			return output;
		}
		public static List<string> MapTextValueProfileTextValue( List<WMP.TextValueProfile> input )
		{
			var output = new List<string>();
			if ( input == null || !input.Any() )
				return null;
			foreach ( var item in input )
			{
				if ( !string.IsNullOrWhiteSpace( item.TextValue ) )
					output.Add( item.TextValue );
			}
			if ( !output.Any() )
				return null;

			return output;
		}

		#region Methods for merged requirements and competencies
		//
		/// <summary>
		/// Get all children for one of credential, assessment or lopp.
		/// </summary>
		/// <param name="requirements"></param>
		/// <param name="recommendations"></param>
		/// <param name="connections"></param>
		/// <param name="credential"></param>
		/// <param name="assessment"></param>
		/// <param name="learningOpportunity"></param>
		public static void GetAllChildren( MergedConditions requirements, MergedConditions recommendations, ConnectionData connections, 
					MC.Credential credential, WMP.AssessmentProfile assessment, WMP.LearningOpportunityProfile learningOpportunity )
		{
			credential = credential ?? new MC.Credential();
			assessment = assessment ?? new WMP.AssessmentProfile();
			learningOpportunity = learningOpportunity ?? new WMP.LearningOpportunityProfile();

			//Recursive bubbling - wish this could be simpler
			var allRequiredAssessments = new List<WMP.AssessmentProfile>() { assessment }
					.Concat( credential.Requires.SelectMany( m => m.TargetAssessment ) )
					.Concat( assessment.Requires.SelectMany( m => m.TargetAssessment ) )
					.Concat( learningOpportunity.Requires.SelectMany( m => m.TargetAssessment ) )
					.ToList();
			var allRequiredLearningOpps = new List<WMP.LearningOpportunityProfile>() { learningOpportunity }
					.Concat( credential.Requires.SelectMany( m => m.TargetLearningOpportunity ) )
					.Concat( assessment.Requires.SelectMany( m => m.TargetLearningOpportunity ) )
					.ToList();
			var gatheredLearningOpps = new List<WMP.LearningOpportunityProfile>();
			GetChildLearningOpps( allRequiredLearningOpps, gatheredLearningOpps );
			var allRequiredCredentials = new List<MC.Credential>() //{ credential } //Don't use this here instead of the GetChild method, it doesn't work
					.Concat( assessment.Requires.SelectMany( m => m.TargetCredential ) )
					.Concat( learningOpportunity.Requires.SelectMany( m => m.TargetCredential ) )
					.ToList();
			GetChildCredentials( new List<MC.Credential>() { credential }, allRequiredCredentials, allRequiredAssessments, gatheredLearningOpps );

			requirements.TopLevelCredentials = credential.Requires.SelectMany( m => m.TargetCredential )
							.Concat( assessment.Requires.SelectMany( m => m.TargetCredential ) )
							.Concat( learningOpportunity.Requires.SelectMany( m => m.TargetCredential ) ).ToList();
			requirements.TopLevelAssessments = credential.Requires.SelectMany( m => m.TargetAssessment )
							.Concat( assessment.Requires.SelectMany( m => m.TargetAssessment ) )
							.Concat( learningOpportunity.Requires.SelectMany( m => m.TargetAssessment ) ).ToList();
			requirements.TopLevelLearningOpportunities = credential.Requires.SelectMany( m => m.TargetLearningOpportunity )
							.Concat( assessment.Requires.SelectMany( m => m.TargetLearningOpportunity ) )
							.Concat( learningOpportunity.Requires.SelectMany( m => m.TargetLearningOpportunity ) ).ToList();

			requirements.TargetAssessment = allRequiredAssessments;
			requirements.TargetLearningOpportunity = gatheredLearningOpps;
			requirements.TargetCredential = allRequiredCredentials;

			//Don't bother recursive bubbling recommendations
			recommendations.TargetAssessment = new List<WMP.AssessmentProfile>() { assessment }
				.Concat( credential.Recommends.SelectMany( m => m.TargetAssessment ) )
				.Concat( assessment.Recommends.SelectMany( m => m.TargetAssessment ) )
				.Concat( learningOpportunity.Recommends.SelectMany( m => m.TargetAssessment ) )
				.ToList();
			recommendations.TargetLearningOpportunity = new List<WMP.LearningOpportunityProfile>() { learningOpportunity }
				.Concat( credential.Recommends.SelectMany( m => m.TargetLearningOpportunity ) )
				.Concat( assessment.Recommends.SelectMany( m => m.TargetLearningOpportunity ) )
				.Concat( learningOpportunity.Recommends.SelectMany( m => m.TargetLearningOpportunity ) )
				.ToList();
			recommendations.TargetCredential = new List<MC.Credential>() { credential }
				.Concat( assessment.Recommends.SelectMany( m => m.TargetCredential ) )
				.Concat( learningOpportunity.Recommends.SelectMany( m => m.TargetCredential ) )
				.Concat( learningOpportunity.Recommends.SelectMany( m => m.TargetCredential ) )
				.ToList();

			connections.Requires = credential.Requires.Concat( assessment.Requires ).Concat( learningOpportunity.Requires ).ToList();
			connections.Recommends = credential.Recommends.Concat( assessment.Recommends ).Concat( learningOpportunity.Recommends ).ToList();
		}


		//

		public static CompetencyWrapper GetAllCompetencies( List<WMP.ConditionProfile> containers, bool includeBubbled )
		{
			var wrapper = new CompetencyWrapper();

			//Concatenate all of the frameworks from all of the condition profiles
			if ( includeBubbled )
			{
				wrapper.RequiresByFramework = containers.SelectMany( m => m.RequiresCompetenciesFrameworks )
					.Concat( containers.SelectMany( m => m.TargetCredential ).SelectMany( m => m.Requires ).SelectMany( m => m.RequiresCompetenciesFrameworks ) )
					.Concat( containers.SelectMany( m => m.TargetAssessment ).SelectMany( m => m.RequiresCompetenciesFrameworks ) )
					.Concat( containers.SelectMany( m => m.TargetLearningOpportunity ).SelectMany( m => m.RequiresCompetenciesFrameworks ) )
					.Where( m => m != null )
					.ToList();
			}
			else
			{
				wrapper.RequiresByFramework = containers.SelectMany( m => m.RequiresCompetenciesFrameworks ).Where( m => m != null ).ToList();
			}

			//No bubbling for these(?)
			wrapper.AssessesByFramework = containers.SelectMany( m => m.TargetAssessment ).SelectMany( m => m.AssessesCompetenciesFrameworks ).Where( m => m != null ).ToList();
			wrapper.TeachesByFramework = containers.SelectMany( m => m.TargetLearningOpportunity ).SelectMany( m => m.TeachesCompetenciesFrameworks ).Where( m => m != null ).ToList();

			//Remove duplicates
			wrapper.RequiresByFramework = DeduplicateAndMergeFrameworksAndCompetencies( wrapper.RequiresByFramework );
			wrapper.AssessesByFramework = DeduplicateAndMergeFrameworksAndCompetencies( wrapper.AssessesByFramework );
			wrapper.TeachesByFramework = DeduplicateAndMergeFrameworksAndCompetencies( wrapper.TeachesByFramework );

			return wrapper;
		}
		private static List<MC.CredentialAlignmentObjectFrameworkProfile> DeduplicateAndMergeFrameworksAndCompetencies( List<MC.CredentialAlignmentObjectFrameworkProfile> source )
		{
			var result = new List<MC.CredentialAlignmentObjectFrameworkProfile>();

			foreach( var framework in source )
			{
				var match = result.FirstOrDefault( m => m.Framework == framework.Framework );
				if ( match != null )
				{
					//now check if there are any competencies frm the current framework not in the matched framework
					foreach( var competency in framework.Items )
					{
						if( match.Items.FirstOrDefault( m => m.TargetNode == competency.TargetNode ) == null )
						{
							match.Items.Add( competency );
						}
					}
				}
				else
				{
					result.Add( framework );
				}
			}

			return result;
		}
		//

		public static void GetChildLearningOpps( List<WMP.LearningOpportunityProfile> learningOpportunities, List<WMP.LearningOpportunityProfile> runningTotal )
		{
			foreach ( var lopp in learningOpportunities )
			{
				if ( runningTotal.Where( m => m.Id == lopp.Id ).Count() == 0 )
				{
					runningTotal.Add( lopp );
					GetChildLearningOpps( lopp.HasPart, runningTotal );
				}
			}
		}
		//

		public static void GetChildCredentials( List<MC.Credential> credentials, List<MC.Credential> runningCredTotal, List<WMP.AssessmentProfile> runningAssessmentTotal, List<WMP.LearningOpportunityProfile> runningLoppTotal )
		{
			foreach ( var cred in credentials )
			{
				if ( runningCredTotal.Where( m => m.Id == cred.Id ).Count() == 0 )
				{
					runningCredTotal.Add( cred );
					//GetChildCredentials( cred.EmbeddedCredentials, runningCredTotal, runningAssessmentTotal, runningLoppTotal );
					GetChildCredentials( cred.Requires.SelectMany( m => m.TargetCredential ).ToList(), runningCredTotal, runningAssessmentTotal, runningLoppTotal );

					foreach ( var assessment in cred.Requires.SelectMany( m => m.TargetAssessment ) )
					{
						if ( runningAssessmentTotal.Where( m => m.Id == assessment.Id ).Count() == 0 )
						{
							runningAssessmentTotal.Add( assessment );
						}
					}

					foreach ( var lopp in cred.Requires.SelectMany( m => m.TargetLearningOpportunity ) )
					{
						if ( runningLoppTotal.Where( m => m.Id == lopp.Id ).Count() == 0 )
						{
							runningLoppTotal.Add( lopp );
						}
					}
				}
			}
		}
		public static List<WMA.CompetencyFramework> GetFrameworks( List<MC.CredentialAlignmentObjectFrameworkProfile> input, List<WMP.LearningOpportunityProfile> runningTotal )
		{
			var output = new List<WMA.CompetencyFramework>();
			if ( input == null || !input.Any() )
				return null;		

			foreach ( var framework in input )
			{
				if ( framework.Items == null || !framework.Items.Any() )
					continue;

				var fo = new WMA.CompetencyFramework();
				fo.Name = PickText( new List<string>() { framework.FrameworkName, framework.ProfileName, framework.ProfileSummary } );
				fo.Description = framework.Description;
				fo.Source = framework.Framework;
				//comptencies
				foreach ( var competency in framework.Items)
				{
					var comp = new WMA.Competency();
					comp.CompetencyLabel = PickText( new List<string>() { competency.TargetNodeName, competency.ProfileName } );
					//comp.Description = PickText( new List<string>() { competency.Description, competency.TargetNodeDescription } );
					//if ( comp.CompetencyText == comp.Description) //CompetencyText is already set?
					//{
					//	comp.Description = "";
					//}
				}
				
			}

			return output;
		}
		//
		public static string PickText( List<string> choices )
		{
			return choices.FirstOrDefault( m => HasText( m ) );
		}

		public static bool HasText( string text )
		{
			return !string.IsNullOrWhiteSpace( text );
		}
		#endregion

		public static WMA.RegistryData FillRegistryData( string ctid, string label = "" )
		{
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				return null;
			}

			var envelopeBase = workIT.Utilities.UtilityManager.GetAppKeyValue( "cerGetEnvelope" );
			var community = workIT.Utilities.UtilityManager.GetAppKeyValue( "defaultCommunity" );
			var resourceBase = workIT.Utilities.UtilityManager.GetAppKeyValue( "credentialRegistryResource" );
			var envelopeURL = string.Format( envelopeBase, community, ctid );
			var resourceURL = string.Format( resourceBase, community, ctid );
			if ( community != "ce-registry" && UtilityManager.GetAppKeyValue( "usingAssistantForRegistryGets", false ) )//
			{
				var assistantAPIUrl = ConfigHelper.GetConfigValue( "assistantAPIUrl", "" );
				//only include the key if the default community is not ce-registry
				string todaysKey = UtilityManager.GenerateMD5String( DateTime.Now.ToString( "yyyy-MM-dd" ) );
				resourceURL = assistantAPIUrl + string.Format( "resources/{0}/?community={1}&apiKey={2}", ctid, community, todaysKey );
				envelopeURL = assistantAPIUrl + string.Format( "envelopes/{0}/?community={1}&apiKey={2}", ctid, community, todaysKey );

			}
			var output = new WMA.RegistryData()
			{
				CTID = ctid,
				Envelope = new WMA.LabelLink()
				{
					Label= "View Envelope",
					URL = envelopeURL, //string.Format( envelopeBase, community, ctid ),
				},
				Resource = new WMA.LabelLink()
				{
					Label = "View Resource",
					URL = resourceURL,//string.Format( resourceBase, community, ctid ),
				},
			};
            //not including show hide
            if ( UtilityManager.GetAppKeyValue( "includeRegistryPayloadWithDetails", false ) )
            {
                var registryImport = workIT.Factories.ImportManager.GetByCtid( ctid );
                if ( registryImport == null || string.IsNullOrWhiteSpace( registryImport.Payload ) )
                {

                }
                else
                    output.RawMetadata = registryImport.Payload;
            }

            return output;
		}
	}

	public class MergedConditions : WMP.ConditionProfile
	{
		public MergedConditions()
		{
			TopLevelCredentials = new List<MC.Credential>();
			TopLevelAssessments = new List<WMP.AssessmentProfile>();
			TopLevelLearningOpportunities = new List<WMP.LearningOpportunityProfile>();
		}

		public List<MC.Credential> CredentialsSansSelf( int id )
		{
			return TargetCredential.Where( m => m.Id != id ).ToList();
		}
		public List<WMP.AssessmentProfile> AssessmentsSansSelf( int id )
		{
			return TargetAssessment.Where( m => m.Id != id ).ToList();
		}
		public List<WMP.LearningOpportunityProfile> LearningOpportunitiesSansSelf( int id )
		{
			return TargetLearningOpportunity.Where( m => m.Id != id ).ToList();
		}

		public List<MC.Credential> TopLevelCredentials { get; set; }
		public List<WMP.AssessmentProfile> TopLevelAssessments { get; set; }
		public List<WMP.LearningOpportunityProfile> TopLevelLearningOpportunities { get; set; }
	}
	//
	public class CompetencyWrapper
	{
		public CompetencyWrapper()
		{
			Requires = new List<MC.CredentialAlignmentObjectProfile>();
			Teaches = new List<MC.CredentialAlignmentObjectProfile>();
			Assesses = new List<MC.CredentialAlignmentObjectProfile>();
			RequiresByFramework = new List<MC.CredentialAlignmentObjectFrameworkProfile>();
			AssessesByFramework = new List<MC.CredentialAlignmentObjectFrameworkProfile>();
			TeachesByFramework = new List<MC.CredentialAlignmentObjectFrameworkProfile>();
		}
		public List<MC.CredentialAlignmentObjectProfile> Requires { get; set; }
		public List<MC.CredentialAlignmentObjectProfile> Teaches { get; set; }
		public List<MC.CredentialAlignmentObjectProfile> Assesses { get; set; }
		public List<MC.CredentialAlignmentObjectProfile> Concatenated { get { return Requires.Concat( Teaches ).Concat( Assesses ).ToList(); } }
		public int Total { get { return Concatenated.Count(); } }

		public List<MC.CredentialAlignmentObjectFrameworkProfile> RequiresByFramework { get; set; }
		public List<MC.CredentialAlignmentObjectFrameworkProfile> AssessesByFramework { get; set; }
		public List<MC.CredentialAlignmentObjectFrameworkProfile> TeachesByFramework { get; set; }
		public List<MC.CredentialAlignmentObjectFrameworkProfile> ConcatenatedFrameworks { get { return RequiresByFramework.Concat( TeachesByFramework ).Concat( AssessesByFramework ).ToList(); } }
		// Will be checked later
		public List<MC.CredentialAlignmentObjectItem> ConcatenatedCompetenciesFromFrameworks { get { return ConcatenatedFrameworks.SelectMany( m => m.Items ).ToList(); } }
		public int TotalFrameworks { get { return ConcatenatedFrameworks.Count(); } }
		public int TotalCompetenciesWithinFrameworks { get { return ConcatenatedCompetenciesFromFrameworks.Count(); } }
	}
	//

	public class ConnectionData
	{
		public ConnectionData()
		{
			foreach ( var item in this.GetType().GetProperties().Where( m => m.PropertyType == typeof( List<WMP.ConditionProfile> ) ) )
			{
				item.SetValue( this, new List<WMP.ConditionProfile>() );
			}
		}
		public static ConnectionData Process( List<WMP.ConditionProfile> connections, ConnectionData existing, List<MC.ConditionManifest> commonConditions )
		{
			var result = new ConnectionData();
			connections = connections ?? new List<WMP.ConditionProfile>();
			existing = existing ?? new ConnectionData();
			//Handle common conditions
			var manifests = MC.ConditionManifestExpanded.ExpandConditionManifestList( commonConditions ?? new List<MC.ConditionManifest>() );
			//Handle condition profiles
			var conditions = MC.ConditionManifestExpanded.DisambiguateConditionProfiles( connections );
			result.Requires = existing.Requires
				.Concat( conditions.Requires )
				.Concat( manifests.SelectMany( m => m.Requires ) )
				.ToList();
			result.Recommends = existing.Recommends
				.Concat( conditions.Recommends )
				.Concat( manifests.SelectMany( m => m.Recommends ) )
				.ToList();
			result.PreparationFrom = existing.PreparationFrom
				.Concat( conditions.PreparationFrom )
				.Concat( manifests.SelectMany( m => m.PreparationFrom ) )
				.ToList();
			result.AdvancedStandingFrom = existing.AdvancedStandingFrom
				.Concat( conditions.AdvancedStandingFrom )
				.Concat( manifests.SelectMany( m => m.AdvancedStandingFrom ) )
				.ToList();
			result.IsRequiredFor = existing.IsRequiredFor
				.Concat( conditions.IsRequiredFor )
				.Concat( manifests.SelectMany( m => m.IsRequiredFor ) )
				.ToList();
			result.IsRecommendedFor = existing.IsRecommendedFor
				.Concat( conditions.IsRecommendedFor )
				.Concat( manifests.SelectMany( m => m.IsRecommendedFor ) )
				.ToList();
			result.IsAdvancedStandingFor = existing.IsAdvancedStandingFor
				.Concat( conditions.IsAdvancedStandingFor )
				.Concat( manifests.SelectMany( m => m.IsAdvancedStandingFor ) )
				.ToList();
			result.IsPreparationFor = existing.IsPreparationFor
				.Concat( conditions.IsPreparationFor )
				.Concat( manifests.SelectMany( m => m.IsPreparationFor ) )
				.ToList();
			result.Corequisite = existing.Corequisite
				.Concat( conditions.Corequisite )
				.Concat( manifests.SelectMany( m => m.Corequisite ) )
				.ToList();
			result.EntryCondition = existing.EntryCondition
				.Concat( conditions.EntryCondition )
				.Concat( manifests.SelectMany( m => m.EntryCondition ) )
				.ToList();
			result.Renewal = existing.Renewal
				.Concat( conditions.Renewal )
				.Concat( manifests.SelectMany( m => m.Renewal ) )
				.ToList();

			return result;
		}
		public List<WMP.ConditionProfile> Requires { get; set; }
		public List<WMP.ConditionProfile> Recommends { get; set; }
		public List<WMP.ConditionProfile> PreparationFrom { get; set; }
		public List<WMP.ConditionProfile> AdvancedStandingFrom { get; set; }
		public List<WMP.ConditionProfile> IsRequiredFor { get; set; }
		public List<WMP.ConditionProfile> IsRecommendedFor { get; set; }
		public List<WMP.ConditionProfile> IsAdvancedStandingFor { get; set; }
		public List<WMP.ConditionProfile> IsPreparationFor { get; set; }
		public List<WMP.ConditionProfile> Corequisite { get; set; }
		public List<WMP.ConditionProfile> EntryCondition { get; set; }
		public List<WMP.ConditionProfile> Renewal { get; set; }
	}
	//
	public class RolesFilter
	{
		public string n { get; set; }
		public int aid { get; set; }
		public List<string> rid { get; set; }
		public string r { get; set; }
		public string p { get; set; }
		
		public string d { get; set; }

	}
}