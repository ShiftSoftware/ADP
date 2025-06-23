# ADP Sync Agent

The ADP Sync Agent is a NuGet package that provides a robust, reusable framework for building scheduled or event-driven data synchronization pipelines. It standardizes the process of moving and transforming data between various systems with a focus on reliability, performance, and maintainability.

The Sync Agent offers built-in adapters for common data sources and destinations, and provides clean interfaces for implementing custom adapters when needed. This makes it easy to integrate the agent into a wide variety of enterprise data flow scenarios.

## Data Pipelines
A data pipeline defines the end-to-end flow of data from a source system to a destination system, with optional transformation logic in between.

A typical pipeline setup with the ADP Sync Agent involves:

1. Instantiating and configuring a Sync Agent instance. 
2. Supplying a built-in or custom data source adapter. 
3. Supplying a built-in or custom data destination adapter. 
4. Providing a mapping function to transform and manipulate data as needed. 
5. Starting the sync process.

Pipelines can be triggered by various systems, such as:

- Scheduled jobs (e.g., Hangfire, Azure Functions, Windows Services).
- Event-based triggers (e.g., Database change feeds, Service Bus messages, Blob storage events).

## Benefits Over Ad-Hoc Pipelines
In many enterprise environments, data synchronization is handled through numerous ad-hoc pipelines built by different teams, 
often with inconsistent quality, reliability, and scalability.  

These pipelines are prone to critical issues not necessarily due to negligence, 
but because the application development teams building them may lack the budget, time, or immediate need 
to fully account for the complexities and best practices of reliable data integration. 

Their focus is typically on delivering core application functionality, not the underlying data transport. 

As a result, many of the pitfalls associated with data pipelines such as partial failures, inconsistent data, and performance bottlenecks go unnoticed during early development and testing phases.  

Unfortunately, these issues often surface only in real-world scenarios under heavy load, when inaccurate or incomplete data can cause significant damage and become costly to resolve.

Ad-Hoc Pipelines that are directly built into application code are prone to:

- Memory leaks
- Data inconsistency
- Faults on partial failures
- Lack of retries or recovery
- Difficulty in maintenance

The ADP Sync Agent addresses these challenges with a consistent and heavily tested approach.

### Key Advantages

#### Standardized Adapters
Avoid the need to repeatedly implement fragile or error-prone integrations. The Sync Agent offers tested and reusable adapters for common data systems, reducing duplication and bugs.

#### Efficient Memory Management
The agent is optimized to handle large datasets efficiently, with careful memory management to prevent issues like ``OutOfMemoryException``, which commonly occur in ad-hoc pipelines during high-volume data processing.

#### Resilience and Fault Tolerance
Built-in retry policies, error handling mechanisms, and logging ensure that transient failures do not result in data loss or inconsistent states.

#### Atomic Execution
A pipeline execution is considered successful only if all critical operations complete successfully, including reading from the source, transforming the data, writing to the destination, and marking the source as synced. This final step is essential to prevent duplicate processing in future runs.

The mechanism for marking the source as synced varies by source type:

- For database tables, this may involve setting a LastSynced timestamp or updating a status flag on each record.
- For flat file sources, the Sync Agent stores a copy of the synced file in a content repository. On subsequent runs, a git diff is performed against the new file to identify only the changes.

If any of these steps fail whether it's during data transfer or while marking the data as synced the entire pipeline execution is treated as a failure, and the developer can chose whether to commit partial results or not. 

#### Extensibility
Developers can implement custom Data Adapters to integrate with proprietary or domain-specific systems.

The mapping function allows inline or reusable transformation logic to adapt source data to the required destination schema or format.

## Preview

In most cases the Sync Agent is running in the background. 
But the sync agent provides tools when a UI is needed.

<iframe 
width="100%" 
height="480" 
src="https://www.youtube.com/embed/__V-kbgKNiI?si=d57DLN3LQOUFGZkM&amp;controls=0&autoplay=1&loop=1&playlist=__V-kbgKNiI&mute=1&modestbranding=1&showinfo=0&rel=0" 
title="YouTube video player" 
frameborder="0" 
allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" 
referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>