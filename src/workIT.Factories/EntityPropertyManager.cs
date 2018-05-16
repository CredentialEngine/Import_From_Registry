using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using EM = workIT.Data;
using workIT.Utilities;

using workIT.Data.Views;

using ThisEntity = workIT.Models.Common.Enumeration;
using DBEntity = workIT.Data.Tables.Entity_Property;
using ViewContext = workIT.Data.Views.workITViews;
using EntityContext = workIT.Data.Tables.workITEntities;

namespace workIT.Factories
{
	public class EntityPropertyManager : BaseFactory
	{
		string thisClassName = "EntityPropertyManager";
		#region Entity property persistance ===================


		/// <summary>
		/// Update Entity properies
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="parentTypeId">NOt used directly, useful for messages</param>
		/// <param name="categoryId">This could be part of the entity, just need to confirm</param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool AddProperties( Enumeration entity, Guid parentUid, int parentTypeId, int categoryId, bool isRequired, ref SaveStatus status )
		{
			bool isAllValid = true;
			int updatedCount = 0;
			int count = 0;

			if ( !IsGuidValid(parentUid) )
			{
				status.AddError("A valid identifier was not provided to the Update method.");
				return false;
			}
			if ( entity == null )
			{
				entity = new Enumeration();
				return true;
			}
			//get parent entity
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}
			string schemaName = "";
			using ( var context = new EntityContext() )
			{
				DBEntity op = new DBEntity();
				
				foreach (var item in entity.Items )
				{
					schemaName = "";
					if ( !string.IsNullOrWhiteSpace( item.SchemaName ) )
						schemaName = item.SchemaName;
					else if ( !string.IsNullOrWhiteSpace( item.Name ) )
						schemaName = item.Name;

					if (!string.IsNullOrWhiteSpace(schemaName))
					{
						CodeItem code = CodesManager.Codes_PropertyValue_GetBySchema( categoryId, schemaName );
						if ( code != null && code.Id > 0)
						{
							op = new DBEntity();
							op.EntityId = parent.Id;
							op.PropertyValueId = code.Id;
							op.Created = System.DateTime.Now;

							context.Entity_Property.Add( op );
							count = context.SaveChanges();
							if ( count == 0 )
							{
								status.AddWarning( string.Format( thisClassName + ".AddProperties(). Unable to add property value Id of: {0} for categoryId: {1}, parentTypeId: {2}  ", code.Id, categoryId, parentTypeId ) );
								isAllValid = false;
							}
							else
								updatedCount++;
						}
						else
						{
							//document invalid schema
							status.AddWarning( string.Format( thisClassName + ".AddProperties(). Invalid schema name encountered of: {0} for categoryId: {1}, parentTypeId: {2}. IGNORED  ", schemaName, categoryId, parentTypeId ) );
							//isAllValid = false;
						}
					} else
					{
						//document invalid schema
						status.AddWarning( string.Format( thisClassName + ".AddProperties(). Invalid to schema name encountered of: {0} for categoryId: {1}, parentTypeId: {2}  ", schemaName, categoryId, parentTypeId ) ); 
						isAllValid = false;
					}
				}
			}

			if (updatedCount == 0 && isRequired )
			{
				//document invalid schema
				status.AddError( string.Format( thisClassName + ".AddProperties(). Error an property is required for categoryId: {0}  ", categoryId ) );
				isAllValid = false;
			}

			return isAllValid;
		}


		#endregion
		#region Entity property read ===================
		public static Enumeration FillEnumeration( Guid parentUid, int categoryId )
		{
			Enumeration entity = new ThisEntity();
			entity = CodesManager.GetEnumeration( categoryId );

			entity.Items = new List<EnumeratedItem>();
			EnumeratedItem item = new EnumeratedItem();

			using ( var context = new ViewContext() )
			{
				List<EntityProperty_Summary> results = context.EntityProperty_Summary
					.Where( s => s.EntityUid == parentUid
						&& s.CategoryId == categoryId )
					.OrderBy( s => s.CategoryId ).ThenBy( s => s.SortOrder ).ThenBy( s => s.Property )
					.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( EntityProperty_Summary prop in results )
					{

						item = new EnumeratedItem();
						item.Id = prop.PropertyValueId;
						item.Value = prop.PropertyValueId.ToString();
						item.Selected = true;

						item.Name = prop.Property;
						item.SchemaName = prop.PropertySchemaName;
						entity.Items.Add( item );
					}
				}
			}

			return entity;
		}
		#endregion

	}
}