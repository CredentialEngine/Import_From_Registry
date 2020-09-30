using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Download.Services;

namespace Download.Models
{
	public class RegistryObject
	{
		public RegistryObject( string payload )
		{
			if ( !string.IsNullOrWhiteSpace( payload ) )
			{
				dictionary = RegistryServices.JsonToDictionary( payload );
				if ( payload.IndexOf( "@graph" ) > 0 && payload.IndexOf( "@graph\": null" ) == -1 )
				{
					IsGraphObject = true;
					//get the graph object
					object graph = dictionary[ "@graph" ];
					//serialize the graph object
					var glist = JsonConvert.SerializeObject( graph );
					//parse graph in to list of objects
					JArray graphList = JArray.Parse( glist );

					var main = graphList[ 0 ].ToString();
					BaseObject = JsonConvert.DeserializeObject<RegistryBaseObject>( main );
					CtdlType = BaseObject.CdtlType;
					Ctid = BaseObject.Ctid;
					//not important to fully resolve yet
					if ( BaseObject.Name != null )
					{
						Name = BaseObject.Name.ToString();
						if ( Name.IndexOf( "{" ) > -1 && Name.IndexOf( ":" ) > 1 )
						{
							int pos = Name.IndexOf( "\"", Name.IndexOf( ":" ) );
							int endpos = Name.IndexOf( "\"", pos + 1 );
							if ( pos > 1 && endpos > pos )
							{
								Name = Name.Substring( pos + 1, endpos - ( pos + 1 ) );
							}
						}
						//if ( BaseObject.Name is LanguageMap )
						//{

						//}
					}
					else if ( CtdlType == "ceasn:CompetencyFramework" )
					{
						Name = ( BaseObject.CompetencyFrameworkName ?? "" ).ToString();
					}
					else
						Name = "?????";

					//if ( BaseObject.Name.GetType())
					//{

					//}
				}
				else
				{
					//check if old resource or standalone resource
					BaseObject = JsonConvert.DeserializeObject<RegistryBaseObject>( payload );
					CtdlType = BaseObject.CdtlType;
					Ctid = BaseObject.Ctid;
					Name = BaseObject.Name.ToString();
					if ( Name.IndexOf( "{" ) > -1 && Name.IndexOf( ":" ) > 1 )
					{
						int pos = Name.IndexOf( "\"", Name.IndexOf( ":" ) );
						int endpos = Name.IndexOf( "\"", pos + 1 );
						if ( pos > 1 && endpos > pos )
						{
							Name = Name.Substring( pos + 1, endpos - ( pos + 1 ) );
						}
					}
				}
				CtdlType = CtdlType.Replace( "ceterms:", "" );
				CtdlType = CtdlType.Replace( "ceasn:", "" );
			}
		}

		Dictionary<string, object> dictionary = new Dictionary<string, object>();

		public bool IsGraphObject { get; set; }
		public RegistryBaseObject BaseObject { get; set; } = new RegistryBaseObject();
		public string CtdlType { get; set; } = "";
		public string CtdlId { get; set; } = "";
		public string Ctid { get; set; } = "";
		public string Name { get; set; }
	}

	public class RegistryBaseObject
	{
		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		/// <summary>
		/// Type  of CTDL object
		/// </summary>
		[JsonProperty( "@type" )]
		public string CdtlType { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string Ctid { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
		public object Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public object Description { get; set; }

		[JsonProperty( PropertyName = "ceasn:name" )]
		public object CompetencyFrameworkName { get; set; }

		[JsonProperty( PropertyName = "ceasn:description" )]
		public object FrameworkDescription { get; set; }


		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; }

	}
}
