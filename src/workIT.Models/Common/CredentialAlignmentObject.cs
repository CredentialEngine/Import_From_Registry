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
	public class CredentialAlignmentObjectProfile : BaseProfile
	{
		public CredentialAlignmentObjectProfile()
		{
			//Items = new List<CredentialAlignmentObjectItem>();
		}

		public int EducationFrameworkId { get; set; }
		public string FrameworkUrl { get; set; }
		public string FrameworkName { get; set; }

		public string CodedNotation { get; set; }
		public string TargetNodeName { get; set; }
		public string TargetNodeDescription { get; set; }
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
		/// </summary>
		public string FrameworkUrl { get; set; }

		public List<CredentialAlignmentObjectItem> Items { get; set; }
		public static List<CredentialAlignmentObjectProfile> FlattenAlignmentObjects( List<CredentialAlignmentObjectFrameworkProfile> data )
		{
			var result = new List<CredentialAlignmentObjectProfile>();

			foreach ( var framework in data )
			{
				foreach ( var item in framework.Items )
				{
					result.Add( new CredentialAlignmentObjectProfile()
					{

						//Framework = string.IsNullOrWhiteSpace( framework.FrameworkName ) ? framework.FrameworkName : framework.FrameworkUrl,
						FrameworkName = framework.FrameworkName,
						FrameworkUrl = framework.FrameworkUrl,
						TargetNodeName = item.TargetNodeName,
						TargetNodeDescription = item.TargetNodeDescription,
						//TargetUrl = string.IsNullOrWhiteSpace( item.TargetUrl ) ? framework.EducationalFrameworkUrl : item.TargetUrl,
						TargetNode = item.TargetNode,
						ProfileName = item.ProfileName,
						Description = item.Description,
						CodedNotation = item.CodedNotation,
						Weight = item.Weight
					} );
				}
			}

			return result;
		}
		//
		public static List<CredentialAlignmentObjectFrameworkProfile> ExpandAlignmentObjects( List<CredentialAlignmentObjectProfile> data )
		{
			var result = new List<CredentialAlignmentObjectFrameworkProfile>();
			foreach ( var item in data )
			{
				var currentFramework = result.FirstOrDefault( m => m.FrameworkName == item.FrameworkName || m.FrameworkUrl == item.FrameworkUrl );
				if ( currentFramework == null )
				{
					currentFramework = new CredentialAlignmentObjectFrameworkProfile()
					{
						FrameworkName = item.FrameworkName,
						FrameworkUrl = item.FrameworkUrl
					};
					result.Add( currentFramework );
				}
				currentFramework.Items.Add( new CredentialAlignmentObjectItem()
				{
					TargetNodeDescription = item.TargetNodeDescription,
					TargetNodeName = item.TargetNodeName,
					TargetNode = item.TargetNode,
					CodedNotation = item.CodedNotation
				} );
			}
			return result;
		}
		//
	}
	public class CredentialAlignmentObjectItem : BaseProfile
    {
        /// <summary>
        /// Url for the competency
        /// </summary>
        public string TargetNode { get; set; }
        public string TargetNodeName { get; set; }
        public string TargetNodeDescription { get; set; }

        public string CodedNotation { get; set; }

		//public string Weight { get; set; }
		public decimal Weight { get; set; }
		//public string AlignmentDate { get; set; }


		//primarily available from a search

		public int ConnectionTypeId { get; set; }
        public int SourceEntityTypeId { get; set; }
        public int SourceParentId { get; set; }

        //public int AlignmentTypeId { get; set; }
        //public string AlignmentType { get; set; }

        //for use with CASS comps, initially
        public int CompetencyId { get; set; }
        public string RepositoryUri { get; set; }
        
    }	//
}
