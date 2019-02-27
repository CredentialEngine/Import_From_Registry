using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using workIT.Models.ProfileModels;
using workIT.Models.Common;
using EM = workIT.Data.Tables;
using workIT.Utilities;
using Views = workIT.Data.Views;
using ViewContext = workIT.Data.Views.workITViews;

namespace workIT.Factories
{
	public class OrganizationServiceManager : BaseFactory
	{
		//public bool OrganizationService_Update( Organization entity, bool isAdd, ref string statusMessage )
		//{
		//	bool isAllValid = true;
		//	int updatedCount = 0;
		//	statusMessage = "";
		//	if ( entity.Id == 0 )
		//	{
		//		statusMessage = "A valid organization identifier was not provided to the Organization_UpdateParts method.";
		//		return false;
		//	}
		//	if ( entity.ServiceType == null )
		//		entity.ServiceType = new Enumeration();
		//	//for an update, we need to check for deleted Items, or just delete all and readd
		//	//==> then interface would have to always return everything
		//	//TODO - need to add code for handling the separate other data
		//	using ( var context = new ViewContext() )
		//	{
		//		EM.Organization_Services op = new EM.Organization_Services();

		//		//get all existing for org type
		//		var results = context.Organization_ServiceSummary
		//					.Where( s => s.OrganizationId == entity.Id )
		//					.OrderBy( s => s.Service )
		//					.ToList();

		//		#region deletes/updates check
		//		var deleteList = from existing in results
		//						 join item in entity.ServiceType.Items
		//								 on existing.CodeId equals item.Id
		//								 into joinTable
		//						 from result in joinTable.DefaultIfEmpty( new EnumeratedItem { SchemaName = "missing", Id = 0 } )
		//						 select new { DeleteId = existing.OrgServiceId, ItemId = ( result.Id ) };

		//		foreach ( var v in deleteList )
		//		{

		//			if ( v.ItemId == 0 )
		//			{
		//				//delete item
		//				if ( OrganizationService_Delete( v.DeleteId, ref statusMessage ) == false )
		//				{
		//					//EM.Organization_Service p = context.Organization_Service.FirstOrDefault( s => s.Id == v.DeleteId );
		//					//if ( p != null && p.Id > 0 )
		//					//{
		//					//	context.Organization_Service.Remove( p );
		//					//	count = context.SaveChanges();
		//					//}
		//					//else
		//					//{
		//					//	statusMessage = string.Format( "Organization_Service record was not found: {0}", v.DeleteId );
		//						isAllValid = false;
		//					//}
		//				}
		//			}
		//		}
		//		#endregion

		//		#region new items
		//		//should only be empty ids, where not in current list, so should be adds
		//		//Item.Id is actually the code value, so compare to the codeId, not the PK id
		//		var newList = from item in entity.ServiceType.Items
		//					  join existing in results
		//							on item.Id equals existing.CodeId
		//							into joinTable
		//					  from addList in joinTable.DefaultIfEmpty( new Views.Organization_ServiceSummary { OrgServiceId = 0, CodeId = 0 } )
		//					  select new { AddId = item.Id, ExistingId = addList.CodeId };
		//		foreach ( var v in newList )
		//		{
		//			if ( v.ExistingId == 0 )
		//			{
		//				int id = OrganizationService_Add( entity.Id, v.AddId, entity.LastUpdatedById );

		//				if ( id == 0 )
		//				{
		//					statusMessage += string.Format( " Unable to add service value Id of: {0} <br\\> ", v.AddId );
		//					isAllValid = false;
		//				}
		//				else
		//					updatedCount++;
		//			}
		//		}
		//		#endregion

		//		//not sure there will be an other!
		//		// 16-09-02 mp - handled in the main Org.FromMap
		//		//entity.ServiceType.ParentId = entity.Id;
		//		////HACK ALERT!!! - only way to handle
		//		//if ( entity.ServiceType.Id == 0 )
		//		//	entity.ServiceType.Id = CodesManager.PROPERTY_CATEGORY_ORG_SERVICE;
		//		//new OrganizationPropertyManager().
		//		//PropertyOther_Update( entity.ServiceType, entity.LastUpdatedById, ref statusMessage );
		//	}


