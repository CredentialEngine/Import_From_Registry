# Import_From_Registry
Sample project to import resources from the credential registry. While the code is also included for the [**Credential Finder**](https://credentialfinder.org/) site, the main focus of this repo is importing data from the [**Credential Registry**](https://credreg.net/). 

See the wiki for the current documentation: https://github.com/CredentialEngine/Import_From_Registry/wiki

## Preparation
### Sql Server
Install MS Sql Server 2016+.
https://www.microsoft.com/en-us/sql-server/sql-server-downloads 
### Visual Studio
Install Visual Studio Community 2019+
https://visualstudio.microsoft.com/vs/community/ 

## Updates
### August 30, 2022 Release Update
Uploaded a new release to the [**/release/2022-08-30 folder**](https://github.com/CredentialEngine/Import_From_Registry/tree/master/release/2022-08-30) with a new database backup.
This release includes the option to start fresh with an empty database using the provided backup file or to apply updates to an existing database created from the May 27, 2022 download. There is a Read.me file in the DatabaseUpdates folder with additional guidance. 

### May 27, 2022 Release Update
Generally when doing an import, all of the organizations should be done first. An issue was reported that can occur when credentials are imported before the organizations. An update was made to identity a possible issue and prevent any errors.

There were a number of database updates:
- Added Reference.Framework, and Reference.FrameworkItem which are replacing Reference.Frameworks (the latter has not been removed yet). Entity.ReferenceFramework had the property: ReferenceFrameworkId renamed to ReferenceFrameworkItemId. The latter is a foreign key to [Reference.FrameworkItem].Id
- CredentialStatusTypeId was added directly on the Credential table. Previously it was a generic property stored in Entity.Property.
- Codes.EntityTypes - add IsTopLevelEntity for all entities such as Credential, Organization, etc. that are top level resources in the registry (that is have a CTID and do not have a parent entity).

### May 17, 2022 Release Update
A connection string was missing from the April 27, 2022 release. 
The previous release zip file was removed, and a new one (**CTI.Import_22-04-26_UpdatedConfig.zip**) was added with the missing connection string for **ceExternalData**.

### April 27 2022
Uploaded a new release to the [**/release/2022-04-07 folder**](https://github.com/CredentialEngine/Import_From_Registry/tree/master/release/2022-04-07) with a new database backup.

### April 05 2022
Uploaded a new release to the [**/release/2022-04-05 folder**](https://github.com/CredentialEngine/Import_From_Registry/tree/master/release/2022-04-05) with a new database backup.

### December 03 2021
New: Compiled releases of the import will be added to a Release folder. 
- This process will simplify getting started.
- Just
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

### October 24, 2021
Updated Download project
- provided how to filter by the owning organization or a third party publisher

### October 15, 2021
- Import
	- added Job, and WorkRole
	- general updates
		- removed obsolete code
	- Updated Credential import:
		- DataSetProfile will be in a separate envelope, so credential.AggregateDataProfile will create a pending DSP record, until the full DSP is imported. 
	- CompetencyFramework import
		- added more detailed collection to the import even though not used by the Finder (detail page gets data directly from the Credential Registry)
- Credential Finder WebApi

### May 7, 2021
- Credential Finder WebApi
	- Detail Endpoints
		- Updates for organization, credential, assessment, and learning opportunity detail endpoints
		- Endpoints for on demand retrieval of Process Profiles, and Verification services
	- Search endpoints initialization:
		- retrieving all the filters for each search type
		- Autocomplete endpoints
		- Full search integration
	- Home Page endpoint
		- Retrieve totals for all top level documents (credentials, organizations, etc,)
- Import
	- Added Occupation 
	- AggregateDataProfile is effectively replacing the use of HoldersProfile, EarningsProfile, and EmploymentOutcomeProfile



### February 15, 2021
- Primarily updates for the Credential Finder WebApi organization detail endpoint
- As well, added handling of many new properties since last update


## Updates
### January 27, 2021
- Added new database backup for credFinder and ce_external
- Added handling of many new properties since last update

## Updates
### November 17, 2020
- Added new database backup from Sql Server 2016 (see **Database/credFinderGithub201117.zip**)
- a common Sql user of ceGithub is used in the applications. There is Sql in the restore Sql to create the user if necessary, and associate with a newly restored database. 
- Added handling of any new properties since last update

### February 10, 2020
- Updated to .Net 4.6.2
- Added new database backup from Sql Server 2016 (see **Database/credFinderGithub200207_SS2016.zip**)
- a common Sql user of ceGithub is used in the applications. There is Sql in the restore Sql to create the user if necessary, and associate with a newly restored database. 
- Added handling of any new properties since last update


## Quick Start
### Import
- Read the Wiki: https://github.com/CredentialEngine/Import_From_Registry/wiki
- Clone the code
- Provide your own keys for the Credential Engine Api key, and external Apis such as Google Maps 
- Unzip and restore the two required Sql Server databases (restore SQL is provided for both)
- Main database: Database/credFinderGithub200207_SS2016.zip  
- External Data: CE_ExternalData_190309.zip
- Open the solution and use Nuget to restore packages 

### Credential Finder
The solution includes the code for the Credential Finder site: https://credentialfinder.org/.
#### Elastic
The latter site uses Elastic for searching. The code to build and maintain the Elastic indices is included. 
Guidance for the installation and configuration of Elastic is not currently provided. 
#### Sql Server
By default the web site will use Sql server stored procedures for the simple searches. 
