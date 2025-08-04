# Professional Database Management Setup Guide

## SSMS Step-by-Step Guide - You Have It Installed! ✅

### Step 1: Launch SSMS and Connect to Your Database

1. **Open SQL Server Management Studio**
   - Look for "Microsoft SQL Server Management Studio" in your Start Menu
   - Or search for "SSMS" in Windows search
   - Click to launch

2. **Connect to Server Dialog Box**
   - SSMS will show a "Connect to Server" dialog when it starts
   - If it doesn't appear, click "File" → "Connect Object Explorer"

3. **Enter Connection Details:**
   ```
   Server type: Database Engine
   Server name: (localdb)\mssqllocaldb
   Authentication: Windows Authentication
   ```
   - Leave Username and Password blank (Windows Auth handles this)
   - Click "Connect"

4. **Verify Connection**
   - You should see "(localdb)\mssqllocaldb" appear in Object Explorer (left panel)
   - If you get an error, make sure your API is running (it starts the LocalDB)

### Step 2: Navigate to Your Database

1. **Expand the Server**
   - Click the ► arrow next to "(localdb)\mssqllocaldb"
   - Click the ► arrow next to "Databases"

2. **Find Your Database**
   - Look for "PersonalityAssessmentDb"
   - Click the ► arrow next to it to expand

3. **Explore Your Tables**
   - Click the ► arrow next to "Tables"
   - You'll see:
     - `dbo.Assessments`
     - `dbo.PersonalityProfiles`
     - `dbo.UserResponses`
     - `dbo.Users`
     - `dbo.__EFMigrationsHistory` (system table)

### Step 3: View Your Data

#### Method 1: Quick Table View
1. **Right-click on `dbo.Users`**
2. **Select "Edit Top 200 Rows"**
3. **You'll see a grid with your user data:**
   - UserId, Name, Email, CreatedDate columns
   - You can edit data directly in this grid
   - Press Enter to save changes

#### Method 2: Using SQL Queries (More Powerful)
1. **Click "New Query" button** (or Ctrl+N)
2. **Make sure "PersonalityAssessmentDb" is selected** in the dropdown at the top
3. **Try these sample queries:**

```sql
-- See all users
SELECT * FROM Users ORDER BY CreatedDate DESC;

-- Users with assessment counts
SELECT 
    u.UserId,
    u.Name,
    u.Email,
    u.CreatedDate,
    COUNT(a.AssessmentId) as AssessmentCount,
    MAX(a.CompletedDate) as LastAssessment
FROM Users u
LEFT JOIN Assessments a ON u.UserId = a.UserId
GROUP BY u.UserId, u.Name, u.Email, u.CreatedDate
ORDER BY u.CreatedDate DESC;

-- Recent personality profiles with summaries
SELECT 
    u.Name,
    u.Email,
    p.CreatedDate as ProfileCreated,
    JSON_VALUE(p.ProfileData, '$.Summary') as PersonalitySummary
FROM PersonalityProfiles p
INNER JOIN Users u ON p.UserId = u.UserId
ORDER BY p.CreatedDate DESC;

-- Database statistics
SELECT 
    'Users' as TableName, COUNT(*) as RecordCount FROM Users
UNION ALL
SELECT 'Assessments', COUNT(*) FROM Assessments
UNION ALL  
SELECT 'UserResponses', COUNT(*) FROM UserResponses
UNION ALL
SELECT 'PersonalityProfiles', COUNT(*) FROM PersonalityProfiles;
```

4. **Execute Query:** Press F5 or click "Execute" button
5. **View Results:** Results appear in the bottom panel

### Step 4: Essential SSMS Features for Your Database

#### A. Table Design View
1. **Right-click on `dbo.Users`** → **"Design"**
2. **You'll see the table structure:**
   - Column names, data types, allow nulls
   - You can modify the table structure here
   - **⚠️ Be careful** - changes affect your API

#### B. Data Export
1. **Right-click on "PersonalityAssessmentDb"**
2. **Tasks** → **"Export Data..."**
3. **Choose destination:** Excel, CSV, another database
4. **Select tables to export**
5. **Follow the wizard**

