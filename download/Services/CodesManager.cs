using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Download.Services
{
	public class CodesManager
	{
		#region constants - entity types. 
		//An Entity is typically created only where it can have a child relationship, ex: Entity.Property
		public static int ENTITY_TYPE_CREDENTIAL	= 1;
		public static int ENTITY_TYPE_ORGANIZATION	= 2; //what about QACred
		public static int ENTITY_TYPE_ASSESSMENT_PROFILE = 3;
		public static int ENTITY_TYPE_CONNECTION_PROFILE = 4;
		public static int ENTITY_TYPE_CONDITION_PROFILE = 4;
		public static int ENTITY_TYPE_COST_PROFILE	= 5;
		public static int ENTITY_TYPE_COST_PROFILE_ITEM = 6;
		public static int ENTITY_TYPE_LEARNING_OPP_PROFILE = 7;
		public static int ENTITY_TYPE_PATHWAY		= 8;
		public static int ENTITY_TYPE_RUBRIC		= 9;

		public static int ENTITY_TYPE_COMPETENCY_FRAMEWORK = 10;
		public static int ENTITY_TYPE_CONCEPT_SCHEME = 11;

		public static int ENTITY_TYPE_REVOCATION_PROFILE = 12;
		public static int ENTITY_TYPE_VERIFICATION_PROFILE = 13;
		public static int ENTITY_TYPE_PROCESS_PROFILE = 14;
		public static int ENTITY_TYPE_CONTACT_POINT = 15;
		public static int ENTITY_TYPE_ADDRESS_PROFILE = 16;
		public static int ENTITY_TYPE_CASS_COMPETENCY_FRAMEWORK = 17;
		public static int ENTITY_TYPE_JURISDICTION_PROFILE = 18;
		public static int ENTITY_TYPE_CONDITION_MANIFEST = 19;
		public static int ENTITY_TYPE_COST_MANIFEST = 20;
		public static int ENTITY_TYPE_FINANCIAL_ASST_PROFILE = 21;

		//
		public static int ENTITY_TYPE_PATHWAY_SET = 23;
		public static int ENTITY_TYPE_PATHWAY_COMPONENT = 24;
		public static int ENTITY_TYPE_COMPONENT_CONDITION = 25;
		public static int ENTITY_TYPE_TRANSFER_VALUE_PROFILE = 26;
		//
		public static int ENTITY_TYPE_AGGREGATE_DATA_PROFILE = 27;
		public static int ENTITY_TYPE_TRANSFER_INTERMEDIARY = 28;

		public static int ENTITY_TYPE_DATASET_PROFILE = 31;
		//
		public static int ENTITY_TYPE_JOB_PROFILE = 32;
		public static int ENTITY_TYPE_TASK_PROFILE = 33;
		public static int ENTITY_TYPE_WORKROLE_PROFILE = 34;
		public static int ENTITY_TYPE_OCCUPATIONS_PROFILE = 35;
		public static int ENTITY_TYPE_LEARNING_PROGRAM = 36;
		public static int ENTITY_TYPE_COURSE = 37;




		#endregion
	}
}
