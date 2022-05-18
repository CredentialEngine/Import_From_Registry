/*
Sample restore SQL to a SQL server 2016+ database

The database has one user now that will have to be created in the target database (or new users could be added, and replace those referenced by the apps). 
======================================================================
-- create user if not previouly done
use master 
go
sp_addLogin 'ceGithub', 'ce$Rocks2020', master
======================================================================
Run the restore step, 
First:
- change @wnTestBackupLoc to point to folder that contains the backup file
- change @wnTestDataLoc to the folder where the sql server data file will be stored.
- change @wnTestLogsLoc to the folder where the sql server log file will be stored.
- change set @BackupFile = 'CE_ExternalData_220517.bak'	 to be the actual name of the backup file in the folder referenced by @wnTestBackupLoc

Restore. 
using: set @RestoreAction = 'list' will essentially validate the folders and file name etc are valid. 
When ready to do the restore, comment the latter statement (prefix with --), then execute the script.

======================================================================
After completing the restore, run the following sql to 'associate' the named accounts in the backup file with the ones created on this server

Use CE_ExternalData
go
sp_change_users_login 'report'
go
sp_change_users_login 'update_one', 'ceGithub','ceGithub'
go
sp_change_users_login 'report'
go

======================================================================
NOTES
- if db in use, the following script can be used to remove locks, then try again

use master 
go
declare @sql as varchar(20), @db as varchar(20), @spid as int
set @db = 'CE_ExternalData'
select @spid = min(spid)  from master..sysprocesses  where dbid = db_id(@db) 
and spid != @@spid    

while (@spid is not null)
begin
    print 'Killing process ' + cast(@spid as varchar) + ' ...'
    set @sql = 'kill ' + cast(@spid as varchar)
    exec (@sql)

    select 
        @spid = min(spid)  
    from 
        master..sysprocesses  
    where 
        dbid = db_id(@db) 
        and spid != @@spid
end 

print 'Process completed...'

*/
-- Script to restore a CE_ExternalData backup 

Declare
	@RestoreAction	varchar(20),
	@DestDatabase	varchar(150),
	@DestDatafile	varchar(150),
	@DestLogfile	varchar(150),
	@DestDataPath	varchar(150),
	@DestLogPath	varchar(150),
	@BackupFile		varchar(150),
	@BackupPath		varchar(150),
	@Datafile		varchar(150),
	@Logfile		varchar(150),
	@BackupDir  	varchar(150)
	,@wnTestDataLoc	varchar(150)
	,@wnTestLogsLoc	varchar(150)
	,@wnTestBackupLoc	varchar(150)

-- ===============================================================
set @wnTestBackupLoc  = 'C:\data\sql2016\backups\Adhoc\'                
set @wnTestDataLoc    = 'C:\data\sql2016\'
set @wnTestLogsLoc    = 'C:\data\sql2016\'

-- 
-- Actions:
-- ======================================================================
-- Use List to list the contents of the selected backup file
-- Use replace when restoring the backup from one db overtop another db (our typical scenario)
	-- comment out the line with the 'list' statement when ready to do the restore. 

set @RestoreAction = 'replace'
set @RestoreAction = 'list'

set @DestDatabase = 'CE_ExternalData'
set @DestDatafile = @DestDatabase --+ '_Data'
set @DestLogfile  = @DestDatabase + '_Log'

-- set to the current locations===
set @BackupDir 	= @wnTestBackupLoc

-- ==> change this the actual name of the backup file
set @BackupFile = 'CE_ExternalData_220517.bak'	

if 1 = 2 begin
-- If the source backup is the same as the dest. then use:
	set @Datafile = @DestDatabase + '_Data'
	set @Logfile = @DestDatabase + '_Log'
	end
else begin
-- Otherwise use (either name from backup or may need to check logical name used in the 
-- backup - that is first execute RESTORE FILELISTONLY FROM DISK = @BackupPath below
	set @Datafile = 'CE_ExternalData'
	set @Logfile  = 'CE_ExternalData_Log'	
	end

-- following should just be generic variables
set @BackupPath = @BackupDir + @BackupFile

set @DestDataPath = @wnTestDataLoc + @DestDatafile + '.mdf'
set @DestLogPath  = @wnTestLogsLoc  + @DestLogfile + '.ldf'
print '====== Restore request parameters ===================='
print '        Action: ' + @RestoreAction
print 'SOURCE:'
print '        Source: ' + @BackupPath
print '      Datafile: ' + @Datafile
print '       Logfile: ' + @Logfile
print '------------------------------------------------------'
print 'DESTINATION:'
print '      Database: ' + @DestDatabase
print '          Data: ' + @DestDataPath
print '           Log: ' + @DestLogPath
print ''
print '======================================================'
if @RestoreAction = 'list' begin
RESTORE FILELISTONLY FROM DISK = @BackupPath
end
else if @RestoreAction = 'recover' begin
RESTORE DATABASE @DestDatabase FROM DISK = @BackupPath
	WITH RECOVERY,
	MOVE @Datafile 	TO @DestDataPath,
	MOVE @Logfile 	TO @DestLogPath
end
else if @RestoreAction = 'replace' begin
RESTORE DATABASE @DestDatabase FROM DISK = @BackupPath
	WITH RECOVERY, 
		 REPLACE,
	MOVE @Datafile 	TO @DestDataPath,
	MOVE @Logfile 	TO @DestLogPath
end else begin
	print 'Invalid restore action of : ' +  @RestoreAction 
	print 'no action taken'
end
GO