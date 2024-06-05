# RCache
This started as an alternative for a slower, and less efficient custom cache implementation used at work. It outperforms MemoryCache, so I decided to keep investigating and see if I could match JitBit's FastCache which is incredibly fast.

# Current Results
| Method                             | CacheSize | Mean          | Error         | StdDev       | Gen0    | Allocated |
|----------------------------------- |---------- |--------------:|--------------:|-------------:|--------:|----------:|
| RichardCache_GetOrAdd              | 1000      |  89,264.45 ns | 40,168.900 ns | 6,216.182 ns | 16.4795 |  103944 B |
|  RichardCache_LookupExistingKeys    | 1000      | 100,729.88 ns | 27,841.956 ns | 4,308.573 ns | 16.4795 |  103946 B |
| RichardCache_LookupNonExistingKeys | 1000      |     101.81 ns |     82.895 ns |    12.828 ns |  0.0166 |     104 B |
| MemoryCache_LookupExistingKeys     | 1000      | 233,383.98 ns | 171,923.09 ns | 9,423.685 ns | 11.2305 |   71920 B |
| MemoryCache_LookupNonExistingKeys  | 1000      |     118.52 ns |      26.42 ns |     1.448 ns |  0.0114 |      72 B |
| MemoryCache_Add                    | 1000      | 322,932.31 ns |  92,134.45 ns | 5,050.200 ns | 52.2461 |  327920 B |
| FastCache_GetOrAdd                 | 1000      |  82,815.06 ns |  90,811.77 ns | 4,977.699 ns |  6.3477 |   39920 B |
| FastCache_LookupExistingKeys       | 1000      |  89,718.48 ns |  68,039.11 ns | 3,729.453 ns |  6.3477 |   39920 B |
| FastCache_LookupNonExistingKeys    | 1000      |      73.19 ns |      86.15 ns |     4.722 ns |  0.0063 |      40 B |



# Contributions
Please feel free to contribute.


