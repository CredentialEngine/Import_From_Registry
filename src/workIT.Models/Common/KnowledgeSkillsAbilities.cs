using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{

	[Serializable]
	public class KnowledgeSkillsAbilities
	{
		public KnowledgeSkillsAbilities()
		{
		}
		public int ParentEntityId { get; set; }


		public int KSATypeId { get; set; }
		//????????????
		public Entity TargetEntity { get; set; }
		

		public int TargetEntityTypeId { get; set; }
		public Guid TargetEntityUid { get; set; }

		//may not be applicable
		public int TargetEntityBaseId { get; set; }
		public string TargetEntityName { get; set; }
		public string TargetEntityType { get; set; }
		public string TargetEntitySubjectWebpage { get; set; }
		public int TargetEntityStateId { get; set; }
		public string TargetCTID { get; set; }



	}
}
