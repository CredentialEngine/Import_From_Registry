# Import_From_Registry
Sample project to import resources from the credential registry

See the wiki for the current documentation: https://github.com/CredentialEngine/Import_From_Registry/wiki

# Updates
## Febuary 10, 2020
- Updated to .Net 4.6.2
- Added new database backup from Sql Server 2016 (see **Database/credFinderGithub200207_SS2016.zip**)
- a common Sql user of ceGithub is used in the applications. There is Sql in the restore Sql to create the user if necessary, and associate with a newly restored database. 
- Added handling of any new properties since last update


# Quick Start
- Read the Wiki: https://github.com/CredentialEngine/Import_From_Registry/wiki
- Clone the code
- Unzip and restore the two required Sql Server databases (restore SQL is provided for both)
- Main database: Database/credFinderGithub200207_SS2016.zip  
- External Data: CE_ExternalData_190309.zip
- Open the solution and use Nuget to restore packages 
