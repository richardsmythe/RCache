# RCache
This started as an alternative for a slower, and less efficient custom cache implementation used at work. It outperforms MemoryCache, so I decided to keep investigating and see if I could match JitBit's FastCache which is incredibly fast.

# Current Results
| Method                             | CacheSize | Mean          | Error         | StdDev       | Gen0    | Allocated |
|----------------------------------- |---------- |--------------:|--------------:|-------------:|--------:|----------:|
RichardCache_GetOrAdd              | 1000      | 73,063.88 ns |  66,950.44 ns |  3,669.779 ns | 16.4795 |  103944 B |
| RichardCache_LookupExistingKeys    | 1000      | 89,040.26 ns | 197,430.67 ns | 10,821.841 ns | 16.4795 |  103944 B |
| RichardCache_LookupNonExistingKeys | 1000      |     87.11 ns |     109.39 ns |      5.996 ns |  0.0166 |     104 B |
| MemoryCache_LookupExistingKeys     | 1000      | 233,383.98 ns | 171,923.09 ns | 9,423.685 ns | 11.2305 |   71920 B |
| MemoryCache_LookupNonExistingKeys  | 1000      |     118.52 ns |      26.42 ns |     1.448 ns |  0.0114 |      72 B |
| MemoryCache_Add                    | 1000      | 322,932.31 ns |  92,134.45 ns | 5,050.200 ns | 52.2461 |  327920 B |
| FastCache_GetOrAdd                 | 1000      | 70,586.85 ns | 112,218.24 ns |  6,151.061 ns |  6.3477 |   39920 B |
| FastCache_LookupExistingKeys       | 1000      | 67,343.71 ns |  30,711.26 ns |  1,683.388 ns |  6.3477 |   39920 B |
| FastCache_LookupNonExistingKeys    | 1000      |     71.13 ns |      45.39 ns |      2.488 ns |  0.0063 |      40 B |



# Contributions
Please feel free to contribute.


