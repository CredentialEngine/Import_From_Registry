# Download from the Credential Registry
Sample project to download resources from the credential registry and save the JSON-LD files to the local file system and/or store in a Sql Server database. 
There are two approaches to use this project:
- use the source code (compile and run, or modify as needed)
- use the compiled version in the releases folder


## Preparation
### Sql Server
Install MS Sql Server 2016+.
https://www.microsoft.com/en-us/sql-server/sql-server-downloads 
### Visual Studio
Install Visual Studio Community 2019+
https://visualstudio.microsoft.com/vs/community/ 


## Versions
### September 2, 2022
Updates
- Added a zip of the compiled application (CredentialRegistryDownload_22-09-02.zip) to the releases folder
- Added option to specify a list of data publishers
- Changed app key of envType to environment (for consistency with all other Credential Engine applications).
- Changed app key of *savingDocumentToDatabase* to *writingToDownloadResourceDatabase* for clarity
- There has been no change to the database structure, but a zip of a new backup (CredentialRegistryDownload_backup_empty.zip) was added to the releases folder


### April 12, 2022
Updates
- Added option to handle list of organizations
- Simplified the selecting of resources to include

### October 24, 2021
Updates
- provided how to filter by the owning organization or a third party publisher

New
- added option to store the downloaded documents in a database. There is currently a sample for Sql Server 2016+. We hope to also add an example for MySql soon. 
	Table: Resource
	Columns: CTDL:Type, CTID, Name, Description, SubjectWebpage, Created and LastUpdated date from the registry document, the date downloaded, and the CredentialRegistryGraph
- Usage
	- install SqlServer
	- download starting empty database from github

### September 30, 2020
Initial release. 

This project is designed to have minimum dependencies/configuration. The purpose is to enable downloading documents from the registry and storing on the local file system. A user can decide to store the documents in their own datastore or work with the documents from the file system. 


# Configuration
## App.Config
There will be many of the same keys used by the import project. 
## Primary Key values

