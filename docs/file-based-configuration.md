# File-based Configuration

You can configure Splunk Distribution of OpenTelemetry .NET using a YAML file.

## Not Supported configurations

> ⚠️ **Important:**  
> File-based configuration support is currently **limited**.  
> Some settings **cannot be configured via YAML files**.  
> These options **must still be set using environment variables** to
> ensure correct runtime behavior and instrumentation activation.

The settings listed below are **not supported** through the configuration file.

Future versions may extend file-based configuration to include these parameters.

### General

- `SPLUNK_ACCESS_TOKEN`
- `SPLUNK_REALM`

### Instrumentations configuration

- `SPLUNK_TRACE_RESPONSE_HEADER_ENABLED`

## Profiling configuration

> ⚠️ **Important:**  
> Profiling configuration via file-based configuration requires
> `meter_provider` or `tracer_provider` to be configured via file-based configuration as well.
> Without any of them, profiling configuration will not work.
> `callgraphs` requires `baggage propagator` to be set via file-based configuration.

Demonstration of all possible configuration options.

```yaml
# Distribution configuration
distribution:
  # Splunk-specific configuration
  splunk:
    # Profiling configuration
    profiling:
      # Exporter configuration
      exporter:
        # Configure exporter to be OTLP Log exporter with HTTP transport.
        otlp_log_http:
          # Configure endpoint, including the logs specific path.
          # If omitted or null, http://localhost:4318/v1/logs is used.
          endpoint: "http://localhost:4318/v1/logs"
          # Configure interval for exporting collected stack data. 
          # Minimum value is 500.
          # If omitted or null, 500 is used.
          schedule_delay: 500
          # Configure timeout for exporting stack data.
          # If omitted or null, 3000 is used.
          export_timeout: 3000
      # Always-on profiling configuration    
      always_on:
        # Configure CPU call stack profiler
        cpu_profiler:
          # Configure frequency with which call stacks are sampled.
          # If omitted or null, 10000 is used.
          sampling_interval: 10000
        # Configure memory allocation profiler
        memory_profiler:
          # Configure maximum memory samples collected per minute. 
          # Maximum value is 500.
          # If omitted or null, 200 is used.
          max_memory_samples: 200
      # Configure Callgraph snapshot profiler
      callgraphs:
        # Configure sampling interval.
        # If omitted or null, 40 is used.
        sampling_interval: 40
        # Configure probability of selecting a trace. 
        # Maximum value is 0.1.
        # If omitted or null, 0.01 is used.
        selection_probability: 0.01
        # Configure if the High-resolution Timer is enabled or not.
        # If omitted or null, false is used.
        high_resolution_timer_enabled: false
```

## Configuration Examples

To start using file-based configuration, put this YAML content into a your configuration file.

### Default Configuration With All Profilings

Standard configurations that were used for enabling all profilers with default settings.

```yaml
# Distribution configuration
distribution:
  # Splunk-specific configuration
  splunk:
    # Profiling configuration
    profiling:
      # Exporter configuration
      exporter:
        # Configure exporter to be OTLP Log exporter with HTTP transport with default settings
        otlp_log_http:
      # Always-on profiling configuration  
      always_on:
        # Configure CPU stack profiler with default settings
        cpu_profiler:
        # Configure Memory allocation profiler with default settings
        memory_profiler:
    # Configure Callgraph snapshot profiler with default settings
      callgraphs:
```

### Envarironment Variable Migration Example

Here is a typical starting point for configuring the Splunk Distribution of OpenTelemetry .NET
when migrating from environment variable based configuration.

```yaml
# Distribution configuration
distribution:
  # Splunk-specific configuration
  splunk:
    # Profiling configuration
    profiling:
      # Exporter configuration
      exporter:
        # Configure exporter to be OTLP Log exporter with HTTP transport.
        otlp_log_http:
          # Configure endpoint, including the logs specific path.
          # If omitted or null, http://localhost:4318/v1/logs is used.
          endpoint: ${SPLUNK_PROFILER_LOGS_ENDPOINT}
          # Configure interval for exporting collected stack data. 
          # Minimum value is 500.
          # If omitted or null, 500 is used.
          schedule_delay: ${SPLUNK_PROFILER_EXPORT_INTERVAL}
          # Configure timeout for exporting stack data.
          # If omitted or null, 3000 is used.
          export_timeout: ${SPLUNK_PROFILER_EXPORT_TIMEOUT}
      # Always-on profiling configuration    
      always_on:
        # Configure CPU call stack profiler
        cpu_profiler:
          # Configure frequency with which call stacks are sampled.
          # If omitted or null, 10000 is used.
          sampling_interval: ${SPLUNK_PROFILER_CALL_STACK_INTERVAL}
        # Configure memory allocation profiler
        memory_profiler:
          # Configure maximum memory samples collected per minute. 
          # Maximum value is 500.
          # If omitted or null, 200 is used.
          max_memory_samples: ${SPLUNK_PROFILER_MEMORY_MAX_SAMPLES}
      # Configure Callgraph snapshot profiler
      callgraphs:
        # Configure sampling interval.
        # If omitted or null, 20 is used.
        sampling_interval: ${SPLUNK_SNAPSHOT_SAMPLING_INTERVAL}
        # Configure probability of selecting a trace. 
        # Maximum value is 0.1.
        # If omitted or null, 0.01 is used.
        selection_probability: ${SPLUNK_SNAPSHOT_SELECTION_PROBABILITY}
        # Configure if the High-resolution Timer is enabled or not.
        # If omitted or null, false is used.
        high_resolution_timer_enabled: ${SPLUNK_SNAPSHOT_HIGH_RES_TIMER_ENABLED}
```
