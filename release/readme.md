# Releases History
## June 30, 2024
We apolgize for the long delay between releases. 
See specific details in the release [**README**](https://github.com/CredentialEngine/Import_From_Registry/blob/master/release/2024-06-30/readme.md)

## September 14, 2023
We apolgize for the long delay between releases. 
See specific details in the release [**README**](https://github.com/CredentialEngine/Import_From_Registry/blob/master/release/2023-09-13/readme.md)

Added a new backup for CE_ExternalData. Recall that this database must always be present with the credential finder database.
	CE_ExternalData_backup_2023_09_14.zip

# August 30, 2022 Release Update
Updates
- Added collections, and progression model
- Added LifeCycleStatusTypeId to learning opportunity, assessment, transfer value, and organization

# May 17, 2022 Release Update
A connection string was missing from the April 27, 2022 release. 
The previous release zip file was removed, and a new one (**CTI.Import_22-04-26_UpdatedConfig.zip**) was added with the missing connection string for **ceExternalData**.
 
## December 03 2021
We are using a new or alternate approach for using the import application. 
Compiled releases of the import will be added to a Release folder. 
We will be providing regular releases of the application - compiled and ready to run. 
This approach will ease the set up and running of the application, and still enable some customization for the current requirements of a partner. 

- This process will simplify getting started.
### Steps
	- download the contents of the latest release folder
	- unzip into a folder such as C:\CredentialRegistryImport
	- download and install the database backups
	- NOTES	
		- each release folder will contain a file containing a zip of the compiled application and a zip of the related backup file
		- a SQL script to restore the backup will be in the Release root folder (Restore_credFinder.sql)
		- The base release folder will contain a zip of the CE_External database. This is independent of releases and will not change frequently.  
	- refer to the documentation regarding the app.config file. In the compiled application, this will be CTI.Import.exe.config.
		- the config file has a description of the purpose of the main application keys
		- the main updates will be for your Credential Registry API key and the database related connection strings
		- after configuring the latter keep a copy outside the current downloaded release. 
		- then when another release is downloaded, the saved config file can just be copied into the new application's folder (after checking if there were any new AppKey entries!). 

Suggested Folder Structure
- c:\CredentialRegistryImport
  - data	(could use to store the Sql Server databases)
  - backups 	(could store the backup databases and restore sql here)
  -  Import 	(store releases)
- c:\\@logs - the default folder for application log files. Update the config file if a different folder is to be used (perhaps c:\CredentialRegistryImport\logs).