<table class="c24">
		<tbody>
			<tr><th>Application Key</th><th>Value/Notes</th></tr>
			<tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">scheduleType</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">&nbsp;Options:</span></p><ul class="c4 lst-kix_14f7h5cunlxz-0 start"><li class="c1 c3"><span class="c2">daily, </span></li><li class="c1 c3"><span class="c2">sinceLastRun, </span></li><li class="c1 c3"><span class="c2">hourly, </span></li><li class="c1 c3"><span class="c2">adhoc, </span></li><li class="c1 c3"><span class="c2">or integer - meaning minutes. Could use to schedule for 7:00 am and minutes since 8pm previous night</span></li></ul><ul class="c4 lst-kix_14f7h5cunlxz-1 start"><li class="c1 c12"><span class="c2">will need to record or derive the last run </span></li></ul></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">startingDate</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">If doing an adhoc run, provide the starting date. </span></p><p class="c1"><span class="c2">NOTE: start/end dates will be converted to UTC before calling the registry searches. If the dates will be provided in UTC, be sure to set usingUTC_ForTime to false.</span></p><p class="c1 c10"><span class="c2"></span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">endingDate</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">If doing an adhoc import and the endDate is to be less than the current date, then provide an end date.</span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">usingUTC_ForTime</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">If true (default), the import will convert any calculated dates to UTC. </span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">Import Entity options.</span></p><p class="c1"><span class="c23">Importing_credential</span></p><p class="c1"><span class="c23">Importing_assessment_profile</span></p><p class="c1"><span class="c23">Importing_learning_opportunity_profile</span></p><p class="c1"><span class="c23">importing_organization</span></p><p class="c1"><span class="c23">Importing_cost_manifest_schema</span></p><p class="c1"><span class="c23">importing_condition_manifest_schema</span></p><p class="c1"><span class="c23">importing_competency_frameworks</span></p><p class="c1"><span class="c23">importing_pathway</span></p><p class="c1"><span class="c23">importing_pathwayset</span></p><p class="c1"><span class="c23">importing_transfer_value_profile</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">Normally these are all true. Set to false if wish to only import particular entities. </span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">deleteAction</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">Actions: 0-normal; 1-DeleteOnly; 2-SkipDelete </span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1 c10"><span class="c2"></span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1 c10"><span class="c2"></span></p></td></tr><tr class="c9"><td class="c8 c31" colspan="1" rowspan="1"><p class="c1"><span class="c2">&nbsp;CR registry</span></p></td><td class="c13 c31" colspan="1" rowspan="1"><p class="c1"><span class="c2">Keys related to the target credential registry environment</span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">MyCredentialEngineAPIKey</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">Provide your API Key from the CE accounts site</span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1 c10"><span class="c2"></span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1 c10"><span class="c2"></span></p></td></tr><tr class="c9"><td class="c8 c15" colspan="1" rowspan="1"><p class="c1"><span class="c2">sandbox</span></p></td><td class="c13 c15" colspan="1" rowspan="1"><p class="c1"><span class="c2">If downloading from the sandbox: </span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1 c10"><span class="c2"></span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">&nbsp; &nbsp; &lt;add key=&quot;credentialRegistryUrl&quot;value=&quot;https://sandbox.credentialengineregistry.org&quot; /&gt;</span></p><p class="c1"><span class="c2">&nbsp; &nbsp; &lt;add key=&quot;credentialRegistryGet&quot; value=&quot;https://sandbox.credentialengineregistry.org/ce-registry/envelopes/{0}&quot; /&gt;</span></p><p class="c1"><span class="c2">&nbsp; &nbsp; &lt;add key=&quot;credentialRegistrySearch&quot;value=&quot;https://sandbox.credentialengine.org/assistant/search/direct?&quot; /&gt;</span></p><p class="c1"><span class="c2">&nbsp; &nbsp; &lt;add key=&quot;credentialRegistryResource&quot; value=&quot;https://sandbox.credentialengineregistry.org/graph/{0}&quot; /&gt;</span></p><p class="c1 c10"><span class="c2"></span></p></td></tr><tr class="c9"><td class="c8 c40" colspan="1" rowspan="1"><p class="c1"><span class="c2">production</span></p></td><td class="c13 c40" colspan="1" rowspan="1"><p class="c1"><span class="c2">If downloading from production:</span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1 c10"><span class="c2"></span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">&lt;add key=&quot;credentialRegistryUrl&quot; value=&quot;https://credentialengineregistry.org&quot; /&gt;</span></p><p class="c1"><span class="c2">&lt;add key=&quot;credentialRegistryGet&quot; value=&quot;https://credentialengineregistry.org/ce-registry/envelopes/{0}&quot; /&gt;</span></p><p class="c1"><span class="c2">&lt;add key=&quot;credentialRegistrySearch&quot; value=&quot;https://apps.credentialengine.org/assistant/search/direct?&quot; /&gt;</span></p><p class="c1"><span class="c2">&lt;add key=&quot;credentialRegistryResource&quot; value=&quot;https://credentialengineregistry.org/resources/{0}&quot; /&gt; </span></p><p class="c1 c10"><span class="c2"></span></p></td></tr><tr class="c9"><td class="c8 c37" colspan="1" rowspan="1"><p class="c1"><span class="c2">Section for tracing and logging</span></p></td><td class="c13 c37" colspan="1" rowspan="1"><p class="c1"><span class="c2">The utilities project has code for tracing, logging errors, and saving files (example files downloaded from registry). The same approach is used in the finder project. </span></p><p class="c1"><span>The default folder for logs is c:\@logs. Create the latter folder and give modify access as needed for your environment, or just select </span><span class="c22">Everyone</span><span class="c2">.</span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">appTraceLevel</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">Set to the max level of tracing to display. The DoTrace methods start with a display level. The actual method that outputs trace messages to a file (and console) will only output message with a display level less than or equal to the value of appTraceLevel. </span></p><p class="c1"><span class="c2">LoggingHelper.DoTrace( 8,&rdquo;some message to only show in rare circumstances&rdquo; );</span></p><p class="c1"><span class="c2">Or to messages to always log. </span></p><p class="c1"><span class="c2">LoggingHelper.DoTrace( 1, string.Format( &quot; - Updates since: {0} &quot;, startingDate ) );</span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">path.error.log</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span>Path and filename for file to store error messages. </span><span class="c36">Uses a pattern so only a month of files are kept, and then overwritten</span><span class="c2">. </span></p><p class="c1"><span class="c20">C:\@logs\[date]_CERegistryDownload_ErrorLog.txt</span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">path.trace.log</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">Path and filename for file to store trace messages. Uses a pattern so only a month of files are kept, and then overwritten. </span></p><p class="c1"><span class="c20">C:\@logs\[date]_CERegistryDownload_TraceLog.txt</span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">path.log.file</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">Path and pattern for saving files such as registry downloads. </span></p><p class="c1"><span class="c20">C:\@logs\RegistryDownload\[date]_[filename].json</span></p><p class="c1"><span class="c2">If using the latter default value, be sure to create the RegistryDownload folder in the C:\@logs folder.</span></p></td></tr><tr class="c9"><td class="c8" colspan="1" rowspan="1"><p class="c1"><span class="c2">path.email.log</span></p></td><td class="c13" colspan="1" rowspan="1"><p class="c1"><span class="c2">Path to file for logging emails, if logAllEmail=yes</span></p></td></tr>
		</tbody>
	</table>