#### C. Database Backup
1. **Right-click on "PersonalityAssessmentDb"**
2. **Tasks** → **"Back Up..."**
3. **Backup type:** Full
4. **Destination:** Click "Add" to choose location
5. **Example:** `C:\Backups\PersonalityAssessmentDb_2025-06-29.bak`
6. **Click OK**

#### D. Query Performance Analysis
1. **Write a query in New Query window**
2. **Click "Include Actual Execution Plan"** (Ctrl+M)
3. **Execute the query**
4. **Check "Execution plan" tab** to see performance details

### Step 5: Useful Views for Your Data

Create these views for easier data analysis:

```sql
-- Create a view for user summary
CREATE VIEW UserSummary AS
SELECT 
    u.UserId,
    u.Name,
    u.Email,
    u.CreatedDate,
    COUNT(a.AssessmentId) as TotalAssessments,
    MAX(a.CompletedDate) as LastAssessmentDate,
    CASE 
        WHEN COUNT(a.AssessmentId) > 0 THEN 'Active'
        ELSE 'Inactive'
    END as UserStatus
FROM Users u
LEFT JOIN Assessments a ON u.UserId = a.UserId
GROUP BY u.UserId, u.Name, u.Email, u.CreatedDate;

-- Now you can query it easily
SELECT * FROM UserSummary ORDER BY TotalAssessments DESC;
```

### Step 6: Database Maintenance Tasks

#### Weekly Maintenance Checklist:
1. **Backup Database:**
   ```sql
   BACKUP DATABASE PersonalityAssessmentDb 
   TO DISK = 'C:\Backups\PersonalityAssessmentDb_Weekly.bak'
   WITH INIT; -- Overwrites previous backup
   ```

2. **Check Database Size:**
   ```sql
   SELECT 
       DB_NAME() as DatabaseName,
       (SELECT SUM(size) * 8 / 1024 FROM sys.database_files WHERE type = 0) as DataSizeMB,
       (SELECT SUM(size) * 8 / 1024 FROM sys.database_files WHERE type = 1) as LogSizeMB;
   ```

3. **Update Statistics (for better performance):**
   ```sql
   EXEC sp_updatestats;
   ```

### Step 7: Advanced Features You Can Explore

#### A. Activity Monitor
- **Right-click server** → **"Activity Monitor"**
- **See:** Current connections, recent queries, resource usage

#### B. Database Diagrams
- **Right-click "Database Diagrams"** → **"New Database Diagram"**
- **Add all tables** to see relationships visually

#### C. Import/Export Wizard
- **Right-click database** → **Tasks** → **"Import Data"**
- **Import from:** Excel, CSV, other databases

### Troubleshooting Common Issues

#### Issue 1: "Cannot connect to (localdb)\mssqllocaldb"
**Solution:** Make sure your API is running first:
```cmd
cd "g:\Personality-Assessment\Personality-Assessment\PersonalityAssessment.Api\PersonalityAssessment.Api"
dotnet run
```

#### Issue 2: "Database not found"
**Solution:** Check if database exists:
```sql
SELECT name FROM sys.databases WHERE name = 'PersonalityAssessmentDb';
```

#### Issue 3: "Permission denied"
**Solution:** Run SSMS as Administrator (right-click → "Run as administrator")

### Pro Tips for Daily Use

1. **Keyboard Shortcuts:**
   - `Ctrl+N`: New Query
   - `F5`: Execute Query
   - `Ctrl+Shift+R`: Refresh Object Explorer
   - `Ctrl+L`: Display Estimated Execution Plan

2. **Query Templates:**
   - **View** → **Template Explorer**
   - **Drag templates** into query window

3. **Multiple Query Windows:**
   - **Keep one window** for data viewing
   - **Keep another** for maintenance tasks

4. **Save Frequently Used Queries:**
   - **File** → **Save As** (save as .sql files)
   - **Create a folder** for your common queries

### What You Can Do Right Now:

1. **Open SSMS and connect** using the steps above
2. **Run the sample queries** to see your data
3. **Try editing a user's name** in the grid view
4. **Create your first backup**
5. **Explore the table relationships**

---

## Option 2: Azure Data Studio - Modern & Cross-Platform 🚀

**Download & Install:**
1. Go to: https://docs.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio
2. Download for Windows (free)
3. Install and launch

