# Integrations
Being the centralized platform between the distributor, its dealers, and customers. Integrations is in the heart of the ADP.

## Connectors
The following are the different types of integration connectors that can be utilized by ADP:

- Direct Database (SQL, or other types of databases).
- Restful APIs.
- Parquet, CSV, JSON, XML, or other file-based data exchange formats.

### Direct Database Connector
The distributor obtains and stores a connection string of each dealer's DMS database securely in a cloud vault.  
This connector allows the ADP to directly query the dealer's DMS database to fetch data periodically.

!!! warning
	Writing to the dealer's DMS databas is typically not required. It's preferred that dealers share connection string with read-only access.

### Restful API Connector
This could come in two modes:

- **Push by DMS (Batch)**: The distributor provides a set of RESTful APIs that the dealers can use to push data batches to the ADP.
- **Pull by ADP (Batch)**: The dealer's DMS system provides a set of RESTful APIs that the ADP can use to pull data batches from the dealer's DMS.

!!! note
	**Webhooks or Event Notifications:**  

	For push based integration, the dealer's system can trigger real-time updates to the ADP when a change occurs, eliminating the need for periodic polling.

	However, this by itself is not sufficient due to the following reasons:

	- **Initial Sync**: While it's technically possible to use webhooks for initial sync, it may not be practical as webhooks are typically used to push a single record at a time. And the initial sync may involve a large number of records.
	- **Data Loss**: If the webhook trigger fails for any reason, the ADP may miss important updates. If no other means of data transfer is available, then there should be a way to re-trigger **(retry)** the missed push indefinitely until the record is successfully pushed.
	- **Data Reinitialization**: There are cases when full data reinitialization is required. For example, if a dealer's Database or ADP database is structurally changed, or if the data is corrupted. In such cases, the dealer's DMS system should be able to reinitialize the data in ADP by pushing all records again.

### File-based Connector

The dealer's DMS system can periodically export data in Parquet, CSV, JSON, or XML format to a specified local or remote location. 

These files can be synced to ADP cloud using a file sync tool like [Azure File Sync](https://learn.microsoft.com/en-us/azure/storage/file-sync/file-sync-deployment-guide?tabs=azure-portal%2Cproactive-portal). ADP will then process these files to update its database.


## Data Synchronization

Efficient data synchronization is critical to ensure the ADP platform remains up-to-date without loading and processing redundant information.  
Rather than syncing the entire dataset during each integration cycle, ADP supports several techniques to fetch only newly added or modified data.  


### Sync Strategies

#### Timestamps / Last Updated Fields
Records that include a last_modified, updated_at, or similar timestamp field can be filtered to return only those changed since the last successful sync.   
This takes different forms depending on the connector type:

- **Direct Database**: The SQL query can be modified to include a WHERE clause that filters records based on the last_updated timestamp.
- **Restful API**:
    - **Push by DMS**: The dealer's DMS system should store the last successful push timestamp and only push records that have been modified/created since that timestamp.
	- **Pull by ADP**: ADP stores the last successful pull timestamp and only requests records that have been modified/created since that timestamp.
- **File-based**: 
    - The dealer's DMS system can dump records that have been modified/created since the last successful dump and store them in a file containing the current batch of records. (company_a_part_stock_2025_05_13_12_00_00.csv).
	- The dealer's DMS system can dump all records to a file and replace it on every dump. (company_a_part_stock.csv). ADP will handle processing the file and only updating the records that have been modified/created since the last successful dump. (This is done internally by doing a git diff between the last dump and the current dump).