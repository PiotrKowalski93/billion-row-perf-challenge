# Memory-Mapped Files (MMF)

## What is a Memory-Mapped File?

A Memory-Mapped File allows a file (or a portion of it) to be mapped directly into a process's virtual address space.
Instead of explicitly reading and writing data using system calls (`read`, `write`, `FileStream`, etc.), the application accesses file contents as if they were normal memory.

```
File on Disk
      ↓
Operating System
      ↓
Virtual Memory Mapping
      ↓
Process Address Space
```

---

## Why Use MMF?

### Benefits

* Avoids extra user-space copies.
* Simplifies random access to large files.
* OS handles caching automatically through the page cache.
* Multiple processes can share the same mapped memory.
* Often faster for large datasets and frequent random access.

### Typical Use Cases

* Databases
* Trading systems
* Large log processing
* Game engines
* Shared memory IPC
* Scientific computing

---

## How It Works

1. The process requests a mapping (`mmap` on Linux, `MemoryMappedFile` on .NET).
2. The OS reserves virtual memory pages.
3. No file data is loaded immediately.
4. When a page is accessed, a **page fault** occurs.
5. The OS loads the required page from disk into RAM.
6. Subsequent accesses behave like normal memory reads/writes.

This behavior is known as **lazy loading**.

---

## Key Concepts

### Page Fault

Occurs when a mapped page is not currently present in physical memory.

```
Memory Access
      ↓
Page Not Present
      ↓
Page Fault
      ↓
OS Loads Page
      ↓
Execution Continues
```

Page faults are expensive because they require kernel intervention.

---

### Demand Paging

Pages are loaded only when accessed.

Benefits:

* Faster startup
* Lower memory consumption
* Efficient handling of huge files

---

### Shared vs Private Mapping

#### Shared Mapping

Changes are visible to all processes mapping the same region.

```
Process A
      ↓
 Shared Pages
      ↑
Process B
```

#### Private Mapping (Copy-On-Write)

Processes initially share pages.

After a write:

```
Shared Page
      ↓
Write Occurs
      ↓
OS Creates Private Copy
```

Other processes do not see the modification.

---

## Performance Characteristics

### Good For

* Sequential scans of large files
* Random access workloads
* Large datasets that do not fit entirely in memory
* Inter-process communication

### Potential Issues

* Page faults can introduce latency spikes.
* Access patterns may cause page cache thrashing.
* Large mappings can increase TLB pressure.
* Performance depends heavily on storage speed.

---

## MMF vs Traditional File I/O

| Feature           | Memory-Mapped File | Read/Write API |
| ----------------- | ------------------ | -------------- |
| Random Access     | Excellent          | Moderate       |
| Sequential Access | Good               | Good           |
| Copies            | Fewer              | More           |
| OS Page Cache     | Automatic          | Automatic      |
| IPC Support       | Excellent          | Poor           |
| Complexity        | Moderate           | Low            |

---

## .NET Example

```csharp
using var mmf = MemoryMappedFile.CreateFromFile("data.bin");

using var accessor = mmf.CreateViewAccessor();

long value = accessor.ReadInt64(0);
```

The file content becomes accessible through a memory view rather than explicit file reads.

---

## Low-Latency Considerations

For latency-sensitive systems:

* Warm pages before processing.
* Avoid first-touch page faults during critical paths.
* Use huge pages where appropriate.
* Measure major/minor page faults.
* Be aware that page faults can introduce microsecond-to-millisecond latency spikes.

---

## Summary

Memory-Mapped Files allow files to be accessed as memory, leveraging the operating system's virtual memory subsystem. They are especially useful for large datasets, random access workloads, and inter-process communication. Their performance comes from reduced copying and OS-managed caching, but page faults can become a significant latency source in performance-critical systems.
