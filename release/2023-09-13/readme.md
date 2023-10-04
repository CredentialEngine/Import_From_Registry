# Releases
# September 13, 2023 Release

## Application Config
Updates
- Upgrade from 4.6.2 to 4.7.2.Net
- Added a new backup for CE_ExternalData. Recall that this database must always be present with the credential finder database.
	CE_ExternalData_backup_2023_09_14.zip

New Classes
- Verification Service Profile is now a top level class (with a CTID)
- SupportService
- ScheduledOffering
- Version 1 for Job, Occupation, WorkRole, Task

## Application Config


## Logging

Reminder: There are options to save documents downloaded from the Credential Registry as part of an import. 
Set the key: savingDocumentToFileSystem to true to have documents saved. These documents will be saved in the folder and using the file name template specified by the key: path.log.file.
You can control whether to save a file by logFileTraceLevel. If the latter is greater than appTraceLevel, files will not be saved.



Suggested Folder Structure
- c:\CredentialRegistryImport
  - data	(could use to store the Sql Server databases)
  - backups 	(could store the backup databases and restore sql here)
  -  Import 	(store releases)
- c:\\@logs - the default folder for application log files. Update the config file if a different folder is to be used (perhaps c:\CredentialRegistryImport\logs).



