using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using workIT.Models;
using workIT.Utilities;

namespace workIT.Services
{
	public class FileReferenceServices
	{
		//Saves an image reference (default format = ImageFormat.Png)
		public static void SaveImageReference( FileReference data, Guid ownerRowID, int fileSizeLimitInBytes, bool allowOverwrites = true, int maxWidth = 0, int maxHeight = 0, ImageFormat format = null )
		{
			//Skip if no reference
			if ( data == null )
			{
				return;
			}

			//Prepare the reference
			PrepareFileReference( data, ownerRowID );
			try
			{
				//If there is a file to save...
				if ( !string.IsNullOrWhiteSpace( data.RawData ) )
				{
					using ( var updatedStream = new MemoryStream() )
					{
						//Set the format
						format = format ?? ImageFormat.Png;

						//Get saving data
						var savingDirectory = ConfigHelper.GetConfigValue( "widgetUploadPath", "" );
						var savingPathAndName = $"{savingDirectory}Widget_{ownerRowID}." + format.ToString().ToLower();

						//Ensure we can do anything
						if ( !allowOverwrites && File.Exists( savingPathAndName ) )
						{
							throw new Exception( "The file already exists." );
						}

						//Convert from data URL to byte[]
						var fileBytes = ConvertBas64DataURLToByteArray( data.RawData );
						if ( fileBytes == null || fileBytes.Length == 0 )
						{
							throw new Exception( "Error converting file from Base64 to byte[]" );
						}

						//Create the image object
						var image = Image.FromStream( new MemoryStream( fileBytes ) );

						//Resize the image down until it fits inside the specified max width and/or max height
						if ( ( maxWidth > 0 || maxHeight > 0 ) && image.Width > 0 && image.Height > 0 )
						{
							//resizeFactor = 1 / (currentValue / maxValue)
							var widthResizeFactor = maxWidth == 0 ? image.Width : ( double )1 / ( ( double )image.Width / ( double )maxWidth );
							var heightResizeFactor = maxHeight == 0 ? image.Height : ( double )1 / ( ( double )image.Height / ( double )maxHeight );

							//To mimic css background-size: contain, use the smaller of the two resizeFactors
							//but only if we care about the value of that axis
							var scaleBy = ( maxWidth > 0 && widthResizeFactor < heightResizeFactor ) ? widthResizeFactor :
								( maxHeight > 0 && heightResizeFactor < widthResizeFactor ) ? heightResizeFactor :
								1;

							//Determine the new sizes
							var newWidth = ( int )Math.Round( image.Width * scaleBy );
							var newHeight = ( int )Math.Round( image.Height * scaleBy );

							//Do the resize
							image = ( Image )new Bitmap( image, new Size( newWidth, newHeight ) );
						}

						//Ensure the resized image isn't too big
						using ( var fs = new FileStream( savingPathAndName, FileMode.Create, FileAccess.ReadWrite ) )
						{
							image.Save( updatedStream, format );
							if ( updatedStream.Length > fileSizeLimitInBytes )
							{
								throw new Exception( "The file is too large. (Must be under " + ( fileSizeLimitInBytes * 0.001 ) + "kb)." );
							}
							byte[] bytes = updatedStream.ToArray();
							fs.Write( bytes, 0, bytes.Length );
						}
					}
				}
			} catch (Exception ex)
			{
				LoggingHelper.LogError( ex, "FileReferenceServices.SaveImageReference" );
			}
			//If successful (or no file - e.g. an update to the label), save the reference
			//var toSave = Utilities.SimpleMap<DBM.FileReference>( data );
			//var saved = FileReferenceManager.SaveFileReference( toSave );

			//Return the saved reference
			//return GetByRowId( saved.RowId );
		}
		//
		private static void PrepareFileReference( FileReference reference, Guid ownerRowID )
		{
			//If it's a new reference...
			if ( reference.RowId == null || reference.RowId == Guid.Empty )
			{
				//Ensure there is a GUID and some basic info
				reference.RowId = Guid.NewGuid();
				reference.Created = DateTime.Now;
				reference.IsActive = true;
			}
			//If it's an existing reference...
			else
			{
				//Ensure that the owner is really the owner
				//var existing = GetByRowId( reference.RowId );
				//if ( existing.OwnerRowId != ownerRowID )
				//{
				//	throw new Exception( "You do not have permission to edit that file." );
				//}
			}

			//Ensure there is an extension
			if ( string.IsNullOrWhiteSpace( reference.FileExtension ) )
			{
				reference.FileExtension = "Unknown";
			}

			//Ensure these properties are populated
			reference.OwnerRowId = ownerRowID;
			//reference.ValueForProperty = propertyName;
			reference.LastUpdated = DateTime.Now;
		}
		public static void DeleteFile( Guid ownerRowID, string format )
		{
			//Validate the delete
			//if ( fileRowID == null || fileRowID == Guid.Empty )
			//{
			//	throw new Exception( "No file specified." );
			//}

			//var reference = GetByRowId( fileRowID );
			//if ( reference == null || reference.Id == 0 )
			//{
			//	throw new Exception( "Unable to find file: " + fileRowID.ToString() );
			//}

			//if ( ownerRowID != reference.OwnerRowId )
			//{
			//	throw new Exception( "You do not have permission to delete the file: " + fileRowID.ToString() );
			//}

			//Delete the file
			var savingDirectory = UtilityManager.GetAppKeyValue( "widgetUploadPath" );
			var savingPathAndName = $"{savingDirectory}Widget_{ownerRowID}." + format.ToLower();
			//var savingPathAndName = savingDirectory + fileRowID.ToString() + "." + FileExtension;

			if ( !File.Exists( savingPathAndName ) )
			{
				return;
			}

			try
			{
				File.Delete( savingPathAndName );
			}
			catch ( Exception ex )
			{
				throw new Exception( "Error deleting file: " + ex.Message );
			}

			//Delete the reference
			//FileReferenceManager.DeleteFileReference( reference.RowId );
		}
		public static byte[] ConvertBas64DataURLToByteArray( string dataURL )
		{
			//Convert the data from the base64 string to a byte[]
			var base64string = dataURL.Split( new string[] { ";base64," }, StringSplitOptions.RemoveEmptyEntries ).LastOrDefault() ?? "";
			var fileBytes = Convert.FromBase64String( base64string );
			return fileBytes;
		}
	}
}