**Connection Details:**
- **Connection Type:** Microsoft SQL Server
- **Server:** `(localdb)\mssqllocaldb`
- **Authentication Type:** Windows Authentication
- **Database:** PersonalityAssessmentDb

**What You Can Do:**
- Modern, VS Code-like interface
- IntelliSense for SQL
- Notebooks for documentation
- Git integration
- Extensions and themes
- Charts and visualizations
- Cross-platform (Windows, Mac, Linux)

---

## Option 3: Visual Studio SQL Server Object Explorer

If you have Visual Studio installed:
1. Open Visual Studio
2. Go to View → SQL Server Object Explorer
3. Right-click "SQL Server" → Add SQL Server
4. Server Name: `(localdb)\mssqllocaldb`
5. Connect

---

## Option 4: Professional Database Tools

### DBeaver (Free & Powerful)
- Universal database tool
- Download: https://dbeaver.io/
- Supports 200+ database types
- Great for data visualization

### DataGrip (JetBrains - Paid)
- Professional IDE for databases
- Advanced query assistance
- Refactoring tools
- 30-day free trial

### Navicat (Paid)
- Enterprise-grade database tool
- Beautiful UI
- Advanced features

---

## Your Database Schema Overview

Once connected, you'll see these tables:

```
PersonalityAssessmentDb/
├── Users
│   ├── UserId (PK)
│   ├── Name
│   ├── Email
│   └── CreatedDate
├── Assessments
│   ├── AssessmentId (PK)
│   ├── UserId (FK)
│   ├── StartedDate
│   ├── CompletedDate
│   └── Status
├── UserResponses
│   ├── ResponseId (PK)
│   ├── AssessmentId (FK)
│   ├── QuestionId
│   ├── AnswerValue
│   └── ResponseTime
└── PersonalityProfiles
    ├── ProfileId (PK)
    ├── UserId (FK)
    ├── CreatedDate
    ├── UpdatedDate
    └── ProfileData (JSON)
```

## Useful SQL Queries to Get Started

### View All Users with Assessment Count
```sql
SELECT 
    u.UserId,
    u.Name,
    u.Email,
    u.CreatedDate,
    COUNT(a.AssessmentId) as AssessmentCount
FROM Users u
LEFT JOIN Assessments a ON u.UserId = a.UserId
GROUP BY u.UserId, u.Name, u.Email, u.CreatedDate
ORDER BY u.CreatedDate DESC;
```

### View Latest Assessments
```sql
SELECT TOP 10
    u.Name,
    u.Email,
    a.AssessmentId,
    a.StartedDate,
    a.CompletedDate,
    a.Status
FROM Assessments a
INNER JOIN Users u ON a.UserId = u.UserId
ORDER BY a.StartedDate DESC;
```

### View Personality Profile Summary
```sql
SELECT 
    u.Name,
    u.Email,
    p.CreatedDate as ProfileCreated,
    JSON_VALUE(p.ProfileData, '$.Summary') as PersonalitySummary
FROM PersonalityProfiles p
INNER JOIN Users u ON p.UserId = u.UserId
ORDER BY p.CreatedDate DESC;
```

### Database Statistics
```sql
SELECT 
    'Users' as TableName, COUNT(*) as RecordCount FROM Users
UNION ALL
SELECT 'Assessments', COUNT(*) FROM Assessments
UNION ALL  
SELECT 'UserResponses', COUNT(*) FROM UserResponses
UNION ALL
SELECT 'PersonalityProfiles', COUNT(*) FROM PersonalityProfiles;
```

## Pro Tips

1. **Always backup before making changes:**
   ```sql
   BACKUP DATABASE PersonalityAssessmentDb 
   TO DISK = 'C:\Backups\PersonalityAssessmentDb.bak'
   ```

2. **Use transactions for data modifications:**
   ```sql
   BEGIN TRANSACTION;
   -- Your changes here
   -- ROLLBACK; -- if something goes wrong
   COMMIT; -- if everything is good
   ```

3. **Create indexes for better performance:**
   ```sql
   CREATE INDEX IX_Users_Email ON Users(Email);
   CREATE INDEX IX_Assessments_UserId ON Assessments(UserId);
   ```

4. **Regular maintenance:**
   - Monitor database size
   - Check for unused data
   - Update statistics
   - Rebuild indexes periodically
