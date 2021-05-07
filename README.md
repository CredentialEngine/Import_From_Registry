# Import_From_Registry
Sample project to import resources from the credential registry. While the code is also included for the [**Credential Finder**](https://credentialfinder.org/) site, the main focus of this repo is importing data from the [**Credential Registry**](https://credreg.net/). 

See the wiki for the current documentation: https://github.com/CredentialEngine/Import_From_Registry/wiki

## Updates
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
