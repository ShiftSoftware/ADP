# ADP Sync Agent

The ADP Sync Agent is a NuGet package that provides a robust, reusable framework for building scheduled or event-driven data synchronization pipelines. It standardizes the process of moving and transforming data between various systems with a focus on reliability, performance, and maintainability.

The Sync Agent ships with built-in adapters for common data sources and destinations, and exposes clean interfaces for implementing custom adapters when needed. This makes it easy to integrate the agent into a wide variety of enterprise data flow scenarios.

## Package

| | |
|---|---|
| **Package ID** | `ShiftSoftware.ADP.SyncAgent` |
| **Target Framework** | `.NET 10` |

## Key Capabilities

| Capability | Description |
|---|---|
| **Standardized Adapters** | Ready-to-use, tested adapters for CSV, EF Core, Azure Cosmos DB, and DuckDB — eliminating the need for repetitive, fragile integrations. |
| **Efficient Memory Management** | Optimized for large datasets with careful memory handling to prevent `OutOfMemoryException` and other issues common in ad-hoc pipelines. |
| **Resilience & Fault Tolerance** | Configurable retry policies, batch-level error handling, and multiple retry strategies ensure transient failures do not corrupt data. |
| **Atomic Execution** | A pipeline run is only marked successful when every critical step completes — read, transform, write, and mark-as-synced. Partial results are controlled by the developer. |
| **Extensibility** | Developers can implement custom Data Adapters and inline or reusable mapping functions to integrate with any system. |
| **Logging & Progress** | A pluggable logger interface provides real-time progress tracking, elapsed time, remaining timeout, and per-batch status for operational visibility. |

## Why Not Ad-Hoc Pipelines?

In many enterprise environments, data synchronization is handled through numerous ad-hoc pipelines built by different teams, often with inconsistent quality, reliability, and scalability.

These pipelines are prone to critical issues — not necessarily due to negligence, but because the application development teams building them may lack the budget, time, or immediate need to fully account for the complexities and best practices of reliable data integration.

Their focus is typically on delivering core application functionality, not the underlying data transport.

As a result, many of the pitfalls associated with data pipelines — such as partial failures, inconsistent data, and performance bottlenecks — go unnoticed during early development and testing phases.

Unfortunately, these issues often surface only in real-world scenarios under heavy load, when inaccurate or incomplete data can cause significant damage and become costly to resolve.

**Common issues with ad-hoc pipelines:**

- Memory leaks during high-volume processing
- Data inconsistency from partial failures
- Lack of retries or recovery mechanisms
- Difficulty in maintenance and observability

The ADP Sync Agent addresses these challenges with a consistent, heavily tested, and well-structured approach.

## Documentation

| Section | Description |
|---|---|
| [Architecture](architecture.md) | The Sync Engine pipeline lifecycle, action ordering, and batch processing model. |
| [Data Adapters](data-adapters.md) | Built-in source and destination adapters — CSV, EF Core, Cosmos DB, and DuckDB. |
| [Configuration](configuration.md) | Reference for all configuration types and options. |
| [Resilience & Fault Tolerance](resilience.md) | Retry strategies, batch retry, and error handling. |
| [Logging & Monitoring](logging.md) | Pluggable loggers, progress indicators, and operational visibility. |
| [Getting Started](getting-started.md) | Quick start guide with dependency injection setup and end-to-end examples. |

## Preview

In most cases the Sync Agent runs in the background. But when a UI is needed, the Sync Agent provides tools for real-time progress visualization.

<iframe 
width="100%" 
height="480" 
src="https://www.youtube.com/embed/__V-kbgKNiI?si=d57DLN3LQOUFGZkM&amp;controls=0&autoplay=1&loop=1&playlist=__V-kbgKNiI&mute=1&modestbranding=1&showinfo=0&rel=0" 
title="YouTube video player" 
frameborder="0" 
allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" 
referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>
