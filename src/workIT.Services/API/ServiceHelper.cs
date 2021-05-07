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
using WMP = workIT.Models.ProfileModels;
using WMS = workIT.Models.Search;
using workIT.Factories;

namespace workIT.Services.API
{
	public class ServiceHelper
	{
		public static string externalFinderSiteURL = UtilityManager.GetAppKeyValue( "externalFinderSiteURL" );
		public static string baseFinderSiteURL = UtilityManager.GetAppKeyValue( "baseFinderSiteURL" );
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
		public static void MapEntitySearchLink( int orgId, string orgName, int entityCount, string labelTemplate, string searchType, ref List<WMA.LabelLink> output, string roles = "6,7" )
		{
			//var output = new WMA.LabelLink();
			if ( orgId < 1 )
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
				var url = string.Format( "search?autosearch=true&searchType={0}&custom=((n:'organizationroles',aid:{1},rid:[{2}],p:'{3}',d:'{4}',r:''))", searchType, orgId, roles, orgName, HttpUtility.UrlPathEncode ( label ) );
				url = url.Replace( "((", "{" ).Replace( "))", "}" );
				url = url.Replace( "'", "%27" ).Replace( " ", "%20" );
				//url = HttpUtility.UrlPathEncode ( url );

				//new --------------------------------------
				var part1 = string.Format( "search?searchType={0}&filteritemtext={1}", searchType, HttpUtility.UrlPathEncode ( urlLabel ) );
				//json format,probably not
				var part2 = "{" + string.Format( "\"n\":\"organizationroles\",\"aid\":{0},\"rid\":[{1}]", orgId, roles ) + "}";
				var part3 = HttpUtility.UrlPathEncode ( part2 );
				var part4 = "&filterparameters=" + part3;

				output.Add( new WMA.LabelLink()
				{
					Label = label,
					Total = entityCount,
					URL = externalFinderSiteURL + part1 + part4,
					TestURL = baseFinderSiteURL + url
				} );

				

				//output.Add( new WMA.LabelLink()
				//{
				//	Label = label,
				//	Count = entityCount,
				//	URL = externalFinderSiteURL + part1 + part4
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
				//	URL = externalFinderSiteURL + part1 + part4
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
				var url = string.Format( "search?autosearch=true&searchType={0}&custom=((n:'organizationroles',aid:{1},rid:[1,2,10,12],p:'{2}',d:'{3}',r:''))", searchType, orgId, orgName, HttpUtility.UrlPathEncode ( label ) );
				url = url.Replace( "((", "{" ).Replace( "))", "}" );
				url = url.Replace( "'", "%27" ).Replace( " ", "%20" );
				//url = HttpUtility.UrlPathEncode ( url );

				//new --------------------------------------
				//var roleList = roles.Split( ',' ).ToList();
				var part1 = string.Format( "search?searchType={0}&filteritemtext={1}", searchType, HttpUtility.UrlPathEncode ( urlLabel ) );
				//json format,probably not
				var part2 = "{" + string.Format( "\"n\":\"organizationroles\",\"aid\":{0},\"rid\":[{1}]", orgId, roles ) + "}";
				var part3 = HttpUtility.UrlPathEncode ( part2 );
				var part4 = "&filterparameters=" + part3;

				output.Add( new WMA.LabelLink()
				{
					Label = label,
					Total = entityCount,
					URL = externalFinderSiteURL + part1 + part4,
					TestURL = baseFinderSiteURL + url
				} );



				//output.Add( new WMA.LabelLink()
				//{
				//	Label = label,
				//	Count = entityCount,
				//	URL = externalFinderSiteURL + part1 + part4
				//} );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, "ServiceHelper.MapQAPerformedLink" + ex.Message );
			}
			//return output;

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
						orp.URL = externalFinderSiteURL + string.Format( "organization/{0}", item.ActingAgent.Id );
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
									MapEntitySearchLink( item.ActingAgent.Id, item.ActingAgent.Name, 0, ar.Name, searchType, ref orp.Tags, searchRoles );//ar.Id.ToString()
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
			var output =  new List<WMA.Outline>();
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
						orp.URL = externalFinderSiteURL + string.Format( "organization/{0}", item.ActingAgent.Id );
					bool isPublishedByRole = false;
					if ( item.AgentRole != null && item.AgentRole.Items.Any() )
					{
						foreach ( var ar in item.AgentRole.Items )
						{
							//no link
							if ( ar.Id == 30 )
							{
								//if published by, probably will not have other roles!
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
					if ( !isPublishedByRole && orp.Tags.Any() )
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
		/// <param name="label"></param>
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
						}
					}
					if ( string.IsNullOrEmpty( item.ActingAgent.CTID ) )
						orp.URL = item.ActingAgent.SubjectWebpage;
					else
						orp.URL = externalFinderSiteURL + string.Format( "organization/{0}", item.ActingAgent.Id );
					//
					//not sure if we want all roles here - current case could be for parent org
					if ( item.AgentRole != null && item.AgentRole.Items.Any() )
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
			//var filter = new WMS.Filter();
			//if ( input != null && input.Items.Any() )
			//{
			//	filter = SearchServices.ConvertEnumeration( input.Name, input );
			//}
			//
			//https://sandbox.credentialengine.org/finder/search?autosearch=true&searchType=organization&filters=7-1185
			foreach ( var item in input.Items )
			{
				var value = new WMA.LabelLink()
				{
					SearchType = searchType,
					Label = item.Name,//confirm this will be consistant					
				};
				var oldUrl = baseFinderSiteURL + string.Format( "search?autosearch=true&searchType={0}&filters={1}-{2}", searchType, input.Id, item.Id );
				var url = externalFinderSiteURL + string.Format( "search?searchType={0}&filterid={1}&filteritemid={2}", searchType, input.Id, item.Id );
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
				var oldUrl = baseFinderSiteURL + string.Format( "search?autosearch=true&searchType={0}&filters={1}-{2}", searchType, input.Id, item.Id );
				var url = externalFinderSiteURL + string.Format( "search?searchType={0}&filterid={1}&filteritemid={2}", searchType, input.Id, item.Id );

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
			var oldUrl = baseFinderSiteURL + string.Format( "{0}/{1}/{2}", entityType, id, label );
			var url = externalFinderSiteURL + string.Format( "{0}/{1}/{2}", entityType, id, label );
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
			if ( string.IsNullOrWhiteSpace(input))
				return null;
			//
			var output = new WMA.LabelLink()
			{
				Label= label,
				URL= formatUrl ? input : ""
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
				var label = HttpUtility.UrlPathEncode ( item.Name );
				var oldUrl = baseFinderSiteURL + string.Format( "search?autosearch=true&searchType={0}&keywords={1}", searchType, label );
				var url = externalFinderSiteURL + string.Format( "search?searchType={0}&filterid={1}&filteritemtext={2}", searchType, frameworkCategoryId, label );
				
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
			if ( categoryId > 0)
				filterId = string.Format( "&filterid={0}", categoryId );
			foreach ( var item in input )
			{
				var keyword = HttpUtility.UrlPathEncode ( item.TextValue );

				if ( !string.IsNullOrWhiteSpace( item.TextValue ) )
				{
					var value = new WMA.LabelLink()
					{
						Label = item.TextValue,//confirm this will be consistant
						URL = externalFinderSiteURL + string.Format( "search?autosearch=true&searchType={0}&keywords={1}{2}", searchType, keyword, filterId )
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
			if ( input == null || input.Count() == 0 )
				return null;
			//
			foreach ( var item in input )
			{
				var pp = new ME.JurisdictionProfile()
				{
					Description = item.Description,
					GlobalJurisdiction = item.GlobalJurisdiction,
					MainJurisdiction=null
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
						if ( item.MainJurisdiction.HasData())
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
						Identifier = MapIdentifierValue(item.Identifier)
					};
					if ( item.Latitude != 0 )
						address.Latitude = item.Latitude;
					if ( item.Longitude != 0 )
						address.Longitude = item.Longitude;

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

			foreach(var item in input)
			{
				if ( item.HasData() ) {
					var iv = new WMA.IdentifierValue()
					{
						IdentifierType = item.IdentifierType,
						IdentifierTypeName = string.IsNullOrWhiteSpace(item.Name) ? label : item.Name,
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
			if (list != null && list.Any())
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
				pp.DataCollectionMethodType = MapPropertyLabelLinks( item.DataCollectionMethodType, searchType, false );
				pp.ExternalInputType = MapPropertyLabelLinks( item.ExternalInputType, searchType, false );

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
					RevocationCriteriaDescription = item.RevocationCriteriaDescription,
					RevocationCriteriaUrl = item.RevocationCriteriaUrl
				};
				pp.Jurisdiction = MapJurisdiction( item.Jurisdiction );
				//pp.Region = MapJurisdiction( input.Region );
				list.Add( pp );
			}
			if ( list != null && list.Any() )
			{
				output.Label = string.Format("Has Revocation Profile", list.Count);
				output.Total = list.Count;
				List<object> obj = list.Select( f => ( object )f ).ToList();
				output.Values = obj;
			}
			return output;
		}

		public static MC.TopLevelEntityReference MapToEntityReference( MC.TopLevelObject input, string entityType="" )
		{

			if ( input == null || string.IsNullOrWhiteSpace( input.Name ) )
				return null;// new MC.TopLevelEntityReference();	//or NULL

			//var externalFinderSiteURL = UtilityManager.GetAppKeyValue( "baseFinderSiteURL" );

			var output = new MC.TopLevelEntityReference()
			{
				Id = input.Id,//need for links, or may need to create link here
				Name = input.Name,
				SubjectWebpage = input.SubjectWebpage,
				Description = input.Description,
				CTID = input.CTID,
				EntityTypeId = input.EntityTypeId,
				Image=input.Image
			};
			if ( !string.IsNullOrWhiteSpace( entityType ) )
				output.DetailURL = externalFinderSiteURL + entityType+"/" + output.Id;
			else 
			if ( !string.IsNullOrWhiteSpace( output.CTID ) )
				output.DetailURL = externalFinderSiteURL + "resources/" + output.CTID;

			return output;
		}
		public static WMS.AJAXSettings MapAssessmentToAJAXSettings( List<WMP.AssessmentProfile> input, string label )
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
					Label = string.Format( label, input.Count ),
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
		public static WMS.AJAXSettings MapCredentialToAJAXSettings( List<MC.Credential> input, string label )
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
					Label = string.Format( label, input.Count ),
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
		public static WMS.AJAXSettings MapLearningOppToAJAXSettings( List<WMP.LearningOpportunityProfile> input, string label )
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
					Label = string.Format( label, input.Count ),
					Total = input.Count
				};
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
			catch(Exception ex)
			{
				LoggingHelper.LogError( ex, "ServiceHelper.MapOutlineToAJAX" );
				return null;
			}
			
		}
		public static WMS.AJAXSettings MapOutlineToAJAX( WMA.Outline input, string label )
		{

			if ( input == null || string.IsNullOrWhiteSpace(input.Label ))
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

			//var externalFinderSiteURL = UtilityManager.GetAppKeyValue( "baseFinderSiteURL" );

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
			if ( (entityType ?? "").ToLower() == "organization" )
				output.URL = externalFinderSiteURL + entityType + "/" + input.Id;

			else if ( !string.IsNullOrWhiteSpace( entityType ) && ( !input.IsReferenceEntity || !string.IsNullOrWhiteSpace( input.Description ) ) )
				output.URL = externalFinderSiteURL + entityType + "/" + input.Id;

			else if( !string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
				output.URL = input.SubjectWebpage; // externalFinderSiteURL + "resources/" + input.CTID;

			if (input.OwningOrganizationId > 0 && !string.IsNullOrWhiteSpace(input.OrganizationName))
			{
				output.Provider = new WMA.LabelLink()
				{
					Label = input.OrganizationName,
					URL = externalFinderSiteURL + entityType + "organization/" + input.OwningOrganizationId
				};
			}
			return output;

		}

		public static WMS.AJAXSettings MapOrganizationRoleProfileToAJAX( List<WMP.OrganizationRoleProfile> input, string label )
		{

			if ( input == null || !input.Any() )
				return null;
			try
			{
				var list = new List<MC.TopLevelEntityReference>();
				foreach ( var item in input )
				{
					var tlo = MapToEntityReference( item );
					if ( tlo != null && !string.IsNullOrWhiteSpace( tlo.Name ) )
					{
						list.Add( tlo );
					}
				}
				if ( !list.Any() )
					return null;
				//
				var output = new WMS.AJAXSettings()
				{
					//Type=null,
					Label = string.Format( label, input.Count ),
					Total = input.Count
				};
				List<object> obj = list.Select( f => ( object )f ).ToList();
				output.Values = obj;
				return output;
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
				output.DetailURL = externalFinderSiteURL + "organization/" + output.Id;
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
					Label=item.Label,
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
						var code = CodesManager.GetCurrencyCode( qv.UnitText );
						if ( code != null && code.NumericCode > 0 )
						{
							qv.CurrencySymbol = code.HtmlCodes;
							qv.UnitText = code.Currency;
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
					//Meta_LastUpdated = item.LastUpdated

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
					Meta_LastUpdated = item.LastUpdated
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

			//var externalFinderSiteURL = UtilityManager.GetAppKeyValue( "baseFinderSiteURL" );

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
				if ( tlo != null && !string.IsNullOrWhiteSpace( tlo.Description ) )
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

			if ( input == null || string.IsNullOrWhiteSpace( input.Description ) )
				return null;

			//var externalFinderSiteURL = UtilityManager.GetAppKeyValue( "baseFinderSiteURL" );

			var output = new WMA.ConditionProfile()
			{
				Name = input.ProfileName,
				SubjectWebpage = input.SubjectWebpage,
				Description = input.Description,
				Experience = input.Experience,
				AudienceLevelType = MapPropertyLabelLinks( input.AudienceLevelType, searchType, false ),
				AudienceType = MapPropertyLabelLinks( input.AudienceType, searchType, false ),
				CreditUnitTypeDescription = input.CreditUnitTypeDescription,
				SubmissionOfDescription = input.SubmissionOfDescription,
			};
			if ( !string.IsNullOrWhiteSpace(input.DateEffective ))
				output.DateEffective = input.DateEffective;
			//only return if > 0
			if ( input.MinimumAge > 0 )
				output.MinimumAge = input.MinimumAge;
			if ( input.Weight > 0 )
				output.Weight = input.Weight;
			if ( input.YearsOfExperience > 0 )
				output.YearsOfExperience = input.YearsOfExperience;

			//
			if ( input.AssertedBy != null && !string.IsNullOrWhiteSpace(input.AssertedBy.Name))
			{
				var ab = ServiceHelper.MapToOutline( input.AssertedBy, "organization" );
				output.AssertedBy = ServiceHelper.MapOutlineToAJAX( ab, "Asserted by {0} Organization(s)" );
			}
			//
			output.Condition = MapTextValueProfileTextValue( input.Condition ); 
			output.CreditValue = ServiceHelper.MapValueProfile( input.CreditValueList, searchType );


			output.Jurisdiction = MapJurisdiction( input.Jurisdiction );
			output.ResidentOf = MapJurisdiction( input.ResidentOf );
			output.SubmissionOf = MapTextValueProfileTextValue( input.SubmissionOf );
			//CreditValue

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
			var list = new List<WMA.DurationProfile>();

			var outputList = new List<WMS.AJAXSettings>();
			foreach ( var item in input )
			{
				if ( item.EstimatedDuration == null || !item.EstimatedDuration.Any() )
					continue;

				var edlist = MapDurationProfiles( item.EstimatedDuration );
				if ( edlist != null && edlist.Any() )
				{
					list.AddRange( edlist );
					outputList.Add( new WMS.AJAXSettings()
					{
						Label = item.Name,
						URL = baseFinderSiteURL + string.Format( "{0}/{1}/{2}", "Credential", item.Id, item.FriendlyName ),
						Total = edlist.Count(),
						Values = edlist.Select( f => ( object )f ).ToList()
					} );
				}
			}
			var output = new WMS.AJAXSettings();

			if ( list != null && list.Any() )
			{
				output.Label = label;
				output.Total = outputList.Count;
				List<object> obj = outputList.Select( f => ( object )f ).ToList();
				output.Values = obj;
			}
			return output;
		}
		public static WMS.AJAXSettings GetAllDurationsOLD( List<WMP.AssessmentProfile> input, string label )
		{
			if ( input == null || !input.Any() )
				return null;
			var list = new List<WMA.DurationProfile>();
			
			var outputList = new List<WMS.AJAXSettings>();
			foreach ( var item in input )
			{
				if ( item.EstimatedDuration == null || !item.EstimatedDuration.Any() )
					continue;

				var edlist = MapDurationProfiles( item.EstimatedDuration );
				if ( edlist != null && edlist.Any() )
				{
					list.AddRange( edlist );
					outputList.Add( new WMS.AJAXSettings()
					{
						Label = item.Name,
						URL = baseFinderSiteURL + string.Format( "{0}/{1}/{2}", "Assessment", item.Id, item.FriendlyName ),
						Total = edlist.Count(),
						Values = edlist.Select( f => ( object )f ).ToList()
					});
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
			return output;
		}
		public static WMS.AJAXSettings GetAllDurations( List<WMP.AssessmentProfile> input, string label )
		{
			if ( input == null || !input.Any() )
				return null;
			var list = new List<WMA.DurationProfile>();

			var outputList = new List<WMS.AJAXSettings>();
			foreach ( var item in input )
			{
				if ( item.EstimatedDuration == null || !item.EstimatedDuration.Any() )
					continue;

				var edlist = MapDurationProfiles( item.EstimatedDuration );
				if ( edlist != null && edlist.Any() )
				{
					list.AddRange( edlist );
					outputList.Add( new WMS.AJAXSettings()
					{
						Label = item.Name,
						URL = baseFinderSiteURL + string.Format( "{0}/{1}/{2}", "Assessment", item.Id, item.FriendlyName ),
						Total = edlist.Count(),
						Values = edlist.Select( f => ( object )f ).ToList()
					} );
				}
			}
			var output = new WMS.AJAXSettings();

			if ( list != null && list.Any() )
			{
				output.Label = label;
				output.Total = outputList.Count;
				List<object> obj = outputList.Select( f => ( object )f ).ToList();
				output.Values = obj;
			}
			return output;
		}
		public static WMS.AJAXSettings GetAllDurations( List<WMP.LearningOpportunityProfile> input, string label )
		{
			if ( input == null || !input.Any() )
				return null;
			var list = new List<WMA.DurationProfile>();

			var outputList = new List<WMS.AJAXSettings>();
			foreach ( var item in input )
			{
				if ( item.EstimatedDuration == null || !item.EstimatedDuration.Any() )
					continue;

				var edlist = MapDurationProfiles( item.EstimatedDuration );
				if ( edlist != null && edlist.Any() )
				{
					list.AddRange( edlist );
					outputList.Add( new WMS.AJAXSettings()
					{
						Label = item.Name,
						URL = baseFinderSiteURL + string.Format( "{0}/{1}/{2}", "LearningOpportunity", item.Id, item.FriendlyName ),
						Total = edlist.Count(),
						Values = edlist.Select( f => ( object )f ).ToList()
					} );
				}
			}
			var output = new WMS.AJAXSettings();

			if ( list != null && list.Any() )
			{
				output.Label = label;
				output.Total = outputList.Count;
				List<object> obj = outputList.Select( f => ( object )f ).ToList();
				output.Values = obj;
			}
			return output;
		}
		public static List<WMA.DurationProfile> MapDurationProfiles( List<WMP.DurationProfile> input )
		{
			var output = new List<WMA.DurationProfile>();
			if ( input == null || !input.Any() )
				return null;

			foreach ( var item in input )
			{
				var tlo = MapDurationProfile( item );
				if ( tlo != null && ( !string.IsNullOrWhiteSpace( item.DurationSummary )
			  || !string.IsNullOrWhiteSpace( item.Description ) ) )
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

			if ( string.IsNullOrWhiteSpace( output.DurationSummary ) )
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
				if ( fap != null && !string.IsNullOrWhiteSpace( fap.Description ) )
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

		public static void GetAllChildren( MergedConditions requirements, MergedConditions recommendations, ConnectionData connections, MC.Credential credential, WMP.AssessmentProfile assessment, WMP.LearningOpportunityProfile learningOpportunity )
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

		public static CompetencyWrapper GetAllCompetencies( WMP.ConditionProfile container )
		{
			var wrapper = new CompetencyWrapper();

			//Data by framework is reliably populated
			//.Concat( container.TargetAssessment.SelectMany( m => m.RequiresCompetenciesFrameworks ) )
			wrapper.RequiresByFramework = container.TargetCredential.SelectMany( m => m.Requires ).SelectMany( m => m.RequiresCompetenciesFrameworks )
				.Concat( container.TargetLearningOpportunity.SelectMany( m => m.RequiresCompetenciesFrameworks ) )
				.Where( m => m != null )
				.ToList();
			wrapper.AssessesByFramework = container.TargetAssessment.SelectMany( m => m.AssessesCompetenciesFrameworks ).Where( m => m != null ).ToList();
			wrapper.TeachesByFramework = container.TargetLearningOpportunity.SelectMany( m => m.TeachesCompetenciesFrameworks ).Where( m => m != null ).ToList();

			//Data by competency is not reliably populated, so, instead get it from the frameworks
			wrapper.Requires = MC.CredentialAlignmentObjectFrameworkProfile.FlattenAlignmentObjects( wrapper.RequiresByFramework );
			wrapper.Assesses = MC.CredentialAlignmentObjectFrameworkProfile.FlattenAlignmentObjects( wrapper.AssessesByFramework );
			wrapper.Teaches = MC.CredentialAlignmentObjectFrameworkProfile.FlattenAlignmentObjects( wrapper.TeachesByFramework );

			return wrapper;
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
				fo.Source = framework.SourceUrl;
				//comptencies
				foreach (var competency in framework.Items)
				{
					var comp = new WMA.Competency();
					comp.CompetencyLabel = PickText( new List<string>() { competency.TargetNodeName, competency.ProfileName } );
					comp.Description = PickText( new List<string>() { competency.Description, competency.TargetNodeDescription } );
					if ( comp.CompetencyText == comp.Description)
					{
						comp.Description = "";
					}
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

		public static WMA.RegistryData FillRegistryData( string ctid )
		{
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				return null;
			}

			var envelopeBase = workIT.Utilities.UtilityManager.GetAppKeyValue( "cerGetEnvelope" );
			var community = workIT.Utilities.UtilityManager.GetAppKeyValue( "defaultCommunity" );
			var resourceBase = workIT.Utilities.UtilityManager.GetAppKeyValue( "credentialRegistryResource" );

			var output = new WMA.RegistryData()
			{
				CTID = ctid,
				Envelope = new WMA.LabelLink()
				{
					Label= "View Envelope",
					URL = string.Format( envelopeBase, community, ctid ),
				},
				Resource = new WMA.LabelLink()
				{
					Label = "View Resource",
					URL = string.Format( resourceBase, community, ctid ),
				},
			};
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