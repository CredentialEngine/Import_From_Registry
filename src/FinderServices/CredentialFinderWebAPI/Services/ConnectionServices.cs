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
				"  where a.EntityStateId >= 0 and f.EntityStateId > 1  " +
				"  and a.CTID = '{0}' ))", assessmentCTID);
			int ptotalRows = 0;
			CredentialManager.ExistanceSearch( filter, ref ptotalRows );
			if ( ptotalRows  > 0)
				return true;
			else
			{
				//fallback. If the pending entry exists, it to be put there for a valid reason - but could have been a non-credential connection.
				var lopp = AssessmentManager.GetSummaryByCtid( assessmentCTID );
				if ( lopp != null && lopp.Id > 0 )
				{
					return true;
				}
				//probably no message needed
				return false;
			}
		}

		/// <summary>
		/// Check if the lopp for the passed credential has a connnection to a credential
		/// 21-03-02 mparsons - had an issue where an lopp was previously deleted, and so had a state of 0. Since the reason for this check is to publish an lopp, now including deleted lopps in the check
		/// </summary>
		/// <param name="loppCTID"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public static bool DoesLearningOpportunityHaveCredentialConnection( string loppCTID, ref string status )
		{
			string filter = string.Format( "(base.Id in (SELECT f.[Id]   FROM [dbo].[LearningOpportunity] a " +
				"  inner join [Entity.LearningOpportunity]    b on a.id = b.LearningOpportunityId" +
				"  inner join Entity c on b.EntityId = c.Id  " +
				"  Inner Join [Entity.ConditionProfile] d on c.EntityUid = d.rowId " +
				"  Inner Join Entity e on d.EntityId = e.Id " +
				"  inner join Credential f on e.EntityUid = f.RowId" +
				"  where a.EntityStateId >= 0 and f.EntityStateId > 1  " +
				"  and a.CTID = '{0}' ))", loppCTID );
			int ptotalRows = 0;
			CredentialManager.ExistanceSearch( filter, ref ptotalRows );
			if ( ptotalRows > 0 )
				return true;
			else
			{
				//fallback. If the pending entry exists, it was be put there for a valid reason - but could have been a non-credential connection.
				var lopp = LearningOpportunityManager.GetByCtid( loppCTID );
				if (lopp != null && lopp.Id > 0 )
				{
					return true;
				}
				//probably no message needed
				return false;
			}
		}
	}
}
