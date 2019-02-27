using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{

    /// <summary>
    /// CredentialAlignmentObjectProfile - for handling flattened view, especially from import
    /// </summary>
    [Serializable]
    public class CredentialAlignmentObjectProfile : BaseProfile
	{
		public CredentialAlignmentObjectProfile()
		{
			//Items = new List<CredentialAlignmentObjectItem>();
		}

		public int EducationFrameworkId { get; set; }

        public string FrameworkUri { get; set; }
        public string FrameworkCtid { get; set; }

        public string SourceUrl { get; set; }
        public string FrameworkName { get; set; }

		public string CodedNotation { get; set; }
		public string TargetNodeName { get; set; }
        public string TargetNodeName_Map { get; set; }
		public string TargetNodeDescription
		{
			get { return this.Description; }
			set { this.Description = value; }
		}
		public string TargetNodeDescription_Map { get; set; }
		public string TargetNode { get; set; }
		//public string Weight { get; set; }
		public decimal Weight { get; set; }
        //public List<CredentialAlignmentObjectItem> Items { get; set; }
       
    }
	//
	//public class CredentialAlignmentObject : CredentialAlignmentObjectProfile
	//{
	//}
	/* Split Profiles */
	public class CredentialAlignmentObjectFrameworkProfile : BaseProfile
	{
		public CredentialAlignmentObjectFrameworkProfile()
		{
			Items = new List<CredentialAlignmentObjectItem>();
		}

		/// <summary>
		/// Name of the framework
		/// </summary>
		public string FrameworkName { get; set; }
		/// <summary>
		/// Url for the framework
        /// TODO - inconsistancies between the urls???
		/// </summary>
		//public string FrameworkUrl { get; set; }
        public string SourceUrl { get; set; }
        public string FrameworkUri { get; set; }
        public string FrameworkCtid { get; set; }
		public string CaSSViewerUrl { get; set; }
		public bool IsARegistryFrameworkUrl
		{
			get
			{
				if ( string.IsNullOrWhiteSpace( FrameworkUri ) )
					return false;
				else if ( FrameworkUri.ToLower().IndexOf( "credentialengineregistry.org/resources/ce-" ) > -1
					|| FrameworkUri.ToLower().IndexOf( "credentialengineregistry.org/graph/ce-" ) > -1 )
					return true;
				else
					return false;
			}
		}
		public RegistryImport FrameworkPayload { get; set; } = new RegistryImport();

        public List<CredentialAlignmentObjectItem> Items { get; set; }
        //used by detail page to combine all CAOs frameworks
		public static List<CredentialAlignmentObjectProfile> FlattenAlignmentObjects( List<CredentialAlignmentObjectFrameworkProfile> data )
		{
			var result = new List<CredentialAlignmentObjectProfile>();
            CredentialAlignmentObjectProfile entity = new CredentialAlignmentObjectProfile();

            foreach ( var framework in data )
			{
				foreach ( var item in framework.Items )
				{
                    entity = new CredentialAlignmentObjectProfile()
                    {

                        //Framework = string.IsNullOrWhiteSpace( framework.FrameworkName ) ? framework.FrameworkName : framework.FrameworkUrl,
                        FrameworkName = framework.FrameworkName,
                        //FrameworkUrl = framework.FrameworkUrl,
                        TargetNodeName = item.TargetNodeName,
                        TargetNodeDescription = item.TargetNodeDescription,
                        //TargetUrl = string.IsNullOrWhiteSpace( item.TargetUrl ) ? framework.EducationalFrameworkUrl : item.TargetUrl,
                        TargetNode = item.TargetNode,
                        ProfileName = item.ProfileName,
                        Description = item.Description,
                        CodedNotation = item.CodedNotation,
                        Weight = item.Weight
                    };
                    if ( !string.IsNullOrWhiteSpace( framework.FrameworkUri ) )
                        entity.FrameworkUri = framework.FrameworkUri;
                    else
                        entity.SourceUrl = framework.SourceUrl;

                    result.Add( entity );
				}
			}

			return result;
		}
		//
		//public static List<CredentialAlignmentObjectFrameworkProfile> ExpandAlignmentObjects( List<CredentialAlignmentObjectProfile> data )
		//{
		//	var result = new List<CredentialAlignmentObjectFrameworkProfile>();
		//	foreach ( var item in data )
		//	{
		//		var currentFramework = result.FirstOrDefault( m => m.FrameworkName == item.FrameworkName || m.FrameworkUrl == item.SourceUrl );
		//		if ( currentFramework == null )
		//		{
		//			currentFramework = new CredentialAlignmentObjectFrameworkProfile()
		//			{
		//				FrameworkName = item.FrameworkName,
		//				FrameworkUrl = item.SourceUrl
		//			};
		//			result.Add( currentFramework );
		//		}
		//		currentFramework.Items.Add( new CredentialAlignmentObjectItem()
		//		{
		//			TargetNodeDescription = item.TargetNodeDescription,
		//			TargetNodeName = item.TargetNodeName,
		//			TargetNode = item.TargetNode,
		//			CodedNotation = item.CodedNotation
		//		} );
		//	}
		//	return result;
		//}
		//
	}
	public class CredentialAlignmentObjectItem : BaseProfile
    {
        /// <summary>
        /// Url for the competency
        /// </summary>
        public string TargetNode { get; set; }
        public string TargetNodeName { get; set; }
        public string TargetNodeDescription
		{
			get { return this.Description; }
			set { this.Description = value; }
		}

        public string CodedNotation { get; set; }

		//public string Weight { get; set; }
		public decimal Weight { get; set; }
		//public string AlignmentDate { get; set; }


		//primarily available from a search

		public int ConnectionTypeId { get; set; }
        public int SourceEntityTypeId { get; set; }
        public int SourceParentId { get; set; }

		public string FrameworkName { get; set; }
		public string FrameworkUri { get; set; }

		//public int AlignmentTypeId { get; set; }
		//public string AlignmentType { get; set; }

		//for use with CASS comps, initially
		//public int CompetencyId { get; set; }
		//public string RepositoryUri { get; set; }

	}	//
}
