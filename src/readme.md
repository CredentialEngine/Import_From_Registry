# April 27 2022 Release

## Application Config

Updates
- Slight resturcturing of the CTI.Import.exe.config file to place the keys most likely to be uses/customized at the top of the file
- Updated **owningOrganizationCTID** and **publishingOrganizationCTID** to handle a list of CTIDs (previously only one was handled)
- Provided a new option for selecting the resource types to include in an import request
	- Added the key: **usingListOfResourceTypes**
		- if the latter is true, a user will provided a comma separated list of resource types in the appKey: **resourceTypeList**
			example: 
			<add key="resourceTypeList" value="credential, organization" />
		- the appKey: **resourceTypeListAll** contains a list of all of the valid resource types available in the credential registry. Just copy the resource types that should be included in an import to the appKey: **resourceTypeList**
		- if **usingListOfResourceTypes** is false, the previous method will be used where the list of **importing** keys (e.g.. importing_credential) will be used. Set the key value to true for the resource type to import. 

## Logging

Reminder: There are options to save documents downloaded from the Credential Registry as part of an import. 
Set the key: savingDocumentToFileSystem to true to have documents saved. These documents will be saved in the folder and using the file name template specified by the key: path.log.file.
You can control whether to save a file by logFileTraceLevel. If the latter is greater than appTraceLevel, files will not be saved.