		//	return isAllValid;
		//}
		//private int OrganizationService_Add( int entityId, int propertyValueId, int userId )
		//{
		//	int id = 0;
		//	using ( var context = new EntityContext() )
		//	{
		//		EM.Organization_Service op = new EM.Organization_Service();
		//		op.OrganizationId = entityId;
		//		op.CodeId = propertyValueId;
		//		op.Created = System.DateTime.Now;
		//		op.CreatedById = userId;
		//		//op.LastUpdated = System.DateTime.Now;
		//		//op.LastUpdatedById = entity.LastUpdatedById;

		//		context.Organization_Service.Add( op );
		//		int count = context.SaveChanges();

		//		id = op.Id;

		//	}

		//	return id;
		//}
		//public bool OrganizationService_Delete( int recordId, ref string statusMessage )
		//{
		//	bool isOK = true;
		//	using ( var context = new EntityContext() )
		//	{

		//		//delete item
		//		EM.Organization_Service p = context.Organization_Service.FirstOrDefault( s => s.Id == recordId );
		//		if ( p != null && p.Id > 0 )
		//		{
		//			context.Organization_Service.Remove( p );
		//			int count = context.SaveChanges();
		//		}
		//		else
		//		{
		//			statusMessage = string.Format( "Organization_Service record was not found: {0}", recordId ) ;
		//			isOK = false;
		//		}
		//	}

		//	return isOK;
		//}
		/// <summary>
		/// Fill organization services
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		//public static void FillOrganizationService( EM.Organization from, Organization to )
		//{
		//	to.ServiceType = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ORG_SERVICE );

		//	to.ServiceType.ParentId = to.Id;
		//	to.ServiceType.Items = new List<EnumeratedItem>();
		//	EnumeratedItem item = new EnumeratedItem();
		//	//TODO - change to use Entity.Property
		//	foreach ( EM.Organization_Service prop in from.Organization_Service )
		//	{
		//		item = new EnumeratedItem();
		//		//this should be the prop.Id, but client code seems to be using for different purpose
		//		item.Id = prop.CodeId;
		//		item.RecordId = prop.Id;
		//		item.CodeId = prop.CodeId;
		//		item.Value = prop.CodeId.ToString();
		//		item.Name = prop.Codes_AgentService.Title;
		//		item.SchemaName = prop.Codes_AgentService.SchemaTag;

		//		item.Description = prop.Codes_AgentService.Description;

		//		item.Selected = true;
		
		//		to.ServiceType.Items.Add( item );


		//	}

		//}

			#region role codes retrieval ==================
		public static Enumeration GetOrgServices( bool getAll = true )
        {
            Enumeration entity = new Enumeration();

            using ( var context = new Data.Views.workITViews() )
            {
                EM.Views.Codes_PropertyCategory category = context.Codes_PropertyCategory
                            .SingleOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_ORG_SERVICE );

                if ( category != null && category.Id > 0 )
                {
                    entity.Id = category.Id;
                    entity.Name = category.Title;
                    entity.Description = category.Description;
                    entity.SchemaName = category.SchemaName;
                    entity.Url = category.SchemaUrl;
                    entity.Items = new List<EnumeratedItem>();

                    EnumeratedItem val = new EnumeratedItem();

                    //.Where( s => s.IsActive == true && (s.Totals > 0 || getAll )))
                    //List<EM.Tables.Codes_AgentService> list = context.Codes_AgentService
                    //        .Where( s => s.IsActive == true && s.Totals > 0 )
                    //        .OrderBy( s => s.Title )
                    //        .ToList();

                    //foreach ( EM.Codes_AgentService item in list )
                    //{
                    //    val = new EnumeratedItem();
                    //    val.Id = item.Id;
                    //    //??
                    //    val.CodeId = item.Id;
                    //    val.Name = item.Title;
                    //    val.Description = item.Description != null ? item.Description : "";
                    //    val.Totals = ( int )item.Totals;

                    //    val.SchemaName = item.SchemaTag != null ? item.SchemaTag : "";
                    //    //val.SchemaUrl = item.SchemaUrl;
                    //    val.Value = item.Id.ToString();
                    //    if ( item.IsQAService ?? false )
                    //    {
                    //        val.IsSpecialValue = true;
                    //        if ( IsDevEnv() )
                    //            val.Name += " (QA)";
                    //    }

                    //    val.Description = item.Description;
                    //    val.ParentId = category.Id;
                    //    entity.Items.Add( val );
                    //}
                }
            }

            return entity;
        }
        #endregion
    }
}
