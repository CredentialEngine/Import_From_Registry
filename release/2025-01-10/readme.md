# Releases
# January 10, 2025 Release
The primary purpose of this release is for updates related to outcome data. 

## Application Config
Updates
- Many updates for the metric related classes of: Industry, Metric, DataSetProfile, DataSetProfile.DataSetDistribution, DataSetProfile.DataSetService, DataSetProfile.Dimension, DataSetProfile.Observation
- Updated many tables (see DatabaseUpdates\2. Table-updates\individualUpdates)

New Classes
- Classes for the new and updated tables of course. 

## Database
A new backup of the credFinder database is available. 

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




