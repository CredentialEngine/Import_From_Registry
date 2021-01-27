using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	//public class FinancialAlignmentObject : BaseProfile
	//{
	//	public string AlignmentDate { get; set; }
	//	/// <summary>
	//	/// A category of alignment between the learning resource and the framework node.
	//	/// </summary>
	//	public int AlignmentTypeId { get; set; }
	//	public string AlignmentType { get; set; }
	//	//private string _alignmentType = "";
	//	//public string AlignmentType
	//	//{
	//	//	get { return _alignmentType; }
	//	//	set
	//	//	{
	//	//		if ( value.ToLower().Contains( "teaches" ) )
	//	//			value = "Teaches";
	//	//		else if ( value.ToLower().Contains( "requires" ) )
	//	//			value = "Requires";
	//	//		else if ( value.ToLower().Contains( "assesses" ) )
	//	//			value = "Assesses";
	//	//		else
	//	//		{
	//	//			//let it go
	//	//		}
	//	//		_alignmentType = value;
	//	//	}
	//	//}

	//	/// <summary>
	//	/// Framework URL
	//	/// The framework to which the resource being described is aligned.
	//	/// </summary>
	//	public string Framework { get; set; }
	//	public string FrameworkUrl { get { return Framework; } set { Framework = value; } } //Alias used for publishing

	//	/// <summary>
	//	/// The name of the framework to which the resource being described is aligned.
	//	/// Frameworks may include, but are not limited to, competency frameworks and concept schemes such as industry, occupation, and instructional program codes.
	//	/// </summary>
	//	public string FrameworkName { get; set; }


	//	/// <summary>
	//	/// The name of a node in an established educational framework.
	//	/// The name of the competency or concept targeted by the alignment.
	//	/// </summary>
	//	public string TargetNodeName { get; set; }

	//	/// <summary>
	//	/// Target Node - URI
	//	/// The node of a framework targeted by the alignment.
	//	/// </summary>
	//	public string TargetNode { get; set; }

	//	/// <summary>
	//	/// The description of a node in an established educational framework.
	//	/// </summary>
	//	public string TargetNodeDescription { get; set; }


	//	/// <summary>
	//	/// An asserted measurement of the weight, degree, percent, or strength of a recommendation, requirement, or comparison.
	//	/// </summary>
	//	public decimal Weight { get; set; }

	//	/// <summary>
	//	/// Coded Notation
	//	/// A short set of alpha-numeric symbols that uniquely identifies a resource and supports its discovery.
	//	/// </summary>
	//	public string CodedNotation { get; set; }
	//	public List<TextValueProfile> Auto_CodedNotation
	//	{
	//		get
	//		{
	//			var result = new List<TextValueProfile>();
	//			if ( !string.IsNullOrWhiteSpace( CodedNotation ) )
	//			{
	//				result.Add( new TextValueProfile() { TextValue = CodedNotation } );
	//			}
	//			return result;
	//		}
	//	}
	//}
	//
	public class FinancialAssistanceProfile: BaseProfile
	{

		public string Name {
			get { return this.ProfileName;  }
			set { this.ProfileName = value; } 
		}

		/// <summary>
		/// SubjectWebpage - URI
		/// </summary>
		public string SubjectWebpage { get; set; }

		public Enumeration FinancialAssistanceType { get; set; } = new Enumeration();

		public List<QuantitativeValue> FinancialAssistanceValue { get; set; } = new List<QuantitativeValue>();

		public string FinancialAssistanceValueJson { get; set; }

		public List<string> FinancialAssistanceValueSummary { get; set; } = new List<string>();
	}


}
