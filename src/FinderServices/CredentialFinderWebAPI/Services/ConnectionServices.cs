using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using workIT.Factories;

namespace CredentialFinderWebAPI.Services
{
	public class ConnectionServices
	{
		public static bool DoesAssessmentHaveCredentialConnection( string assessmentCTID, ref string status )
		{
			string filter = string.Format( "(base.Id in (SELECT f.[Id]   FROM [dbo].[Assessment] a " +
				"  inner join [Entity.Assessment]    b on a.id = b.AssessmentId" +
				"  inner join Entity c on b.EntityId = c.Id" +
				"  Inner Join [Entity.ConditionProfile] d on c.EntityUid = d.rowId " +
				"  Inner Join Entity e on d.EntityId = e.Id " +
				"  inner join Credential f on e.EntityUid = f.RowId" +
				"  where f.EntityStateId > 1 and a.CTID is not null " +
				"  and a.CTID = '{0}' ))", assessmentCTID);
			int ptotalRows = 0;
			CredentialManager.ExistanceSearch( filter, ref ptotalRows );
			if ( ptotalRows  > 0)
				return true;
			else
			{
				//probably no message needed
				return false;
			}
		}

		public static bool DoesLearningOpportunityHaveCredentialConnection( string loppCTID, ref string status )
		{
			string filter = string.Format( "(base.Id in (SELECT f.[Id]   FROM [dbo].[LearningOpportunity] a " +
				"  inner join [Entity.LearningOpportunity]    b on a.id = b.LearningOpportunityId" +
				"  inner join Entity c on b.EntityId = c.Id" +
				"  Inner Join [Entity.ConditionProfile] d on c.EntityUid = d.rowId " +
				"  Inner Join Entity e on d.EntityId = e.Id " +
				"  inner join Credential f on e.EntityUid = f.RowId" +
				"  where f.EntityStateId > 1 and a.CTID is not null " +
				"  and a.CTID = '{0}' ))", loppCTID );
			int ptotalRows = 0;
			CredentialManager.ExistanceSearch( filter, ref ptotalRows );
			if ( ptotalRows > 0 )
				return true;
			else
			{
				//probably no message needed
				return false;
			}
		}
	}
}
