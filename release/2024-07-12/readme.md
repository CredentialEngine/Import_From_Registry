# Releases
# July 12, 2024 Release

## Application Config
Updates
- Many updates for the work related classes of Job, Occupation, Task, and workRole
- Updated many tables (see DatabaseUpdates\2. Table-updates\individualUpdates)

New Classes
- Rubric
- CredentialingAction (QA Actions)

## Database
All updates can be found in \DatabaseUpdates.zip
See the Read.ME file in the DatabaseUpdates folder (after unzipping)

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




