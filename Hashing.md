# Custom Hashing

## Why use custom hashing?

In the first implementation, station names were stored as strings and the default .NET string hashing was used:

```csharp
var hash = stationName.GetHashCode();
```

This approach is simple, but it has some performance costs.
To calculate the hash, we first need to create a string object:

```
raw bytes
    |
    v
UTF8 decoding
    |
    v
string allocation
    |
    v
GetHashCode()
```

For a normal application this is usually not a problem. However, in the 1 Billion Row Challenge we process a huge amount of data, so even small allocations become expensive.

Allocations cause:
- more work for the Garbage Collector
- more memory usage
- worse cache locality
- additional CPU overhead

## Why use XXHash?

XXHash is a fast non-cryptographic hash function.

It is designed for scenarios where we need:
- very fast hashing
- good distribution of values
- low CPU overhead

For this use case, we do not need cryptographic security. We only need a fast way to map station names to dictionary entries.

## Why use ulong instead of int?

The default .NET hash code returns int, which is 32-bit:

```
int  = 2^32 possible values
```

XXHash3 returns a 64-bit value:

```
ulong = 2^64 possible values
```

This gives a much larger space of possible hashes. More possible values means a lower probability of collisions:

```
int:
[hash space: 4 billion values]

ulong:
[hash space: 18 quintillion values]
```

Collisions are not incorrect because Dictionary can handle them, but they are slower because more entries need to be compared.

With fewer collisions we reduce:
- additional equality checks
- string comparisons
- unnecessary memory accesses

## Important note about collisions

A hash is not a unique identifier. Two different station names can theoretically have the same hash:
```
"Berlin" -> 123456
"London" -> 123456
```

Because of this, the final implementation still keeps the original station name:

```
Dictionary<ulong, (string Name, StationStats Stats)>
```
The hash is only used as a fast lookup key. If a collision happens, the original string can be compared to confirm the correct entry.